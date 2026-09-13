using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ComfortMe.Patches;

[HarmonyPatch(typeof(StationExtension), "UpdateConnection")]
internal static class StationExtensionUpdateConnectionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(StationExtension __instance)
    {
        if (!ComfortLinks.SuppressVanillaStationLines())
        {
            return true;
        }

        __instance.StopConnectionEffect();
        return false;
    }
}

[HarmonyPatch]
internal static class StationExtensionStartConnectionPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(StationExtension)))
        {
            if (method.Name == "StartConnectionEffect")
            {
                yield return method;
            }
        }
    }

    [HarmonyPrefix]
    private static bool Prefix()
    {
        return !ComfortLinks.SuppressVanillaStationLines();
    }
}
