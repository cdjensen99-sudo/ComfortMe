using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ComfortMe;

[BepInPlugin(ModConstants.ModGuid, ModConstants.ModName, ModConstants.ModVersion)]
public sealed class ComfortMePlugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    private Harmony harmony;

    private void Awake()
    {
        Log = Logger;
        ModConfig.Bind(Config);

        if (!ModConfig.Enabled.Value)
        {
            Log.LogInfo($"{ModConstants.ModName} {ModConstants.ModVersion} is disabled in config.");
            return;
        }

        harmony = new Harmony(ModConstants.ModGuid);

        try
        {
            harmony.PatchAll(typeof(ComfortMePlugin).Assembly);
            Log.LogInfo($"Harmony applied {harmony.GetPatchedMethods().Count()} patch(es).");
        }
        catch (Exception ex)
        {
            Log.LogError($"Harmony PatchAll failed: {ex}");
        }

        Log.LogInfo($"{ModConstants.ModName} {ModConstants.ModVersion} loaded ({ModConstants.BuildLabel}, dll {GetDllTimestamp()}).");

        _ = new Terminal.ConsoleCommand(
            "comfortme",
            "Dump ComfortMe catalog HUD diagnostics",
            _ => GroupCatalogPanel.Dump(),
            false,
            false,
            false,
            false,
            false,
            false);
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
    }

    private static string GetDllTimestamp()
    {
        string path = Assembly.GetExecutingAssembly().Location;
        return System.IO.File.Exists(path)
            ? System.IO.File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss")
            : "unknown";
    }
}
