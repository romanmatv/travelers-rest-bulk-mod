using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;


namespace ImprovedClicks;

// [BepInPlugin("rbk-tr-FasterFueling",PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class SampleSubMod
{
    public record struct Foo(ConfigFile ConfigFile, string section)
    {
        public ConfigEntry<bool> enabled()
        {
            return this.bind("isEnabled", false, "Flag to enable or disable this mod");
        }
        public ConfigEntry<T> bind<T>(string key, T defaultValue, string description = "")
        {
            return ConfigFile.Bind(section, key, defaultValue, description);
        }
    }
    public static ConfigEntry<bool> EnabledConfig(ConfigFile Config, string section)
    {
        return Config.Bind(section, "isEnabled", false, "Flag to enable or disable this mod");
    }
    
    
    // public static void Awake(Harmony _harmony, ConfigFile Config, ManualLogSource Logger)
    // {
    //     var enabledFlag = Config.Bind(ModName, "isEnabled", false, "Flag to enable or disable mod.");
    //
    //     // additional configs
    //     _fuelInputWithRightClick = Config.Bind("FasterFueling", "input size", 5,
    //         "Change the amount of fuel to move on one click, while holding ModTrigger");
    //     
    //     if (enabledFlag.Value)
    //     {
    //         _harmony.PatchAll(typeof(Fueling));
    //         Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} loaded sub-module {nameof(Fueling)}!");
    //     }
    // }
    //
    // [HarmonyPatch(typeof(FuelElementUI), "FuelClicked")]
    // [HarmonyPrefix]
    // static void FuelElementUIClick(FuelElementUI __instance)
    // {
    //     if (__instance == null || !Plugin.ModTrigger(1)) return;
    //
    //     var slot = Traverse.Create(__instance)
    //         .Field("slot")
    //         .GetValue<Slot>();
    //
    //     for (var i = 1; i < (_fuelInputWithRightClick?.Value ?? 5); i++)
    //         __instance.OnSlotLeftClick(1, slot);
    // }
}