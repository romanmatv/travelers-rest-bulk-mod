using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;


namespace BetterTools;

public class BetterAx : RestlessMods.SubModBase
{
    internal static Plugin.Tier CurrentTier => Plugin.GetTier(Plugin.MaxLevel.Value, TavernReputation.GetMilestone());
    
    public new static void Awake(Harmony _harmony, ConfigFile configFile, ManualLogSource logger)
    {
        BaseSetup(_harmony, configFile, logger, nameof(BetterAx));

        BaseFinish(typeof(BetterAx));
    }


    [HarmonyPatch(typeof(Tree), "Awake")]
    [HarmonyPostfix]
    private static void Weaken(ref int ___workAmount)
    {
        if (!Plugin.LessenWorkByLevel.Value) return;

        var repLevel = TavernReputation.GetMilestone();
        ___workAmount = Plugin.Reduction(___workAmount, repLevel);
    }

}