using System;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CameraOptions;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : RestlessMods.ModBase
{
    private void Awake()
    {
        Setup(typeof(Plugin), PluginInfo.PLUGIN_NAME);

        ZoomLevel.Awake();
        HudToggles.Awake();
        ScreenShot.Awake();

        // Plugin startup logic
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Update()
    {
        ZoomLevel.Update();
        HudToggles.Update();
        ScreenShot.Update();
    }
}

public class ZoomLevel : RestlessMods.SubModBase
{
    private static ConfigEntry<KeyCode> HotKey =>
        Config.ConfigFile.Bind(nameof(ZoomLevel), "HotKey", KeyCode.PageUp, "Key to change zoom +/-");

    private static readonly int[] ZoomLevels = { 103, 360, 550, 1250, 2000 };
    private static int Zoom { get; set; } = 2;

    private static GraphicsMenuUI _graphicsMenuUI;
    private static GraphicsMenuUI GraphicsMenuUI
    {
        get
        {
            if (!_graphicsMenuUI) _graphicsMenuUI = Object.FindObjectOfType<GraphicsMenuUI>();
            return _graphicsMenuUI;
        }
    }
        
    public new static void Awake()
    {
        BaseSetup(nameof(ZoomLevel));

        Debug.Assert(Config.Enabled != null, nameof(Config.Enabled) + " != null");
        Debug.Assert(HotKey != null, nameof(HotKey) + " != null");

        BaseFinish(typeof(ZoomLevel));
    }

    public static void Update()
    {
        if (!Config.Enabled().Value) return;

        if (!Input.GetKeyDown(HotKey.Value)) return;

        if (Zoom == 4) Zoom = -1;
        GraphicsMenuUI.ForceZoom(1, ZoomLevels[++Zoom]);
            
            
        // if (Input.GetKey(KeyCode.Plus) && Zoom < 4)
        //     GraphicsMenuUI.ForceZoom(1, ZoomLevels[++Zoom]);
        // else if (Input.GetKey(KeyCode.Minus) && Zoom > 0)
        //     GraphicsMenuUI.ForceZoom(1, ZoomLevels[--Zoom]);
    }
}

public class ScreenShot : RestlessMods.SubModBase
{
    private static ConfigEntry<KeyCode> HotKey => Config.ConfigFile.Bind(nameof(ScreenShot), "HotKey", KeyCode.Print, "Key to capture screenshot");

    public new static void Awake()
    {
        BaseSetup(nameof(ScreenShot));

        Debug.Assert(Config.Enabled != null, nameof(Config.Enabled) + " != null");
        Debug.Assert(HotKey != null, nameof(HotKey) + " != null");

        BaseFinish(typeof(ScreenShot));
    }
        
    public static void Update()
    {
        if (!Config.Enabled().Value) return;

        if (!Input.GetKeyDown(HotKey.Value)) return;

        var filename = string.Format("../BepInEx/plugins/{0}_{1}.png",
            HarmonyLib.Traverse.Create(PlayerController.GetPlayer(1)).Field("_currentLocation").GetValue<Location>().ToString(),
            DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture)[..10]
        );
        
        ScreenCapture.CaptureScreenshot(filename);
            
        Log.LogInfo("Oh, " + filename + ", what a nice memory.");
    }
}
public class HudToggles : RestlessMods.SubModBase
{
    private static ConfigEntry<KeyCode> HotKey => Config.ConfigFile.Bind(nameof(HudToggles),"HotKey", KeyCode.F1, "Key to toggle Hud On/Off");
    private static MainActionBarUI _mainActionBarUI;
    private static MainActionBarUI MainActionBarUI
    {
        get
        {
            if (!_mainActionBarUI) _mainActionBarUI = Object.FindObjectOfType<MainActionBarUI>();
            return _mainActionBarUI;
        }
    }
        
    private static TavernManagerUI _tavernManagerUI;
    private static TavernManagerUI TavernManagerUI
    {
        get
        {
            if (!_tavernManagerUI) _tavernManagerUI = Object.FindObjectOfType<TavernManagerUI>();
            return _tavernManagerUI;
        }
    }

    public new static void Awake()
    {
        BaseSetup(nameof(HudToggles));
            
        Debug.Assert(Config.Enabled != null, nameof(Config.Enabled) + " != null");
        Debug.Assert(HotKey != null, nameof(HotKey) + " != null");

        BaseFinish(typeof(HudToggles));
    }

    public static void Update()
    {
        if (!Config.Enabled().Value) return;

        if (!Input.GetKeyDown(HotKey.Value)) return;


        var actionBar = MainActionBarUI;
        var timeDate = TavernManagerUI;

        var toggle = !(actionBar.enabled || timeDate.enabled);

        actionBar.enabled = toggle;
        actionBar.gameObject.SetActive(toggle);
        timeDate.enabled = toggle;
        timeDate.gameObject.SetActive(toggle);
    }
}