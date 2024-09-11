using System.Diagnostics;
using BepInEx.Configuration;
using HarmonyLib;
using RestlessMods;

namespace FishingChanges;

public class BetterFishingPot : SubModBase
{
    private static ConfigEntry<bool> PerfectFishingPot => Config.Bind("perfectFishingPot", false, "fishing pot will never break");
    private static ConfigEntry<int> FishingPotExtraLives => Config.Bind("extra lives", 0, "number of extra lives to give the Fishing Pot");
    
    
    public new static void Awake()
    {
        BaseSetup(nameof(BetterFishingPot));

        Debug.Assert(PerfectFishingPot != null, nameof(PerfectFishingPot) + " != null");
        Debug.Assert(FishingPotExtraLives != null, nameof(FishingPotExtraLives) + " != null");

        BaseFinish(typeof(BetterFishingPot));
    }

    [HarmonyPatch(typeof(Nasa), "Start")]
    [HarmonyPrefix]
    private static void OnBuild(Nasa __instance, ref int ___remainingUses)
    {
        ___remainingUses += FishingPotExtraLives.Value;
    }

    [HarmonyPatch(typeof(Nasa), nameof(Nasa.SetObjectCaptured))]
    [HarmonyPrefix]
    private static void OnCapture(Nasa __instance, ref int ___remainingUses)
    {
        if (!PerfectFishingPot.Value) return;
        ___remainingUses++;
    }
}