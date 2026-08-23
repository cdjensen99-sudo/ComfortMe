namespace ComfortMe;

/// <summary>
/// Decides whether placing a candidate piece would raise comfort at the current snapshot.
/// Colors vs this room, not vs the global category max (that is Hygge's job).
/// </summary>
internal static class UpgradeClassifier
{
    internal static ComfortClassification Classify(Piece piece, ComfortSnapshot snapshot)
    {
        if (piece == null || snapshot == null || piece.m_comfort <= 0)
        {
            return new ComfortClassification(ComfortVerdict.Skip, Piece.ComfortGroup.None, 0, 0);
        }

        Piece.ComfortGroup group = piece.m_comfortGroup;
        int candidate = piece.m_comfort;

        if (group == Piece.ComfortGroup.None)
        {
            if (snapshot.HasActive(piece))
            {
                return new ComfortClassification(ComfortVerdict.Redundant, group, candidate, candidate);
            }

            if (snapshot.HasPlaced(piece))
            {
                return new ComfortClassification(ComfortVerdict.Unlit, group, 0, candidate);
            }

            return new ComfortClassification(ComfortVerdict.NewGroup, group, 0, candidate);
        }

        int current = snapshot.CurrentFor(group);
        if (candidate > current)
        {
            if (snapshot.IsInactivePresent(piece))
            {
                return new ComfortClassification(ComfortVerdict.Unlit, group, current, candidate);
            }

            ComfortVerdict verdict = current == 0 ? ComfortVerdict.NewGroup : ComfortVerdict.Upgrade;
            return new ComfortClassification(verdict, group, current, candidate);
        }

        if (candidate == current)
        {
            return new ComfortClassification(ComfortVerdict.Redundant, group, current, candidate);
        }

        return new ComfortClassification(ComfortVerdict.Downgrade, group, current, candidate);
    }

    internal static bool ShouldBadge(Player player, Piece piece, ComfortClassification classification, ComfortSnapshot snapshot)
    {
        if (!classification.IsUpgrade)
        {
            return false;
        }

        if (ModConfig.RequireMaterials.Value
            && player != null
            && !player.HaveRequirements(piece, Player.RequirementMode.CanBuild))
        {
            return false;
        }

        HighlightPolicy policy = ModConfig.Policy.Value;
        if (policy == HighlightPolicy.UpgradeOnly && classification.Verdict == ComfortVerdict.NewGroup)
        {
            return false;
        }

        if (policy == HighlightPolicy.BestAvailable
            && classification.Group != Piece.ComfortGroup.None
            && player != null)
        {
            int best = PieceDiscovery.BestUpgradeComfort(player, classification.Group, snapshot);
            return best > 0 && piece.m_comfort >= best;
        }

        return true;
    }
}
