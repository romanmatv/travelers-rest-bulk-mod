using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;


namespace BetterTools;

public class BetterPick : RestlessMods.SubModBase
{
    
    internal static Plugin.Tier CurrentTier => Plugin.GetTier(Plugin.MaxLevel.Value, TavernReputation.GetMilestone());

    public new static void Awake(Harmony _harmony, ConfigFile configFile, ManualLogSource logger)
    {
        BaseSetup(_harmony, configFile, logger, nameof(BetterPick));

        BaseFinish(typeof(BetterPick));
    }


    [HarmonyPatch(typeof(Rock), "Awake")]
    [HarmonyPrefix]
    private static void Weaken2(ref int ___workAmount)
    {
        if (!Plugin.LessenWorkByLevel.Value) return;

        var repLevel = TavernReputation.GetMilestone();
        ___workAmount = Plugin.Reduction(___workAmount, repLevel);
    }

    [HarmonyPatch(typeof(Rock), "Chop")]
    [HarmonyPrefix]
    private static void Weaken(ref int ___workAmount, ref int __1)
    {
        if (!Plugin.LessenWorkByLevel.Value) return;

        var repLevel = TavernReputation.GetMilestone();
        ___workAmount = __1 = Plugin.Reduction(___workAmount, repLevel);
        // ___workAmount = Plugin.GradualReduction(_maxLevel, ___workAmount, repLevel);
    }
}