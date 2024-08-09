using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace EndlessLateNights
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;
        private static int _hourToTriggerRestart = 2;
        private static int _minToTriggerRestart = 2;
        private static int _hoursToGoBack = 1;

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(TimeUI), "Update")]
        [HarmonyPrefix]
        static void EndlessLateNights(TimeUI __instance)
        {
            // TODO: find somewhere better for this check seems to really pull down FPS
            GameDate gameDate = HarmonyLib.Traverse.Create(TimeUI.FindObjectOfType<WorldTime>())
                .Field("currentGameDate")
                .GetValue<GameDate>();

            // if not trigger time continue
            if (gameDate.hour != _hourToTriggerRestart || gameDate.min < _minToTriggerRestart) return;

            // Todo: customize restart window
            WorldTime.ChangeHour((_hourToTriggerRestart + GameDate.HOUR_IN_DAY - _hoursToGoBack) % GameDate.HOUR_IN_DAY);
            WorldTime.forceSleepTime.hour += _hoursToGoBack;
        }
    }
}
