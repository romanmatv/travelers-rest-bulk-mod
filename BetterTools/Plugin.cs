using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BetterTools
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;
        private static ConfigEntry<bool> _lessenWorkByLevel;
        private static ConfigEntry<int> _maxLevel;
        private static ConfigEntry<int> _levelPerExtraRow;
        
        private void Awake()
        {
            //PlayerController.GetPlayer(1)._currentLocation;
            //PlayerController.TeleportPlayer(1, new Vector3(23.71f,-18.39f,0f), Location.Road);
            //PlayerController.TeleportPlayer(1, new Vector3(-394.78f, 400.93f, 0.00f), Location.Quarry);
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            _lessenWorkByLevel = Config.Bind("BetterTools", "isActive", true, "Flag enabling/disabling tool improvement by level");
            _maxLevel = Config.Bind("BetterTools", "maxLevel", 40, "Level to 1 hit everything.");
            _levelPerExtraRow = Config.Bind("BetterTools", "levelsPerWaterRow", 10, "Every X levels add a new 3x row of watering.");

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        static int Reduction(int workAmount, int level)
        {
            var result = workAmount * (1f - (float) level / _maxLevel.Value);
            return Mathf.Max(1, (int) Math.Floor(result));
        }

        [HarmonyPatch(typeof(WateringCan),nameof(WateringCan.Action))]
        [HarmonyPostfix]
        static void BetterWatering(WateringCan __instance)
        {
            var repLevel = TavernReputation.GetMilestone();

            var facing = PlayerController.GetPlayerDirection(1);
            var tileMod = facing is Direction.Left or Direction.Down ? -.5 : .5;

            var extraRows = (int) Math.Floor((float)repLevel / _levelPerExtraRow.Value);
            for (int i = 1; i <= extraRows; i++)
            {
                foreach (var soil in Traverse.Create(__instance).Field("fertileSoilsArray").GetValue<FertileSoil[]>())
                {
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

        
        // for now weaken on day start
        [HarmonyPatch(typeof(Tree), "Awake")]
        [HarmonyPostfix]
        static void WeakenTrees(Tree __instance, ref int ___workAmount)
        {
            if (!_lessenWorkByLevel.Value) return;

            var repLevel = TavernReputation.GetMilestone();
            ___workAmount = Reduction(___workAmount, repLevel);
        }

        [HarmonyPatch(typeof(Rock), "Chop")]
        [HarmonyPrefix]
        static void WeakenRocks(Rock __instance, ref int __1, int ___workAmount)
        {
            if (!_lessenWorkByLevel.Value) return;
            
            // TODO: if player has tiered Ax
            // for now go off level
            var repLevel = TavernReputation.GetMilestone();
            __1 = ___workAmount / Reduction(___workAmount, repLevel);
        }
    }
}
