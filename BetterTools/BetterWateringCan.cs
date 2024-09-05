using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;


namespace BetterTools;

public class BetterWateringCan : SampleSubModBase
{
    private static int _maxLevel;
    private static int _maxRows;
    public new static void Awake(Harmony _harmony, ConfigFile configFile, ManualLogSource logger)
    {
        BaseSetup(_harmony, configFile, logger, nameof(BetterWateringCan));
        
        var maxLevel = Config.Bind("maxLevelOverride", 0, "Change the max level for this tool only.");
        var maxRows = Config.Bind("maxRowsOverride", 0, "Change the max rows target for this tool only.");

        _maxLevel = maxLevel.Value > 0 ? maxLevel.Value : Plugin.MaxLevel.Value;
        _maxRows = maxRows.Value > 0 ? maxRows.Value : Plugin.MaxRows.Value;
        
        BaseFinish(typeof(BetterWateringCan));
    }

    internal static Plugin.Tier currentTier => Plugin.GetTier(_maxLevel, TavernReputation.GetMilestone());

    [HarmonyPatch(typeof(WateringCan), nameof(WateringCan.Action))]
    [HarmonyPostfix]
    private static void BetterWatering(WateringCan __instance, int __0, bool __result)
    {
        if (!__result || !Plugin.ModTrigger(__0)) return;
        var repLevel = TavernReputation.GetMilestone();
        var facing = PlayerController.GetPlayerDirection(__0);
        var tileMod = facing is Direction.Left or Direction.Down ? -.5 : .5;

        var extraRows = Plugin.Tiered(_maxLevel, _maxRows, repLevel);

        for (var i = 1; i <= extraRows; i++)
        {

            foreach (var soil in Traverse.Create(__instance).Field("fertileSoilsArray").GetValue<FertileSoil[]>())
            {
                if (soil?.transform?.position == null) continue;
                // if tier 1
                var plot = facing is Direction.Left or Direction.Right
                    ? new Vector2((float)(soil.transform.position.x + (tileMod * i)), soil.transform.position.y)
                    : new Vector2(soil.transform.position.x, (float)(soil.transform.position.y + (tileMod * i)));
                foreach (var component1 in Physics2D.OverlapPointAll((Vector2)plot))
                {
                    var component2 = component1.gameObject.GetComponent<FertileSoil>();
                    if (component2 == null) continue;
                    component2.daysUntilDry = 3;
                    component2.ShowDampGround();
                    break;
                }
            }
        }
    }
}
