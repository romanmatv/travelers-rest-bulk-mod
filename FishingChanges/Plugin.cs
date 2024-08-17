using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine.UI;
using HarmonyLib;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace FishingChanges
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;

        private static ConfigEntry<bool> _skipFishingMinigame;
        private static ConfigEntry<float> _nonRecordFishAnimationTime;

        private static ILHook changedFishingWaitTimesMod;
        
        private void Awake()
        {
            // Plugin startup logic
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));

            _skipFishingMinigame = Config.Bind("FishingChanges", "skipFishingMini-game", false,
                "Flag to disable fishing mini-game.");
            _nonRecordFishAnimationTime = Config.Bind("FishingChanges", "nonRecordAnimationTime", 0f,
                "the game spends 4.5 seconds on showing the fish, this setting changes animationTime for non record fish and trash only");
            
            var method = typeof(FishingController)
                ?.GetMethod("HFOJOILKKKB", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetStateMachineTarget();
            ILContext.Manipulator x = (ILContext il) =>
            {
                // Fun fact local variables not field variables will be lost after the enumeration yields
                var cursor = new ILCursor(il);

                for (var i = 0; i < 3; i++)
                {
                    if (!cursor.TryGotoNext(x => x.MatchLdcR4(1.5f))) continue;
                    cursor.Remove();

                    cursor.EmitDelegate(() => FishingTexts.Get(1).newRecordText.gameObject.activeInHierarchy 
                        ? 1.5f : _nonRecordFishAnimationTime.Value/3f);
                }
            };
            
            changedFishingWaitTimesMod = method == null ? null : new ILHook(method, x);
            
            Logger.LogInfo($"Mod: {PluginInfo.PLUGIN_NAME} {PluginInfo.PLUGIN_GUID} is loaded!");
        }


        //////////////////////////////////////////////////////////////////
        ///  Instant Catch
        [HarmonyPatch(typeof(FishingUI), "LateUpdate")]
        [HarmonyPrefix]
        static bool LateUpdatePrefix(FishingUI __instance)
        {
            if (_skipFishingMinigame.Value == false) return false;
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
