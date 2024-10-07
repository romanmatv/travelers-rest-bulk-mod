using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using Rewired;
using UnityEngine;

namespace RestlessMods;


public abstract class ModBase : BaseUnityPlugin
{
    protected internal static Harmony _harmony;
    protected internal static ConfigFile _config;
    protected internal static ManualLogSource Log;

    public void Setup(Type t, string pluginName)
    {
        _harmony = Harmony.CreateAndPatchAll(t, pluginName);
        Log = Logger;
        _config = Config;
    }
}

public class ModTrigger
{
    // test
    public ConfigFile Config { get; set; }
    public String ModName { get; set; }
    public ManualLogSource Log { get; set; }

    public ConfigEntry<int> GetModKeyEntry() =>
        Config.Bind(ModName, "ModKey by Controller", 11, "L3 is KeyCode 11, -1 is turn off");
    
    public ConfigEntry<KeyCode> GetKeyboardKeyEntry() =>
        Config.Bind(ModName, "ModKey by Keyboard", KeyCode.LeftShift, "Keyboard KeyCode to trigger the mod, None is turn off");

    public ConfigEntry<bool> IsActionKeyEntryEnabled() =>
        Config.Bind(ModName, "ModKey by Action (enable disable)", true, "whether or not you want the ModKey to work off an in game Action");
    public ConfigEntry<ActionType> GetActionKeyEntry() =>
        Config.Bind(ModName, "ModKey by Action", ActionType.SprintHoldAction, "ActionType to trigger the mod, SprintHoldAction is a good one");

    public ConfigEntry<bool> IsDescriptiveKeyEntryEnabled() =>
        Config.Bind(ModName, "ModKey by Description (enable disable)", true, "whether or not you want the ModKey to work off a description string (EXPERT)");
    public ConfigEntry<string> GetDescriptiveKeyEntry() =>
        Config.Bind(ModName, "ModKey by Description", "RightMouseDetect", "Descriptive string for user input to trigger the mode, RightMouseDetect is on holding right click");


    // ReSharper disable once UnusedMember.Global
    internal void FillInConfigFile()
    {
        _ = IsDescriptiveKeyEntryEnabled();
        _ = GetDescriptiveKeyEntry();
        _ = GetKeyboardKeyEntry();
        _ = GetModKeyEntry();
        _ = IsActionKeyEntryEnabled();
        _ = GetActionKeyEntry();
    }

    public bool ModTriggered(int PlayerId)
    {
        return (IsDescriptiveKeyEntryEnabled().Value && PlayerInputs.GetPlayer(PlayerId).GetButton("RightMouseDetect"))
               || (GetKeyboardKeyEntry().Value != KeyCode.None && Input.GetKeyDown(GetKeyboardKeyEntry().Value))
               || (GetModKeyEntry().Value > -1 && JoyStickGetButton(PlayerId, GetModKeyEntry().Value))
               || (IsActionKeyEntryEnabled().Value && PlayerInputs.GetPlayer(PlayerId).GetButton(GetActionKeyEntry().Value));
    }

    private bool JoyStickGetButton(int PlayerId, int controllerKey)
    {
        return ReInput.players.GetPlayer(PlayerId - 1).controllers.Joysticks
            .Any(joystick => joystick.GetButton(controllerKey));
    }
}

public abstract class CustomSpriteSheets
{
    private static readonly Dictionary<string, byte[]> SpriteSheets = new();

