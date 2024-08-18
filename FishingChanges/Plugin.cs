using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace FishingChanges
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;

        private static ConfigEntry<float> _nonRecordFishAnimationTime;

        private static ILHook changedFishingWaitTimesMod;
        
        private void Awake()
        {
            // Plugin startup logic
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));

            _nonRecordFishAnimationTime = Config.Bind("FishingChanges", "nonRecordAnimationTime", 0f,
                "the game spends 4.5 seconds on showing the fish, this setting changes animationTime for non record fish and trash only");


            var method = getDynamicFinishFishingMethod();
            changedFishingWaitTimesMod = method == null ? null : new ILHook(method, ChangeAnimationTime);
            
            Logger.LogInfo($"Mod: {PluginInfo.PLUGIN_NAME} {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static MethodBase getDynamicFinishFishingMethod()
        {
            var finishFishingMethod = typeof(FishingController)
                ?.GetMethod("nameof(FishingController.FinishFishing)");
            
            var dynamicMethodName = "";

            if (finishFishingMethod == null)
            {
                throw new Exception("Cannot load this mod " + finishFishingMethod.Name + " cannot be found.");
            }

            new ILHook(finishFishingMethod, (il) =>
            {
                var cursor = new ILCursor(il);

                // there will be a code of: call .* FishingController::<<<<dynamicMethodName>>>>(
                cursor.TryGotoNext(x => x.OpCode.Equals(OpCodes.Call));
                dynamicMethodName = cursor.Instrs[cursor.Index].ToString().Split("::")[1].Split("(")[0];
                Console.Out.WriteLine(finishFishingMethod.Name + " found as " + dynamicMethodName);
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
    }
}
