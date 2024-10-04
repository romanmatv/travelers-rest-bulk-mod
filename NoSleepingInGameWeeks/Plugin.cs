using System;
using BepInEx;
using HarmonyLib;

namespace NoSleepingInGameWeeks;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static PauseMenuUI PauseMenuUIInstance => AccessTools.FirstProperty(typeof(PauseMenuUI),
        property => property.GetMethod.ReturnType == typeof(PauseMenuUI)).GetValue(null, null) as PauseMenuUI;
        
    [HarmonyPatch(typeof(SaveUI), nameof(SaveUI.TitleFadeInFinished))]
    [HarmonyPostfix]
    private static void AfterGameLoad(SaveUI __instance, SaveSlotUI ___lastSlotSelected)
    {
        var instance = WorldTime.GetInstance();
        var currentGameDate = Traverse.Create(instance).Field("currentGameDate").GetValue<GameDate>();
        if (currentGameDate.hour != 11) return;
            
        PauseMenuUIInstance.Open(1);
        PauseMenuUIInstance.Open(2);
    }
        
    private void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(Plugin));

        // Plugin startup logic
        Console.Out.WriteLine($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }
}