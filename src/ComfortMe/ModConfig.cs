using BepInEx.Configuration;
using UnityEngine;

namespace ComfortMe;

internal enum HighlightPolicy
{
    AnyIncrease,
    BestAvailable,
    UpgradeOnly,
}

internal static class ModConfig
{
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<bool> DebugLogging;
    internal static ConfigEntry<bool> ShowValueBadge;
    internal static ConfigEntry<bool> BadgeUpgradesOnly;
    internal static ConfigEntry<bool> ShowGroupCatalog;
    internal static ConfigEntry<bool> ShowComfortCategory;
    internal static ConfigEntry<bool> ShowComfortLinks;
    internal static ConfigEntry<KeyboardShortcut> HudCursorKey;
    internal static ConfigEntry<HighlightPolicy> Policy;
    internal static ConfigEntry<bool> RequireMaterials;

    internal static void Bind(ConfigFile config)
    {
        Enabled = config.Bind(
            "General",
            "Enabled",
            true,
            "Enable ComfortMe. When false, no Harmony patches run.");

        DebugLogging = config.Bind(
            "General",
            "DebugLogging",
            false,
            "Log snapshot and classifier details to BepInEx.");

        ShowValueBadge = config.Bind(
            "Hud",
            "ShowValueBadge",
            true,
            "Show a compact +N chip on every comfort hammer icon, colored vs this room.");

        BadgeUpgradesOnly = config.Bind(
            "Hud",
            "BadgeUpgradesOnly",
            false,
            "When true, only green upgrade chips appear. Default shows +N on every comfort piece: green raise, grey equal/unlit, red lower.");

        ShowGroupCatalog = config.Bind(
            "Hud",
            "ShowGroupCatalog",
            true,
            "Show the slim This room panel beside the hammer: Base, Shelter, and pieces that currently count.");

        ShowComfortCategory = config.Bind(
            "Hud",
            "ShowComfortCategory",
            true,
            "Add a Comfort row to the 1.0 hammer Categories list. It shows every unlocked piece with comfort, without moving those recipes out of Furniture or Lighting.");

        ShowComfortLinks = config.Bind(
            "Hud",
            "ShowComfortLinks",
            true,
            "Draw sparkle lines to pieces that currently count when you hover the This room panel or the Rested status icon. While placing a comfort piece, draw a line from that ghost to you. Workbench/forge upgrade lines and Base/Shelter are not drawn.");

        HudCursorKey = config.Bind(
            "Hud",
            "HudCursorKey",
            new KeyboardShortcut(KeyCode.LeftAlt),
            "Hold this key while the hammer is out and ready to place or repair (piece grid closed) to free the mouse from the camera so you can hover the HUD. Normal look is unchanged until you hold it.");

        Policy = config.Bind(
            "Hud",
            "HighlightPolicy",
            HighlightPolicy.AnyIncrease,
            "AnyIncrease: badge every piece that would raise comfort. BestAvailable: only the best discovered piece in that group. UpgradeOnly: in-group upgrades, not missing groups.");

        RequireMaterials = config.Bind(
            "Hud",
            "RequireMaterials",
            false,
            "When true, only badge pieces you can currently place. Default leaves greyed icons badgeable as a shopping hint.");
    }

    internal static void LogDebug(string message)
    {
        if (DebugLogging.Value)
        {
            ComfortMePlugin.Log?.LogInfo(message);
        }
    }
}
