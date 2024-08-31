using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;


namespace ImprovedClicks;

public class FasterShopping : SampleSubModBase
{
    private static ConfigEntry<int> _shopInputWithRightClick;
    
    public new static void Awake(Harmony _harmony, ConfigFile configFile, ManualLogSource logger)
    {
        BaseSetup(_harmony, configFile, logger, nameof(FasterShopping));

        _shopInputWithRightClick = Config.Bind("input size", 50,
            "Change the amount of items to grab on one click, while holding ModTrigger");
        
        BaseFinish(typeof(FasterShopping));
    }
    
    [HarmonyPatch(typeof(ShopElementUI), nameof(ShopElementUI.SelectClicked))]
    [HarmonyPrefix]
    static void ShopElementUIClick(ShopElementUI __instance)
    {
        _log.LogInfo("Clicked a shop element");
        _log.LogInfo("trigger " + Plugin.ModTrigger(1));
        if (__instance == null || !Plugin.ModTrigger(1)) return;
        
        _log.LogInfo("Here at least passed the mod trigger");
        _log.LogInfo("rightClickAmount " + _shopInputWithRightClick.Value);
        
        for (var i = 1; i < _shopInputWithRightClick.Value; i++)
            __instance.SingleElementClicked();
    }

}