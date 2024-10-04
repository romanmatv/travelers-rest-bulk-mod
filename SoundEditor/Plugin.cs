using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace SoundEditor;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static Harmony _harmony;
    private static ConfigFile _configFile;
        
    private void Awake()
    {
        _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
        _configFile = Config;
            
        SoundTest.Awake(_harmony, _configFile, Logger);

                
        // Plugin startup logic
        Console.Out.WriteLine($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }
}