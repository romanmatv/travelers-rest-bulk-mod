using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;


namespace ImprovedClicks;

// [BepInPlugin("rbk-tr-FasterFueling",PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class SampleSubModBase
{
    protected record struct Configuration(ConfigFile ConfigFile, string SectionName)
    {
        public ConfigEntry<bool> Enabled()
        {
            return Bind("isEnabled", true, "Flag to enable or disable this mod");
        }
        public ConfigEntry<T> Bind<T>(string key, T defaultValue, string description = "")
        {
            return ConfigFile.Bind(SectionName, key, defaultValue, description);
        }
    };

    protected static Configuration Config;
    private static ConfigEntry<bool> _isEnabled;
    private static string _modName;
    protected static ManualLogSource _log;
    private static Harmony _harmony;

    protected static void BaseSetup(Harmony _harmony, ConfigFile Config, ManualLogSource Logger, string modName)
    {
        SampleSubModBase.Config = new Configuration(Config, modName);
        _log = Logger;
        SampleSubModBase._harmony = _harmony;
        _modName = modName;
        _isEnabled = SampleSubModBase.Config.Enabled();
    }

    protected static void BaseFinish(Type type)
    {
        if (!_isEnabled.Value) return;
    
        _harmony.PatchAll(type);
        _log.LogInfo("\t loaded sub-module " + _modName + "!");
    }

    public static void Awake(Harmony _harmony, ConfigFile configFile, ManualLogSource logger)
    {
        BaseSetup(_harmony, configFile, logger, nameof(SampleSubModBase));
        
        // Add more here
        
        BaseFinish(typeof(SampleSubModBase));
    }
}