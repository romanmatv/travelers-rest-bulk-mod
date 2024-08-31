using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;


namespace ImprovedClicks;

public class FasterWaterWell : SampleSubModBase
{
    private static CommonReferences _commonReferences;

    public new static void Awake(Harmony _harmony, ConfigFile configFile, ManualLogSource logger)
    {
        BaseSetup(_harmony, configFile, logger, nameof(FasterWaterWell));

        BaseFinish(typeof(FasterWaterWell));

    }
    
    [HarmonyPatch(typeof(CommonReferences), "Start")]
    [HarmonyPostfix] //Has to be a postfix so that common ref is loaded 
    private static void CommonReferenceSavingPostfix(CommonReferences __instance)
    {
        _commonReferences = __instance;
    }
        
    [HarmonyPatch(typeof(Well), "MouseUp")]
    [HarmonyPostfix]
    static void MouseUpPrefix(Well __instance)
    {
        bool repeat = Plugin.ModTrigger(1);
        var emptyBucket = _commonReferences?.bucketItem;

        if (!repeat || emptyBucket == null) return;
            
        while (__instance.IsAvailableByProximity(1) && PlayerInventory.GetPlayer(1).HasItem(emptyBucket))
        {
            __instance.MouseUp(1);
        }
    }

}