namespace ComfortMe;

internal enum ComfortVerdict
{
    Skip,
    NewGroup,
    Upgrade,
    Redundant,
    Unlit,
    Downgrade,
}

internal readonly struct ComfortClassification
{
    internal ComfortClassification(
        ComfortVerdict verdict,
        Piece.ComfortGroup group,
        int current,
        int candidate)
    {
        Verdict = verdict;
        Group = group;
        Current = current;
        Candidate = candidate;
        Delta = candidate - current;
    }

    internal ComfortVerdict Verdict { get; }

    internal Piece.ComfortGroup Group { get; }

    internal int Current { get; }

    internal int Candidate { get; }

    internal int Delta { get; }

    internal bool IsUpgrade => Verdict == ComfortVerdict.Upgrade || Verdict == ComfortVerdict.NewGroup;

    internal static int ShopRank(ComfortVerdict verdict)
    {
        switch (verdict)
        {
            case ComfortVerdict.Upgrade:
            case ComfortVerdict.NewGroup:
                return 0;
            case ComfortVerdict.Unlit:
                return 1;
            case ComfortVerdict.Redundant:
                return 2;
            case ComfortVerdict.Downgrade:
                return 3;
            default:
                return 4;
        }
    }
}
