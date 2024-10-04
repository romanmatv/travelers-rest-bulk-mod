using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;

namespace ImprovedClicks;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : RestlessMods.ModBase
{
    private static Harmony _harmony;
    private static ConfigEntry<int> _modGamepadHotKey;

    public static bool ModTrigger(int PlayerId)
    {
        return PlayerInputs.GetPlayer(PlayerId).GetButton("RightMouseDetect")
               || PlayerInputs.GetPlayer(PlayerId).GetButton(ActionType.SprintHoldAction)
               || ReInput.players.GetPlayer(PlayerId - 1).controllers.Joysticks
                   .Any(joystick => joystick.GetButton(_modGamepadHotKey.Value));
    }

    private void Awake()
    {
        Setup(typeof(Plugin), PluginInfo.PLUGIN_NAME);

        _modGamepadHotKey = Config.Bind("ImprovedClicks", "keycode for button trigger", 11,
            "Haven't mapped all buttons but L3 on Stadia controller is KeyCode 11 in the Rewire.JoyStick that is being used by TravellersRest.");

        FasterFueling.Awake();
        FasterShopping.Awake();
        FasterWaterWell.Awake();
        OtherSortingOptions.Awake();
        TapRefiller.Awake();
            
        // Plugin startup logic
        Console.Out.WriteLine($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }
}