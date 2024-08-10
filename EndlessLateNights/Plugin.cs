using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace EndlessLateNights
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;
        private static ConfigEntry<int> _hourToTriggerRestart;
        private static ConfigEntry<int> _minToTriggerRestart;
        private static ConfigEntry<int> _hoursToGoBack;
        private static WorldTime _worldTime;

        private static WorldTime WorldTime
        {
            get
            {
                if (_worldTime == null) _worldTime = UnityEngine.Object.FindObjectOfType<WorldTime>();
                return _worldTime;
            }
        }

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));

            _hourToTriggerRestart = Config.Bind("EndlessLateNights", "trigger hour", 2,
                "Hour to trigger the time loop");
            _minToTriggerRestart = Config.Bind("EndlessLateNights", "trigger min", 30,
                "Min to trigger the time loop");
            _hoursToGoBack = Config.Bind("EndlessLateNights", "hours to go back", 2,
                "Hours to go backwards (WARNING: going back to the previous day can mess with things)");
            
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(TimeUI), "Update")]
        [HarmonyPrefix]
        static void EndlessLateNights()
        {
            var gameDate = HarmonyLib.Traverse.Create(WorldTime)
                    .Field("currentGameDate")
                    .GetValue<GameDate>();
            
            // if not trigger time continue
            if (gameDate.hour != _hourToTriggerRestart.Value || gameDate.min < _minToTriggerRestart.Value) return;
            
            // Todo: customize restart window
            WorldTime.ChangeHour((gameDate.hour + GameDate.HOUR_IN_DAY - _hoursToGoBack.Value) % GameDate.HOUR_IN_DAY);
            WorldTime.forceSleepTime.hour += _hoursToGoBack.Value;
            UnityEngine.Debug.Log($"endless night going back {_hoursToGoBack.Value.ToString()} hours.");
        }
    }
}
