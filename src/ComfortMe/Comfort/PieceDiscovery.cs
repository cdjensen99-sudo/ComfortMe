using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ComfortMe;

internal readonly struct DiscoveredPiece
{
    internal DiscoveredPiece(Piece piece, Piece.PieceCategory category)
    {
        Piece = piece;
        Category = category;
    }

    internal Piece Piece { get; }

    internal Piece.PieceCategory Category { get; }
}

/// <summary>
/// Known (unlocked) hammer pieces across every tab, not only the visible category.
/// </summary>
internal static class PieceDiscovery
{
    private static readonly FieldInfo BuildPiecesField = AccessTools.Field(typeof(Player), "m_buildPieces");
    private static readonly FieldInfo AvailableField = AccessTools.Field(typeof(PieceTable), "m_availablePieces");

    internal static PieceTable GetTable(Player player)
    {
        return player == null ? null : BuildPiecesField?.GetValue(player) as PieceTable;
    }

    internal static List<DiscoveredPiece> AllComfort(Player player)
    {
        List<DiscoveredPiece> result = new List<DiscoveredPiece>();
        PieceTable table = GetTable(player);
        IList available = AvailableField?.GetValue(table) as IList;
        if (available == null)
        {
            return result;
        }

        for (int cat = 0; cat < available.Count; cat++)
        {
            if (available[cat] is not IList list)
            {
                continue;
            }

            Piece.PieceCategory category = (Piece.PieceCategory)cat;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is Piece piece && piece.m_comfort > 0)
                {
                    result.Add(new DiscoveredPiece(piece, category));
                }
            }
        }

        return result;
    }

    internal static int BestUpgradeComfort(Player player, Piece.ComfortGroup group, ComfortSnapshot snapshot)
    {
        int best = 0;
        List<DiscoveredPiece> discovered = AllComfort(player);
        for (int i = 0; i < discovered.Count; i++)
        {
            Piece piece = discovered[i].Piece;
            if (piece.m_comfortGroup != group)
            {
                continue;
            }

            ComfortClassification classification = UpgradeClassifier.Classify(piece, snapshot);
            if (classification.IsUpgrade && piece.m_comfort > best)
            {
                best = piece.m_comfort;
            }
        }

        return best;
    }

    internal static string CategoryLabel(PieceTable table, Piece.PieceCategory category)
    {
        if (table != null && table.m_categories != null && table.m_categoryLabels != null)
        {
            int index = table.m_categories.IndexOf(category);
            if (index >= 0 && index < table.m_categoryLabels.Count)
            {
                string label = table.m_categoryLabels[index];
                if (!string.IsNullOrEmpty(label) && Localization.instance != null)
                {
                    return Localization.instance.Localize(label);
                }

                if (!string.IsNullOrEmpty(label))
                {
                    return label;
                }
            }
        }

        return CategoryFallback(category);
    }

    internal static string LocalName(Piece piece)
    {
        if (piece == null || string.IsNullOrEmpty(piece.m_name))
        {
            return string.Empty;
        }

        return Localization.instance != null
            ? Localization.instance.Localize(piece.m_name)
            : piece.m_name;
    }

    internal static string GroupLabel(Piece.ComfortGroup group)
    {
        switch (group)
        {
            case Piece.ComfortGroup.Fire:
                return "Fire";
            case Piece.ComfortGroup.Bed:
                return "Bed";
            case Piece.ComfortGroup.Banner:
                return "Banner";
            case Piece.ComfortGroup.Chair:
                return "Seating";
            case Piece.ComfortGroup.Table:
                return "Table";
            case Piece.ComfortGroup.Carpet:
                return "Carpet";
            case Piece.ComfortGroup.None:
                return "Unique";
            default:
                return group.ToString();
        }
    }

    private static string CategoryFallback(Piece.PieceCategory category)
    {
        switch (category)
        {
            case Piece.PieceCategory.Misc:
                return "Misc";
            case Piece.PieceCategory.Crafting:
                return "Crafting";
            case Piece.PieceCategory.BuildingWorkbench:
                return "Building";
            case Piece.PieceCategory.BuildingStonecutter:
                return "Stonecutter";
            case Piece.PieceCategory.Furniture:
                return "Furniture";
            default:
                return category.ToString();
        }
    }
}
