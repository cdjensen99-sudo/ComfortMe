using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ComfortMe.Patches;

[HarmonyPatch(typeof(ByUsagePieceList), "UpdateAvailableTags")]
internal static class ComfortUsageTagInjectPatch
{
    [HarmonyPostfix]
    private static void Postfix(ByUsagePieceList __instance, PieceTable pieceTable)
    {
        try
        {
            ComfortUsageTag.Inject(__instance, pieceTable);
        }
        catch (Exception ex)
        {
            ComfortMePlugin.Log?.LogError($"Comfort category inject failed: {ex}");
        }
    }
}

[HarmonyPatch(typeof(ByUsagePieceList), "GetTagDisplayName")]
internal static class ComfortUsageTagNamePatch
{
    [HarmonyPrefix]
    private static bool Prefix(ByUsagePieceList __instance, int index, ref string __result)
    {
        if (!ComfortUsageTag.IsComfortRow(__instance, index))
        {
            return true;
        }

        __result = ComfortUsageTag.DisplayName;
        return false;
    }
}

[HarmonyPatch(typeof(ByUsagePieceList), "GetAvailablePiecesWithTag")]
internal static class ComfortUsageTagPiecesPatch
{
    [HarmonyPrefix]
    private static bool Prefix(int tagId, PieceTable pieceTable, IList<Piece> resultOut)
    {
        if (!ComfortUsageTag.IsAllocated(tagId))
        {
            return true;
        }

        try
        {
            ComfortUsageTag.Fill(pieceTable, resultOut);
        }
        catch (Exception ex)
        {
            ComfortMePlugin.Log?.LogError($"Comfort category fill failed: {ex}");
        }

        return false;
    }
}

[HarmonyPatch(typeof(ByUsagePieceList), "GetTagById")]
internal static class ComfortUsageTagByIdPatch
{
    [HarmonyPrefix]
    private static bool Prefix(int id, ref Piece.UsageTagFlags __result)
    {
        if (!ComfortUsageTag.IsAllocated(id))
        {
            return true;
        }

        __result = 0;
        return false;
    }
}

[HarmonyPatch(typeof(BuildUi), "UpdateTagButtons")]
internal static class ComfortUsageTagRetargetPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        try
        {
            ComfortUsageTag.RetargetSelection();
        }
        catch (Exception ex)
        {
            ComfortMePlugin.Log?.LogError($"Comfort category retarget failed: {ex}");
        }
    }
}

