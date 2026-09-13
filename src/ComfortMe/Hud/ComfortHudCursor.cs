using BepInEx.Configuration;
using UnityEngine;

namespace ComfortMe;

/// <summary>
/// Hold a configurable key (default Left Alt) to free the mouse from the camera
/// while a place tool is equipped and the piece grid is closed.
/// </summary>
internal static class ComfortHudCursor
{
    internal static bool ShouldFreeMouse()
    {
        if (!ModConfig.Enabled.Value)
        {
            return false;
        }

        Player player = Player.m_localPlayer;
        if (player == null || !player.InPlaceMode())
        {
            return false;
        }

        if (Hud.IsPieceSelectionVisible())
        {
            return false;
        }

        KeyboardShortcut key = ModConfig.HudCursorKey.Value;
        return key.MainKey != KeyCode.None && key.IsPressed();
    }

    internal static void FreeCursor()
    {
        ZCursor.LockState = CursorLockMode.None;
        ZCursor.Show();
    }
}
