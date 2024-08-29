using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MapLayoutChanges
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;

        private static ConfigEntry<bool> _removeTavernRocks;
        private static ConfigEntry<bool> _quarryRocksInfinite;

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));

            _removeTavernRocks = Config.Bind("MapChanges", "removeTavernRocks", false);
            _quarryRocksInfinite = Config.Bind("MapChanges", "quarryRocksInfinite", false, "To offset missing rocks around Tavern this option will allow endless mining of rocks in the 'Quarry' (north with the miners).");

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }


        [HarmonyPatch(typeof(Rock), "Start")]
        [HarmonyPrefix]
        private static bool ClearRocksFromRoad(Rock __instance, ref Placeable ___placeable, ref bool ___hasMaterial, ref Sprite ___emptyMaterialSprite, SpriteRenderer ___materialSpriteRenderer)
        {
            if (_quarryRocksInfinite.Value && Location.Quarry == ___placeable.currentLocation)
            {
                // material rocks have an empty sprite which we don't want to bother with the random named real sprite so just make the empty one the same as the real
                ___emptyMaterialSprite = ___materialSpriteRenderer.sprite;
            }
            if (_removeTavernRocks.Value == false || Location.Road != ___placeable.currentLocation) return true;

            // just yeet the rock out of the Road
            ___placeable.ChangePosition(new Vector3(0f, 7f, 0f));

            return false;
        }

        [HarmonyPatch(typeof(Rock), nameof(Rock.Chop))]
        [HarmonyPostfix]
        private static void RegrowRock(Rock __instance, int ___workAmount, Placeable ___placeable, ref GameObject ___initialGameObject, ref GameObject ___emptyGameObject)
        {
            if (!(__instance.work.WorkDone() >= 1) || !_quarryRocksInfinite.Value || ___placeable.currentLocation != Location.Quarry) return;
            
            // Reset the amount of work needed, and destroy/reStart rock
            var traversalInstance = HarmonyLib.Traverse.Create(__instance);

            __instance.work.StartWork(___workAmount);

            traversalInstance.Method("OnDestroy").GetValue();

            if (traversalInstance.Field("hasEmptyGameObject").GetValue<bool>())
            {
                ___initialGameObject.SetActive(true);
                ___emptyGameObject.SetActive(false);
            }

            traversalInstance.Method("Start").GetValue();
        }
    }
}
/*
Rock at [Road]  (13.75, -22.94, 0.00)       Filon de Cobre Variant 858768 [1042 - Mena de Cobre]. [Tavern, Road, BarnInterior]
Rock at [Road]  (22.00, -10.50, 0.00)       Filon de Piedra Grande Variant (1) 859530 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [Road]  (13.00, -24.50, 0.00)       Filon de Cobre Grande Variant 860276 [1042 - Mena de Cobre]. [Tavern, Road, BarnInterior]
Rock at [Road]  (23.00, -17.00, 0.00)       Filon de Hierro Grande Variant 860766 [1041 - Mena de Hierro]. [Tavern, Road, BarnInterior]
Rock at [Road]  (-3.00, -22.00, 0.00)       Filon de Carbon Grande Variant 861244 [1055 - Carbon]. [Tavern, Road, BarnInterior]

Rock at [Road]  (10.50, -0.33, 0.00)        Filon de Cobre Pequeño Variant 863474 [1042 - Mena de Cobre]. [Tavern, Road, BarnInterior]
Rock at [Road]  (21.75, -0.94, 0.00)        Filon de Carbon Variant 864076 [1055 - Carbon]. [Tavern, Road, BarnInterior]
Rock at [Road]  (2.75, -8.44, 0.00)         Filon de Piedra Variant 864500 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [Road]  (20.25, -18.94, 0.00)       Filon de Hierro Variant 865468 [1041 - Mena de Hierro]. [Tavern, Road, BarnInterior]
Rock at [Road]  (21.00, -1.83, 0.00)        Filon de Carbon Pequeño Variant 873082 [1055 - Carbon]. [Tavern, Road, BarnInterior]

Rock at [Road]  (4.75, -24.94, 0.00)        Filon de Piedra Variant 873678 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [Road]  (-2.50, -6.83, 0.00)        Filon de Hierro Pequeño Variant 875364 [1041 - Mena de Hierro]. [Tavern, Road, BarnInterior]
Rock at [Road]  (2.50, -16.33, 0.00)        Filon de Cobre Pequeño Variant 878506 [1042 - Mena de Cobre]. [Tavern, Road, BarnInterior]
Rock at [Road]  (3.50, -7.83, 0.00)         Filon de Piedra Pequeña MAIN 882210 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [Road]  (19.50, -19.83, 0.00)       Filon de Hierro Pequeño Variant (1) 882288 [1041 - Mena de Hierro]. [Tavern, Road, BarnInterior]

Rock at [None]  (23.75, 8.06, 0.00)         Filon de Carbon Variant (1) 881634 [1055 - Carbon]. [Tavern, Road, BarnInterior]
Rock at [None]  (-1.25, 7.56, 0.00)         Filon de Piedra Variant (7) 882106 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [None]  (-10.25, 0.06, 0.00)        Filon de Piedra Variant (5) 866672 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [None]  (-2.75, 8.06, 0.00)         Filon de Piedra Variant (8) 870146 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [None]  (6.00, 6.67, 0.00)          Filon de Piedra Pequeña MAIN (2) 860778 [1049 - Piedra]. [Tavern, Road, BarnInterior]

Rock at [None]  (4.75, 8.06, 0.00)          Filon de Piedra Variant (6) 861636 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [None]  (30.75, 8.06, 0.00)         Filon de Carbon Variant (2) 861122 [1055 - Carbon]. [Tavern, Road, BarnInterior]
Rock at [None]  (-9.50, -0.83, 0.00)        Filon de Piedra Pequeña MAIN (1) 861132 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [None]  (3.75, 7.56, 0.00)          Filon de Piedra Variant (1) 858898 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [None]  (-9.75, 6.06, 0.00)         Filon de Piedra Variant (4) 857632 [1049 - Piedra]. [Tavern, Road, BarnInterior]

Rock at [None]  (-10.25, 7.06, 0.00)        Filon de Piedra Variant (3) 857670 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [None]  (24.75, 7.56, 0.00)         Filon de Piedra Variant (2) 883732 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [None]  (-0.50, 7.67, 0.00)         Filon de Piedra Pequeña MAIN (3) 883792 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [None]  (31.50, 2.00, 0.00)         Filon de Piedra Grande Variant (2) 886550 [1049 - Piedra]. [Tavern, Road, BarnInterior]

Rock at [Quar]  (-415.75, 421.56, 0.00)     Filon de Carbon Variant 953116 [1055 - Carbon]. [Tavern, Road, BarnInterior]
Rock at [Quar]  (-388.25, 397.56, 0.00)     Filon de Piedra Variant 953654 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [Quar]  (-382.75, 406.56, 0.00)     Filon de Piedra Variant (3) 953950 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [Quar]  (-419.00, 421.00, 0.00)     Filon de Hierro Grande Variant (2) 954306 [1041 - Mena de Hierro]. [Tavern, Road, BarnInterior]
Rock at [Quar]  (-399.00, 426.50, 0.00)     Filon de Hierro Grande Variant (1) 954614 [1041 - Mena de Hierro]. [Tavern, Road, BarnInterior]

Rock at [Quar]  (-397.25, 412.06, 0.00)     Filon de Piedra Variant (2) 954752 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [Quar]  (-392.25, 415.06, 0.00)     Filon de Cobre Variant (2) 955436 [1042 - Mena de Cobre]. [Tavern, Road, BarnInterior]
Rock at [Quar]  (-416.00, 404.17, 0.00)     Filon de Piedra Pequeña MAIN (1) 956718 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [Quar]  (-409.25, 406.56, 0.00)     Filon de Carbon Variant (1) 957158 [1055 - Carbon]. [Tavern, Road, BarnInterior]
Rock at [Quar]  (-399.75, 427.56, 0.00)     Filon de Cobre Variant (1) 958842 [1042 - Mena de Cobre]. [Tavern, Road, BarnInterior]

Rock at [Quar]  (-413.75, 397.56, 0.00)     Filon de Piedra Variant (1) 959076 [1049 - Piedra]. [Tavern, Road, BarnInterior]
Rock at [Quar]  (-430.50, 417.00, 0.00)     Filon de Carbon Grande Variant (1) 959484 [1055 - Carbon]. [Tavern, Road, BarnInterior]
Rock at [Quar]  (-440.00, 410.00, 0.00)     Filon de Piedra Grande Variant (1) 959704 [1049 - Piedra]. [Tavern, Road, BarnInterior]
*/