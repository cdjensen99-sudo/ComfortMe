using BepInEx.Configuration;

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
            "When a comfort piece is selected, show that group's discovered pieces vs this room. The room total stays visible while the hammer is open either way.");

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
