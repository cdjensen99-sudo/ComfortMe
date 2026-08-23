using System;
using HarmonyLib;

namespace ComfortMe.Patches;

[HarmonyPatch(typeof(Hud), "UpdatePieceList")]
internal static class HudUpdatePieceListPatch
{
    [HarmonyPostfix]
    private static void Postfix(Hud __instance, Player player)
    {
        if (!ModConfig.Enabled.Value)
        {
            return;
        }

        try
        {
            ComfortSnapshot snapshot = ComfortSnapshot.Capture(player);
            BuildHudBadge.Refresh(__instance, snapshot);
        }
        catch (Exception ex)
        {
            ComfortMePlugin.Log?.LogError($"UpdatePieceList HUD failed: {ex}");
        }
    }
}

[HarmonyPatch(typeof(Hud), "SetupPieceInfo")]
internal static class HudSetupPieceInfoPatch
{
    [HarmonyPostfix]
    private static void Postfix(Hud __instance, Piece piece)
    {
        if (!ModConfig.Enabled.Value)
        {
            return;
        }

        try
        {
            ComfortSnapshot snapshot = ComfortSnapshot.Capture(Player.m_localPlayer);
            GroupCatalogPanel.Refresh(__instance, piece, snapshot);
        }
        catch (Exception ex)
        {
            ComfortMePlugin.Log?.LogError($"SetupPieceInfo HUD failed: {ex}");
        }
    }
}

[HarmonyPatch(typeof(Hud), "OnDestroy")]
internal static class HudOnDestroyPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        BuildHudBadge.Clear();
        GroupCatalogPanel.Hide();
    }
}
