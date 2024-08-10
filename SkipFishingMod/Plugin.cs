using BepInEx;
using HarmonyLib;
using UnityEngine.UI;

namespace SkipFishingMod
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
            
            Logger.LogInfo($"Mod: {PluginInfo.PLUGIN_NAME} {PluginInfo.PLUGIN_GUID} is loaded!");
            
            
        }


        //////////////////////////////////////////////////////////////////
        ///  Instant Catch


        [HarmonyPatch(typeof(FishingUI), "LateUpdate")]
        [HarmonyPrefix]
        static bool LateUpdatePrefix(FishingUI __instance)
        {
            if (!__instance.content.activeInHierarchy) return true;
            
            // Get the private slider object
            Slider reflectedSlider = Traverse.Create(__instance)
                .Field("progress")
                .GetValue<Slider>();

            if (reflectedSlider != null)
            {
                //Plugin.DebugLog(String.Format("LateUpdatePrefix: reflectedSlider found, value {0}", reflectedSlider.value));
                // Set progress slider value to 1.0 for instant completion
                reflectedSlider.value = 1.0f;
    
            }

            return true;
        }
    }
}