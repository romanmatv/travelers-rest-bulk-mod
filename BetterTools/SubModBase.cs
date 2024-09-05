using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;


namespace BetterTools;

public class SampleSubModBase
{
    protected readonly record struct Configuration(ConfigFile ConfigFile, string SectionName)
    {
        public ConfigEntry<bool> Enabled()
        {
            return Bind("isEnabled", true, "Flag to enable or disable this mod");
        }
        public ConfigEntry<T> Bind<T>(string key, T defaultValue, string description = "")
        {
            return ConfigFile.Bind(SectionName, key, defaultValue, description);
        }

        public ConfigFile ConfigFile { get; } = ConfigFile;
        public string SectionName { get; } = SectionName;
    }

    protected static Configuration Config;
    private static ConfigEntry<bool> _isEnabled;
    private static string _modName;
    private static ManualLogSource _log;
    private static Harmony _harmony;

    protected static void BaseSetup(Harmony harmony, ConfigFile config, ManualLogSource logger, string modName)
    {
        Config = new Configuration(config, modName);
        _log = logger;
        _harmony = harmony;
        _modName = modName;
        _isEnabled = Config.Enabled();
    }

    protected static void BaseFinish(Type type)
    {
        if (!_isEnabled.Value) return;
    
        _harmony.PatchAll(type);
        _log.LogInfo("\t loaded sub-module " + _modName + "!");
    }

    protected static void LoadFailure()
    {
        _log.LogWarning("\t FAILED TO load sub-module " + _modName + "!");
    }

    public static void Awake(Harmony harmony, ConfigFile configFile, ManualLogSource logger)
    {
        BaseSetup(harmony, configFile, logger, nameof(SampleSubModBase));
        
        // Add more here
        
        BaseFinish(typeof(SampleSubModBase));
    }
}