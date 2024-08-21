using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        private static int[] _toolIdList;

        // private static byte[] _toolTextureBytesByFile;

        // public static byte[] toolTextureBytesByFile
        // {
        //     get
        //     {
        //         if (_toolTextureBytesByFile == null)
        //             _toolTextureBytesByFile = File.ReadAllBytes("BepInEx/plugins/rbk-tr-Assets/Textures/TieredTools.png");
        //         return _toolTextureBytesByFile;
        //     }
        // }

        private static byte[] _toolTextureBytes;

        public static byte[] toolTextureSheetBytes
        {
            get
            {
                if (_toolTextureBytes != null) return _toolTextureBytes;

                var info = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("BetterTools.Textures.TieredTools.png");
                using var ms = new MemoryStream();
                info?.CopyTo(ms);
                _toolTextureBytes = ms.ToArray();
                return _toolTextureBytes;
            }
        }

        private void Awake()
        {
            //PlayerController.GetPlayer(1)._currentLocation;
            //PlayerController.TeleportPlayer(1, new Vector3(23.71f,-18.39f,0f), Location.Road);
            //PlayerController.TeleportPlayer(1, new Vector3(-394.78f, 400.93f, 0.00f), Location.Quarry);
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            _lessenWorkByLevel = Config.Bind("BetterTools", "isActive", true,
                "Flag enabling/disabling tool improvement by level");
            _maxLevel = Config.Bind("BetterTools", "maxLevel", 40, "Level to 1 hit everything.");
            _levelPerExtraRow = Config.Bind("BetterTools", "levelsPerWaterRow", 10,
                "Every X levels add a new 3x row of watering.");

            _toolIdList = new[]
            {
                1060,
                1063,
                1435,
            };

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private enum Tier
        {
            Copper = 0,
            Iron = 32,
            Steel = 64,
        }

        static void textureChange(Tool tool)
        {
            var x = tool.GetHashCode() switch
            {
                1435 => _levelPerExtraRow.Value == 0 ? -1 : 0, // watering can
                1063 => 1, // pick
                1060 => 4, // axe
                _ => -1
            };
            if (x == -1) return;
            if (!_lessenWorkByLevel.Value) return;

            var level = TavernReputation.GetMilestone();
            var tier = Tier.Copper;

            if (x == 0)
            {
                var extraRows = (int)Math.Floor(level / (float)_levelPerExtraRow.Value);
                tier = extraRows switch
                {
                    1 => Tier.Iron,
                    > 1 => Tier.Steel,
                    _ => Tier.Copper
                };
            }
            else
            {
                if (level >= _maxLevel.Value)
                    tier = Tier.Steel;
                else if (level >= Math.Floor(_maxLevel.Value / 2f))
                    tier = Tier.Iron;
            }

            var tex = new Texture2D(512, 128, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                
            };
            tex.LoadImage(toolTextureSheetBytes);
            tool.icon = Sprite.Create(tex, new Rect(x * 32, (int)tier, 32, 32), new Vector2(0f, 0f));
        }

        static int Reduction(int workAmount, int level)
        {
            var result = workAmount * (1f - (float)level / _maxLevel.Value);
            return Mathf.Max(1, (int)Math.Floor(result));
        }

        [HarmonyPatch(typeof(WateringCan), nameof(WateringCan.Action))]
        [HarmonyPostfix]
        static void BetterWatering(WateringCan __instance)
        {
            var repLevel = TavernReputation.GetMilestone();

            var facing = PlayerController.GetPlayerDirection(1);
            var tileMod = facing is Direction.Left or Direction.Down ? -.5 : .5;

            var extraRows = (int)Math.Floor((float)repLevel / _levelPerExtraRow.Value);
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


        // On game finishing Load
        [HarmonyPatch(typeof(SaveUI), nameof(SaveUI.TitleFadeInFinished))]
        [HarmonyPostfix]
        static void ToolUpdates(SaveUI __instance, SaveSlotUI ___lastSlotSelected)
        {
            // Go through all items and apply tool changes
            foreach (var toolId in _toolIdList)
            {
                var tool = ItemDatabaseAccessor.GetItem(toolId);

                var inBag = PlayerInventory.GetPlayer(1)
                    .GetSlotsWithItem(tool, null, false, -1);
                var onHand = ActionBarInventory.GetPlayer(1)
                    .GetSlotsWithItem(tool, null, false, -1);
                var onIce = BuildingInventory.GetInstance()
                    .GetSlotsWithItem(tool, null, false, -1);

                ApplyTextureChanges(inBag);
                ApplyTextureChanges(onHand);
                ApplyTextureChanges(onIce);
            }
        }

        private static void ApplyTextureChanges(List<Slot> itemInstances)
        {
            foreach (var slot in itemInstances)
            {
                textureChange(Traverse.Create(slot.itemInstance).Field("item").GetValue<Tool>());
            }
        }
    }
}