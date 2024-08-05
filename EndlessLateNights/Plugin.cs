using BepInEx;
using HarmonyLib;

namespace EndlessLateNights
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        
        [HarmonyPatch(typeof(TimeUI), "Update")]
        [HarmonyPrefix]
        static void EndlessLateNights(TimeUI __instance)
        {
            GameDate gameDate = Traverse.Create(FindObjectOfType<WorldTime>())
                .Field("currentGameDate")
                .GetValue<GameDate>();

            if (gameDate.hour == 2 && gameDate.min >= 30)
            {
                WorldTime.ChangeHour(1);
                WorldTime.forceSleepTime.hour += 1;
            }
        }
    }
}
