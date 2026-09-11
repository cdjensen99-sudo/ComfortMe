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
            GroupCatalogPanel.RefreshFromHud(__instance, snapshot);
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
            BuildHudBadge.ApplyToImage(__instance.m_buildIcon, piece, snapshot);
        }
        catch (Exception ex)
        {
            ComfortMePlugin.Log?.LogError($"SetupPieceInfo HUD failed: {ex}");
        }
    }
}

[HarmonyPatch(typeof(BuildUiPieceButton), "Setup")]
internal static class BuildUiPieceButtonSetupPatch
{
    [HarmonyPostfix]
    private static void Postfix(BuildUiPieceButton __instance)
    {
        if (!ModConfig.Enabled.Value)
        {
            return;
        }

        try
        {
            ComfortSnapshot snapshot = ComfortSnapshot.Capture(Player.m_localPlayer);
            BuildHudBadge.ApplyToButton(__instance, snapshot);
        }
        catch (Exception ex)
        {
            ComfortMePlugin.Log?.LogError($"BuildUi piece badge failed: {ex}");
        }
    }
}

[HarmonyPatch(typeof(BuildUi), "UpdatePieceButtons")]
internal static class BuildUiUpdatePieceButtonsPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        if (!ModConfig.Enabled.Value)
        {
            return;
        }

        try
        {
            Hud hud = Hud.instance;
            ComfortSnapshot snapshot = ComfortSnapshot.Capture(Player.m_localPlayer);
            BuildHudBadge.OnPieceButtonsRebuilt(hud, snapshot);
        }
        catch (Exception ex)
        {
            ComfortMePlugin.Log?.LogError($"UpdatePieceButtons HUD failed: {ex}");
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
        GroupCatalogPanel.Clear();
    }
}
