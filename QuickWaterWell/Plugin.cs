using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace QuickWaterWell
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;
        private static CommonReferences _commonReferences;


        private void Awake()
        {
            // Plugin startup logic
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
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
            bool repeat = PlayerInputs.GetPlayer(1).GetButton("RightMouseDetect");
            var emptyBucket = _commonReferences?.bucketItem;

            if (!repeat || emptyBucket == null) return;
            
            while (__instance.IsAvailableByProximity(1) && PlayerInventory.GetPlayer(1).HasItem(emptyBucket))
            {
                __instance.MouseUp(1);
            }
        }
    }
}