    public static Texture2D GetTextureBySpriteSheetName(string name)
    {
        var tex = new Texture2D(512, 512, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        byte[] image;
        if (SpriteSheets.TryGetValue(name, out var bytes))
            image = bytes;
        else
        {
            var filepath = @"BepInEx\plugins\" + name;
            if (File.Exists(filepath))
            {
                image = File.ReadAllBytes(@"BepInEx\plugins\" + name);
                SpriteSheets.Add(name, image);
            }
            else
            {
                Console.Error.WriteLine("Unable to find spritesheet " + filepath);
                return tex;
            }
        }

        tex.LoadImage(image);
        return tex;
    }
}

public abstract class RandomNameHelper
{
    /**
     *
     * ex:
     public class GameTileMaps : SerializedMonoBehaviour
    {
      private static GameTileMaps CJDEOPCONBO;
      // Static selfReferenceProperty
      public static GameTileMaps DGMLCMCCDHO
      {
        get
        {
          if ((UnityEngine.Object) GameTileMaps.CJDEOPCONBO == (UnityEngine.Object) null)
            GameTileMaps.CJDEOPCONBO = UnityEngine.Object.FindObjectOfType<GameTileMaps>();
          return GameTileMaps.CJDEOPCONBO;
        }
      }
    }
     */
    internal static PropertyInfo GetStaticSelfReferenceProperty<T>(Func<PropertyInfo, bool> filter = null)
    {
        return GetProperty<T, T>();
    }

    internal static FieldInfo GetField<T, TR>([CanBeNull] Func<FieldInfo, bool> filter = null)
    {
        return AccessTools.GetDeclaredFields(typeof(T))
            .First(field => field.FieldType == typeof(TR) && (filter == null || filter.Invoke(field)));
    }

    internal static PropertyInfo GetProperty<T, TR>([CanBeNull] Func<PropertyInfo, bool> filter = null)
    {
        return AccessTools.FirstProperty(
            typeof(T),
            // first property returning Self type
            property => property.GetMethod.ReturnType == typeof(TR) && (filter == null || filter.Invoke(property))
        );
    }

    /**
     * class T { static T AAAAAAAAAA; public static T SELF_PROPERTY => AAAAAAAAAA; } // pull value for T.SELF_PROPERTY
     */
    internal static T GetSelfInstance<T>([CanBeNull] Func<PropertyInfo, bool> filter = null)
    {
        return GetPropertyValue<T, T>(default, filter);
    }

    /**
     * class T { static TR AAAAAAAAAA; } // pull value for T.AAAAAAAAAA
     */
    internal static TR GetPropertyValue<T, TR>(T obj, [CanBeNull] Func<PropertyInfo, bool> filter = null)
    {
        return (TR)GetProperty<T, TR>(filter).GetValue(obj);
    }

    public record struct MethodFinder(Type Type, Type[] ParameterTypes, Func<MethodInfo, bool> Filter = null)
    {
        private MethodInfo _methodInfoCache;

        public MethodInfo GetMethod()
        {
            if (_methodInfoCache == null) _methodInfoCache = FindMethod(Type, ParameterTypes, Filter);
            return _methodInfoCache;
        }

        private static MethodInfo FindMethod(Type type, Type[] parameterTypes,
            [CanBeNull] Func<MethodInfo, bool> filter = null)
        {
            return AccessTools.FirstMethod(type, methodInfo =>
            {
                if (!methodInfo.IsStatic || (filter != null && filter.Invoke(methodInfo) == false)) return false;

                var arguments = methodInfo.GetParameters();
                if (arguments.Length < parameterTypes.Length) return false;
                return parameterTypes
                    .Where((parameterType, index) => arguments[index].ParameterType == parameterType)
                    .Count() == arguments.Length; 
            });
        }
    }


    public static string GetItemIdAndName(Item item)
    {
        var itemName = GetItemName(item);
        return item == null
            ? ""
            : string.Format("{0}{1}", GetItemId(item), string.IsNullOrWhiteSpace(itemName) ? "" : string.Format(" \"{0}\"", itemName));
    }
    public static int GetItemId(Item item)
    {
        return HarmonyLib.Traverse.Create(item).Field("id").GetValue<int>();
    }

    public static string GetItemName(Item item)
    {
        return (item is null
                ? ""
                : item.translationByID
                    ? LocalisationSystem.Get("Items/item_name_" + GetItemId(item))
                    : string.IsNullOrEmpty(item.nameId)
                        ? item.name
                        : item.nameId
            )?.Replace(",", "");
    }
}

public class SubModBase
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

    protected static Dictionary<string, ManualLogSource> logSources = new();
    protected static Dictionary<string, ModTrigger> _modTriggers = new();
    protected static Configuration Config;
    protected static ConfigEntry<bool> IsEnabled;
    protected static string ModName;
    protected static ManualLogSource Log;
    protected static Harmony Harmony;

    public static ModTrigger NewModTrigger(string modName, ConfigFile configFile, ManualLogSource Log)
    {
        var result = new ModTrigger
        {
            ModName = modName,
            Config = configFile,
            Log = Log,
        };
        _modTriggers[modName] = result;
        return result;
    }

    public static bool ModTrigger(string modName, int PlayerId)
    {
        return _modTriggers.TryGetValue(modName, out var trigger) && trigger.ModTriggered(PlayerId);
    }

    protected static void BaseSetup(string modName, bool modTriggers)
    {
        BaseSetup(modName);

        if (modTriggers)
        {
            _modTriggers.Add(modName, new ModTrigger
            {
                ModName = modName,
                Config = Config.ConfigFile,
                Log = Log,
            });
            _modTriggers[modName].FillInConfigFile();
        }
    }
    protected static void BaseSetup(string modName)
    {
        BaseSetup(ModBase._harmony, ModBase._config, ModBase.Log, modName);
    }

    protected static void BaseSetup(Harmony harmony, ConfigFile config, ManualLogSource logger, string modName)
    {
        Config = new Configuration(config, modName);
        Log = logger;
        Harmony = harmony;
        ModName = modName;
        IsEnabled = Config.Enabled();
    }

    protected static void BaseFinish(Type type)
    {
        if (!IsEnabled.Value) return;

        Harmony.PatchAll(type);
        Log.LogInfo("\t loaded sub-module " + ModName + "!");
    }

    protected static void LoadFailure()
    {
        Log.LogWarning("\t FAILED TO load sub-module " + ModName + "!");
    }

    public static void Awake()
    {
        BaseSetup(nameof(SubModBase));

        // Add more here

        BaseFinish(typeof(SubModBase));
    }
    public static void Awake(Harmony harmony, ConfigFile configFile, ManualLogSource logger)
    {
        BaseSetup(harmony, configFile, logger, nameof(SubModBase));

        // Add more here

        BaseFinish(typeof(SubModBase));
    }
}