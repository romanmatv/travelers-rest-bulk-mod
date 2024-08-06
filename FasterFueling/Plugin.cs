using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace FasterFueling
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;
        private static ConfigEntry<int> _fuelInputWithRightClick;

        private void Awake()
        {
            // Plugin startup logic
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            _fuelInputWithRightClick = Config.Bind("FuelInput", "input size", 5,
                "Change the amount of fuel to insert when selecting fuel while holding RightMouse");

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(FuelElementUI), "FuelClicked")]
        [HarmonyPrefix]
        static void FuelElementUIClick(FuelElementUI __instance)
        {
            if (__instance == null || PlayerInputs.GetPlayer(1).GetButton("RightMouseDetect") == false) return;

            var slot = Traverse.Create(__instance)
                .Field("slot")
                .GetValue<Slot>();

            for (var i = 1; i < _fuelInputWithRightClick.Value; i++)
                __instance.OnSlotLeftClick(1, slot);
        }
    }
}