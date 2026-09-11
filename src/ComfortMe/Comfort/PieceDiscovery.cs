using System;
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
/// Known (unlocked) hammer pieces across every tab and usage filter, not only the visible list.
/// </summary>
internal static class PieceDiscovery
{
    private static readonly FieldInfo BuildPiecesField = AccessTools.Field(typeof(Player), "m_buildPieces");
    private static readonly FieldInfo AvailableByCategoryField = AccessTools.Field(typeof(PieceTable), "m_availablePiecesByCategory");
    private static readonly Piece.UsageTagFlags[] UsageFlags = (Piece.UsageTagFlags[])Enum.GetValues(typeof(Piece.UsageTagFlags));

    internal static PieceTable GetTable(Player player)
    {
        return player == null ? null : BuildPiecesField?.GetValue(player) as PieceTable;
    }

    internal static List<DiscoveredPiece> AllComfort(Player player)
    {
        List<DiscoveredPiece> result = new List<DiscoveredPiece>();
        PieceTable table = GetTable(player);
        if (table == null)
        {
            return result;
        }

        HashSet<Piece> seen = new HashSet<Piece>();
        if (table.m_availablePieces != null)
        {
            foreach (Piece piece in table.m_availablePieces)
            {
                AddComfort(result, seen, piece);
            }
        }

        if (result.Count == 0)
        {
            AddFromCategoryLists(result, seen, table);
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

    internal static string AppearanceSummary(Piece piece, PieceTable table)
    {
        if (piece == null)
        {
            return string.Empty;
        }

        List<string> parts = new List<string>();
        AddUnique(parts, CategoryLabel(table, piece.m_category));
        Piece.UsageTagFlags usage = piece.m_usage;
        for (int i = 0; i < UsageFlags.Length; i++)
        {
            Piece.UsageTagFlags flag = UsageFlags[i];
            if (flag == 0 || (usage & flag) != flag)
            {
                continue;
            }

            AddUnique(parts, UsageLabel(flag));
        }

        return string.Join(", ", parts);
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
            case Piece.ComfortGroup.Display:
                return "Display";
            case Piece.ComfortGroup.Decor:
                return "Decor";
            case Piece.ComfortGroup.Garland:
                return "Garland";
            case Piece.ComfortGroup.Lantern:
                return "Lantern";
            case Piece.ComfortGroup.Leisure:
                return "Leisure";
            case Piece.ComfortGroup.None:
                return "Unique";
            default:
                return group.ToString();
        }
    }

    private static void AddFromCategoryLists(List<DiscoveredPiece> result, HashSet<Piece> seen, PieceTable table)
    {
        IList available = AvailableByCategoryField?.GetValue(table) as IList;
        if (available == null)
        {
            return;
        }

        for (int cat = 0; cat < available.Count; cat++)
        {
            if (available[cat] is not IList list)
            {
                continue;
            }

            for (int i = 0; i < list.Count; i++)
            {
                AddComfort(result, seen, list[i] as Piece);
            }
        }
    }

    private static void AddComfort(List<DiscoveredPiece> result, HashSet<Piece> seen, Piece piece)
    {
        if (piece == null || piece.m_comfort <= 0 || !seen.Add(piece))
        {
            return;
        }

        result.Add(new DiscoveredPiece(piece, piece.m_category));
    }

    private static void AddUnique(List<string> parts, string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            if (string.Equals(parts[i], label, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        parts.Add(label);
    }

    private static string UsageLabel(Piece.UsageTagFlags flag)
    {
        DisplayNameAttribute attr = Utils.GetAttributeOfType<DisplayNameAttribute>(flag);
        string key = attr != null ? attr.DisplayName : null;
        if (!string.IsNullOrEmpty(key) && Localization.instance != null)
        {
            return Localization.instance.Localize(key);
        }

        if (!string.IsNullOrEmpty(key))
        {
            return key;
        }

        return flag.ToString();
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
            case Piece.PieceCategory.DeepNorth:
                return "Deep North";
            case Piece.PieceCategory.Feasts:
                return "Feasts";
            case Piece.PieceCategory.Food:
                return "Food";
            case Piece.PieceCategory.Meads:
                return "Meads";
            default:
                return category.ToString();
        }
    }
}
