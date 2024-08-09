using BepInEx;
using HarmonyLib;

namespace EndlessLateNights
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;
        private static int _hourToTriggerRestart = 11;
        private static int _minToTriggerRestart = 30;
        private static int _hoursToGoBack = 2;
        private static WorldTime _worldTime = new();
        private static bool _set;

        // private static TimeUI timeUI;

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(TimeUI), "Update")]
        [HarmonyPrefix]
        static void EndlessLateNights()
        {
            if (!_set)
            {
                _worldTime = TimeUI.FindObjectOfType<WorldTime>();
                _set = true;
            }

            var gameDate = HarmonyLib.Traverse.Create(_worldTime)
                    .Field("currentGameDate")
                    .GetValue<GameDate>();
            
            // if not trigger time continue
            if (gameDate.hour != _hourToTriggerRestart || gameDate.min < _minToTriggerRestart) return;
            
            // Todo: customize restart window
            WorldTime.ChangeHour((gameDate.hour + GameDate.HOUR_IN_DAY - _hoursToGoBack) % GameDate.HOUR_IN_DAY);
            WorldTime.forceSleepTime.hour += _hoursToGoBack;
            UnityEngine.Debug.Log($"endless night going back {_hoursToGoBack} hours.");

        }
    }
}
