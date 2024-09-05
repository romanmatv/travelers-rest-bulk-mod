using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;

namespace BetterTools;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public partial class Plugin : BaseUnityPlugin
{
    private static Harmony _harmony;
    internal static ConfigEntry<bool> LessenWorkByLevel;
    internal static ConfigEntry<int> MaxLevel;
    internal static ConfigEntry<int> MaxRows;
    private static ConfigEntry<int> _modGamepadHotKey;
    static ConfigFile _configFile;

    internal static bool ModTrigger(int PlayerId)
    {
        return PlayerInputs.GetPlayer(PlayerId).GetButton("RightMouseDetect")
               || PlayerInputs.GetPlayer(PlayerId).GetButton(ActionType.SprintHoldAction)
               || ReInput.players.GetPlayer(PlayerId - 1).controllers.Joysticks
                   .Any(Joystick => Joystick.GetButton(_modGamepadHotKey.Value))
            ;
    }

    private void Awake()
    {
        _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
        _configFile = Config;

        LessenWorkByLevel = Config.Bind("BetterTools", "isActive", true,
            "Flag enabling/disabling tool improvement");
        MaxLevel = Config.Bind("BetterTools", "maxLevel", 40, "Level to 1 hit everything.");
        MaxRows = Config.Bind("BetterTools", "maxRows", 6, "Max rows target for BetterTools.maxLevel.");
        

        _toolIdList = new[]
        {
            1060, // axe
            1061, // hoe
            1062, // spade
            1063, // pick
            1064, // scythe
            1435, // wateringCan
        };

        BetterAx.Awake(_harmony, _configFile, Logger);
        BetterHoe.Awake(_harmony, _configFile, Logger);
        BetterSpade.Awake(_harmony, _configFile, Logger);
        BetterPick.Awake(_harmony, _configFile, Logger);
        // BetterScythe.Awake(_harmony, _configFile, Logger);
        BetterWateringCan.Awake(_harmony, _configFile, Logger);

        _modGamepadHotKey = Config.Bind("BetterTools", "keycode for button trigger", 11,
            "Haven't mapped all buttons but L3 on Stadia controller is KeyCode 11 in the Rewire.JoyStick that is being used by TravellersRest.");

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    public enum Tier
    {
        Copper = 0,
        Iron = 1,
        Steel = 2,
    }

    private static byte[] _toolTextureBytes;
    private static int[] _toolIdList;


    private static byte[] ToolTextureSheetBytes
    {
        get
        {
            if (_toolTextureBytes != null) return _toolTextureBytes;

            var info = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("BetterTools.Textures.TieredTools.png");
            using var ms = new MemoryStream();
            info?.CopyTo(ms);
            _toolTextureBytes = ms.ToArray();
            return _toolTextureBytes;
        }
    }
}