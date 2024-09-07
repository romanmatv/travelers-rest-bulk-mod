using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using UnityEngine.UI;


namespace FishingChanges
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;

        private static ConfigEntry<float> _nonRecordFishAnimationTime;
        private static ConfigEntry<bool> _skipFishingMiniGame;

        private static ILHook changedFishingWaitTimesMod;
        
        private void Awake()
        {
            // Plugin startup logic
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin), PluginInfo.PLUGIN_NAME);

            _nonRecordFishAnimationTime = Config.Bind("FishingChanges", "nonRecordAnimationTime", 0f,
                "the game spends 4.5 seconds on showing the fish, this setting changes animationTime for non record fish and trash only");
            _skipFishingMiniGame = Config.Bind("FishingChanges", "skipFishingMiniGame", false,
                "Skips fishing minigame, note if another mod is also patching FishingUI::LateUpdate I will be uninstall that mod.");

            if (_skipFishingMiniGame.Value)
            {
                try
                {
                    // remove other skip minigame mods
                    var moddedMethod = typeof(FishingUI).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                    var otherMods = Harmony.GetPatchInfo(moddedMethod);

                    // Unpatch all mods on this method
                    _harmony.Unpatch(moddedMethod, HarmonyPatchType.Prefix, otherMods.Prefixes.First(patch => patch.owner != _harmony.Id).owner);
                }
                catch (Exception _)
                {
                    // ignored
                }
            }
            
            var method = getDynamicFinishFishingMethod();
            changedFishingWaitTimesMod = method == null ? null : new ILHook(method, ChangeAnimationTime);
            
            Logger.LogInfo($"Mod: {PluginInfo.PLUGIN_NAME} {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static MethodBase getDynamicFinishFishingMethod()
        {
            var finishFishingMethod = typeof(FishingController)
                ?.GetMethod(nameof(FishingController.FinishFishing));
            
            var dynamicMethodName = "";

            if (finishFishingMethod == null)
            {
                throw new Exception("Cannot load this mod " + finishFishingMethod.Name + " cannot be found.");
            }

            new ILHook(finishFishingMethod, (il) =>
            {
                var cursor = new ILCursor(il);

                cursor.TryGotoNext(x =>
                {
                    if (!x.OpCode.Equals(OpCodes.Call))
                        return false;
                    Console.Out.WriteLine(x.ToString());
                    return true;
                });

                // there will be a code of: call .* FishingController::<<<<dynamicMethodName>>>>(

                cursor.TryGotoNext(x =>
                {
                    if (!x.OpCode.Equals(OpCodes.Call))
                        return false;
                    
                    var opString = x?.ToString();
                    
                    var isExpectedMethod =
                        opString.Contains("System.Collections.IEnumerator") &&
                        opString.Contains("FishingController::") &&
                        opString.Contains("(UnityEngine.Vector3,System.Boolean)")
                    ;
                    
                    if (!isExpectedMethod) return false;
                    dynamicMethodName = opString.Split("::")[1].Split("(")[0];
                    Console.Out.WriteLine(finishFishingMethod.Name + " found as " + dynamicMethodName);
                    return true;
                });
            });
            
            
            return typeof(FishingController)
                ?.GetMethod(dynamicMethodName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetStateMachineTarget();
        }

        private static void ChangeAnimationTime(ILContext il)
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
        }
        
        //////////////////////////////////////////////////////////////////
        ///  Instant Catch (by DrStalker)
        [HarmonyPatch(typeof(FishingUI), "LateUpdate")]
        [HarmonyPrefix]
        static bool SkipFishingMinigame(FishingUI __instance)
        {
            if (_skipFishingMiniGame.Value == false) return false;
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
