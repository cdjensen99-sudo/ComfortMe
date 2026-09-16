using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ComfortMe;

/// <summary>
/// Virtual 1.0 hammer usage tag: a Comfort row on the Categories list.
/// Does not add a native PieceCategory or write a usage flag onto prefabs.
/// </summary>
internal static class ComfortUsageTag
{
    internal const string DisplayName = "Comfort";

    private const int FirstId = -100;
    private const int ShowAllId = -1;
    private const int FavoriteUiId = -10;

    private static readonly FieldInfo AvailableTagsField = AccessTools.Field(typeof(ByUsagePieceList), "m_availableTags");
    private static readonly FieldInfo UsageTagsField = AccessTools.Field(typeof(ByUsagePieceList), "m_usageTags");
    private static readonly FieldInfo CurrentTagIdField = AccessTools.Field(typeof(BuildUi), "m_currentTagId");
    private static readonly MethodInfo SetCurrentTagMethod = AccessTools.Method(typeof(BuildUi), "SetCurrentTag", new[] { typeof(int), typeof(bool) });

    private static int allocatedId;
    private static int previousId;
    private static bool loggedInsert;

    internal static void Clear()
    {
        allocatedId = 0;
        previousId = 0;
        loggedInsert = false;
    }

    internal static bool IsAllocated(int tagId)
    {
        return tagId != 0 && (tagId == allocatedId || tagId == previousId);
    }

    internal static bool IsComfortTabSelected()
    {
        if (!ModConfig.ShowComfortCategory.Value || allocatedId == 0)
        {
            return false;
        }

        Hud hud = Hud.instance;
        if (hud == null || hud.m_buildUi == null || !hud.m_buildUi.gameObject.activeInHierarchy)
        {
            return false;
        }

        return CurrentTagIdField?.GetValue(hud.m_buildUi) is int id && IsAllocated(id);
    }

    internal static bool IsComfortRow(ByUsagePieceList list, int index)
    {
        List<int> tags = AvailableTags(list);
        return tags != null && index >= 0 && index < tags.Count && IsAllocated(tags[index]);
    }

    internal static void Inject(ByUsagePieceList list, PieceTable table)
    {
        if (!ModConfig.Enabled.Value || !ModConfig.ShowComfortCategory.Value)
        {
            allocatedId = 0;
            return;
        }

        List<int> tags = AvailableTags(list);
        if (tags == null || !HasComfortPiece(table))
        {
            allocatedId = 0;
            return;
        }

        int previous = allocatedId;
        int id = ClaimId(tags);
        int insertAt = InsertIndex(list, tags);
        tags.Insert(insertAt, id);
        previousId = previous;
        allocatedId = id;

        if (!loggedInsert)
        {
            loggedInsert = true;
            ComfortMePlugin.Log?.LogInfo($"Comfort category row id={id} at list index {insertAt}.");
        }
    }

    internal static void RetargetSelection()
    {
        BuildUi ui = Hud.instance != null ? Hud.instance.m_buildUi : null;
        if (ui == null || CurrentTagIdField == null || allocatedId == 0)
        {
            previousId = allocatedId;
            return;
        }

        object current = CurrentTagIdField.GetValue(ui);
        if (current is int currentId && previousId != 0 && currentId == previousId && currentId != allocatedId)
        {
            SetCurrentTagMethod?.Invoke(ui, new object[] { allocatedId, false });
        }

        previousId = allocatedId;
    }

    internal static void Fill(PieceTable table, IList<Piece> resultOut)
    {
        if (resultOut == null)
        {
            return;
        }

        resultOut.Clear();
        if (table == null || table.m_availablePieces == null)
        {
            return;
        }

        List<Piece> pieces = new List<Piece>();
        foreach (Piece piece in table.m_availablePieces)
        {
            if (piece == null || piece.m_comfort <= 0 || piece.m_repairPiece || piece.m_removePiece)
            {
                continue;
            }

            pieces.Add(piece);
        }

        ComfortSnapshot snapshot = ComfortSnapshot.Capture(Player.m_localPlayer);
        pieces.Sort((a, b) => CompareComfort(a, b, snapshot));
        for (int i = 0; i < pieces.Count; i++)
        {
            resultOut.Add(pieces[i]);
        }
    }

    private static List<int> AvailableTags(ByUsagePieceList list)
    {
        return list == null ? null : AvailableTagsField?.GetValue(list) as List<int>;
    }

    private static int ClaimId(List<int> tags)
    {
        int id = FirstId;
        while (id == ShowAllId || id == FavoriteUiId || tags.Contains(id))
        {
            id--;
            if (id < -1_000_000)
            {
                break;
            }
        }

        return id;
    }

    private static int InsertIndex(ByUsagePieceList list, List<int> tags)
    {
        Piece.UsageTagFlags[] usageTags = UsageTagsField?.GetValue(list) as Piece.UsageTagFlags[];
        if (usageTags == null)
        {
            return tags.Count;
        }

        int furnitureSlot = -1;
        for (int i = 0; i < usageTags.Length; i++)
        {
            if (usageTags[i] == Piece.UsageTagFlags.Furniture)
            {
                furnitureSlot = i;
                break;
            }
        }

        if (furnitureSlot < 0)
        {
            return tags.Count;
        }

        int furnitureRow = tags.IndexOf(furnitureSlot);
        return furnitureRow >= 0 ? furnitureRow + 1 : tags.Count;
    }

    private static bool HasComfortPiece(PieceTable table)
    {
        if (table == null || table.m_availablePieces == null)
        {
            return false;
        }

        foreach (Piece piece in table.m_availablePieces)
        {
            if (piece != null && piece.m_comfort > 0 && !piece.m_repairPiece && !piece.m_removePiece)
            {
                return true;
            }
        }

        return false;
    }

    private static int CompareComfort(Piece a, Piece b, ComfortSnapshot snapshot)
    {
        int rank = ComfortClassification.ShopRank(UpgradeClassifier.Classify(a, snapshot).Verdict)
            .CompareTo(ComfortClassification.ShopRank(UpgradeClassifier.Classify(b, snapshot).Verdict));
        if (rank != 0)
        {
            return rank;
        }

        int comfort = b.m_comfort.CompareTo(a.m_comfort);
        if (comfort != 0)
        {
            return comfort;
        }

        return string.CompareOrdinal(PieceDiscovery.LocalName(a), PieceDiscovery.LocalName(b));
    }
}
