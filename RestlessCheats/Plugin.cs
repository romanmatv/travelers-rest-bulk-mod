using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;

namespace RestlessCheats
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static Harmony _harmony;
        private static ConfigEntry<int> _modGamepadHotKey;

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            _modGamepadHotKey = Config.Bind("BetterTools", "keycode for button trigger", 11,
                "Haven't mapped all buttons but L3 on Stadia controller is KeyCode 11 in the Rewire.JoyStick that is being used by TravellersRest.");

            Teleporter.Awake(_harmony, Config, Logger);
            ItemDuplicator.Awake(_harmony, Config, Logger);
            
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        
        internal static bool ModTrigger(int PlayerId)
        {
            return PlayerInputs.GetPlayer(PlayerId).GetButton("RightMouseDetect")
                   || PlayerInputs.GetPlayer(PlayerId).GetButton(ActionType.SprintHoldAction)
                   || ReInput.players.GetPlayer(PlayerId - 1).controllers.Joysticks
                       .Any(Joystick => Joystick.GetButton(_modGamepadHotKey.Value))
                ;
        }
    }
}