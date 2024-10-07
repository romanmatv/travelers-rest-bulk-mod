using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RestlessCheats;

public class Teleporter : SampleSubModBase
{
    private enum TeleportationTargets
    {
        Bed,
        Bar,
    }

    private static ConfigEntry<TeleportationTargets> BindTeleportationDestination()
    {
        return Config.Bind("Destination", TeleportationTargets.Bar, "Where to teleport with the 'Broom'");
    }
    private static Vector3 TargetPosition =>
        BindTeleportationDestination().Value switch
        {
            TeleportationTargets.Bed => Object.FindObjectOfType<Bed>().transform.position,
            TeleportationTargets.Bar => Object.FindObjectOfType<Bar>().transform.position,
            _ => throw new ArgumentOutOfRangeException()
        };
    
    public new static void Awake(Harmony harmony, ConfigFile configFile, ManualLogSource logger)
    {
        BaseSetup(harmony, configFile, logger, nameof(Teleporter));
        
        // Add more here
        BindTeleportationDestination();
        
        BaseFinish(typeof(Teleporter));
    }

    [HarmonyPatch(typeof(Mop), nameof(Mop.Action))]
    [HarmonyPostfix]
    public static void ToolAction(Mop __instance, int __0, bool __result)
    {
        if (__result || !Plugin.ModTrigger(__0)) return;

        PlayerController.TeleportPlayer(__0, TargetPosition, Location.Tavern);
    }
}