using HarmonyLib;
using UnityEngine;

namespace ComfortMe.Patches;

[HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
internal static class GameCameraMouseCapturePatch
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        if (!ComfortHudCursor.ShouldFreeMouse())
        {
            return true;
        }

        ComfortHudCursor.FreeCursor();
        return false;
    }
}

[HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseDelta))]
internal static class ZInputMouseDeltaPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref Vector2 __result)
    {
        if (ComfortHudCursor.ShouldFreeMouse())
        {
            __result = Vector2.zero;
        }
    }
}
