using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;

namespace ImprovedClicks
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static Harmony _harmony;
        private static ConfigEntry<int> _modGamepadHotKey;

        public static bool ModTrigger(int PlayerId)
        {
            return PlayerInputs.GetPlayer(PlayerId).GetButton("RightMouseDetect")
                   || ReInput.players.GetPlayer(PlayerId - 1).controllers.Joysticks
                       .Any(joystick => joystick.GetButton(_modGamepadHotKey.Value));
        }

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));

            _modGamepadHotKey = Config.Bind("ImprovedClicks", "keycode for button trigger", 11,
                "Haven't mapped all buttons but L3 on Stadia controller is KeyCode 11 in the Rewire.JoyStick that is being used by TravellersRest.");

            
            FasterFueling.Awake(_harmony, Config, Logger);
            FasterShopping.Awake(_harmony, Config, Logger);
            FasterWaterWell.Awake(_harmony, Config, Logger);
            
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}