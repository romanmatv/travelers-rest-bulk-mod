using BepInEx.Configuration;
using HarmonyLib;
using RestlessMods;


namespace ImprovedClicks;

public class FasterFueling : SubModBase
{
    private static ConfigEntry<int> _fuelInputWithRightClick;
    
    public new static void Awake()
    {
        BaseSetup(nameof(FasterFueling));
        
        _fuelInputWithRightClick = Config.Bind("input size", 5,
        "Change the amount of fuel to move on one click, while holding ModTrigger");
        
        BaseFinish(typeof(FasterFueling));
    }

    [HarmonyPatch(typeof(FuelElementUI), "FuelClicked")]
    [HarmonyPrefix]
    static void FuelElementUIClick(FuelElementUI __instance)
    {
        if (__instance == null || !Plugin.ModTrigger(1)) return;

        var slot = Traverse.Create(__instance)
            .Field("slot")
            .GetValue<Slot>();

        for (var i = 1; i < (_fuelInputWithRightClick?.Value ?? 5); i++)
            __instance.OnSlotLeftClick(1, slot);
    }
}

/*
public class Fueling
{
    private static ConfigEntry<bool> _enabled;
    private static SampleSubMod.Foo _configurations;
    private static ConfigEntry<int> _fuelInputWithRightClick;

    public static void Setup(Harmony _harmony, ConfigFile Config, ManualLogSource Logger)
    {
        _configurations = new SampleSubMod.Foo(Config, "FasterFueling");
        
        _enabled = _configurations.enabled();
        _fuelInputWithRightClick = _configurations.bind("input size", 5,
            "Change the amount of fuel to move on one click, while holding ModTrigger");


        if (_enabled.Value)
        {
            _harmony.PatchAll(typeof(Fueling));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} loaded sub-module {nameof(Fueling)}!");
        }

        
        
    }

    [HarmonyPatch(typeof(FuelElementUI), "FuelClicked")]
    [HarmonyPrefix]
    static void FuelElementUIClick(FuelElementUI __instance)
    {
        if (__instance == null || !Plugin.ModTrigger(1)) return;

        var slot = Traverse.Create(__instance)
            .Field("slot")
            .GetValue<Slot>();

        for (var i = 1; i < (_fuelInputWithRightClick?.Value ?? 5); i++)
            __instance.OnSlotLeftClick(1, slot);
    }
}*/