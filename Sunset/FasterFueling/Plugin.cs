using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;

namespace FasterFueling
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;
        private static ConfigEntry<int> _fuelInputWithRightClick;
        private static ConfigEntry<int> _shopInputWithRightClick;
        private static ConfigEntry<int> _modGamepadHotKey;


        private static bool ModTrigger(int PlayerId)
        {
            return PlayerInputs.GetPlayer(PlayerId).GetButton("RightMouseDetect")
                   || ReInput.players.GetPlayer(PlayerId - 1).controllers.Joysticks
                       .Any(joystick => joystick.GetButton(_modGamepadHotKey.Value));
        }

        private void Awake()
        {
            // Plugin startup logic
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            _fuelInputWithRightClick = Config.Bind("FuelInput", "input size", 5,
                "Change the amount of fuel to insert when selecting fuel while holding RightMouse");
            _fuelInputWithRightClick = Config.Bind("ShopInput", "input size", 25,
                "Change the amount of items to select in Shopping Menus while holding RightMouse");
            _modGamepadHotKey = Config.Bind("AllInput", "keycode for button trigger", 11,
                "Haven't mapped all buttons but L3 on Stadia controller is KeyCode 11 in the Rewire.JoyStick that is being used by TravellersRest.");

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
        
        [HarmonyPatch(typeof(ShopElementUI), nameof(ShopElementUI.SelectClicked))]
        [HarmonyPrefix]
        static void ShopElementUIClick(ShopElementUI __instance)
        {
            if (__instance == null || !ModTrigger(1)) return;
            
            for (var i = 1; i < _shopInputWithRightClick.Value; i++)
                __instance.SingleElementClicked();
        }
    }
}