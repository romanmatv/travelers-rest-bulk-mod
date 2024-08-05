using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace FasterFueling
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;
        private static CommonReferences references;


        private void Awake()
        {
            // Plugin startup logic
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            references = CommonReferences.FindObjectOfType<CommonReferences>();
            
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            
            
        }
        
        [HarmonyPatch(typeof(Well), "MouseUp")]
        [HarmonyPostfix]
        static void MouseUpPrefix(Well __instance)
        {
            GameObject gameObject = FindObjectOfType<GameObject>();
            if (gameObject == null)
            {
                gameObject = new GameObject();
            }

            if (references == null)
            {
                references = FindObjectOfType<CommonReferences>();
                if (references == null)
                {
                    references = gameObject
                        .AddComponent<CommonReferences>();
                }
            }
            
            bool repeat = Input.GetMouseButton(1);
            var emptyBucket = references?.bucketItem;

            if (!repeat || emptyBucket == null || !__instance.IsAvailableByProximity(1)) return;
            
            while (PlayerInventory.GetPlayer(1).HasItem(emptyBucket))
            {
                __instance.MouseUp(1);
            }
        }
    }
}
