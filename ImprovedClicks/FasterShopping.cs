using BepInEx.Configuration;
using HarmonyLib;
using RestlessMods;


namespace ImprovedClicks;

public class FasterShopping : SubModBase
{
    private static ConfigEntry<int> _shopInputWithRightClick;
    
    public new static void Awake()
    {
        BaseSetup(nameof(FasterShopping));

        _shopInputWithRightClick = Config.Bind("input size", 50,
            "Change the amount of items to grab on one click, while holding ModTrigger");
        
        BaseFinish(typeof(FasterShopping));
    }
    
    [HarmonyPatch(typeof(ShopElementUI), nameof(ShopElementUI.SelectClicked))]
    [HarmonyPrefix]
    static void ShopElementUIClick(ShopElementUI __instance)
    {
        if (__instance == null || !Plugin.ModTrigger(1)) return;
        
        for (var i = 1; i < _shopInputWithRightClick.Value; i++)
            __instance.SingleElementClicked();
    }

}