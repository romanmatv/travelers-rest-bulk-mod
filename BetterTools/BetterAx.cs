using HarmonyLib;


namespace BetterTools;

public class BetterAx : RestlessMods.SubModBase
{
    internal static Plugin.Tier CurrentTier => Plugin.GetTier(Plugin.MaxLevel.Value, TavernReputation.GetMilestone());
    
    public new static void Awake()
    {
        BaseSetup(nameof(BetterAx));

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