using System;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using Rewired;

namespace RestlessMods;

public abstract class ModTrigger
{
    // test
    public static ConfigFile Config { get; set; }

    public static ConfigEntry<int> GetModKeyEntry(string section) =>
        Config.Bind(section, "ModKey for Controller", 11, "L3 is KeyCode 11");

    public static bool ModTriggered(string section, int PlayerId)
    {
        var modKey = GetModKeyEntry(section).Value;
        return PlayerInputs.GetPlayer(PlayerId).GetButton("RightMouseDetect")
               || PlayerInputs.GetPlayer(PlayerId).GetButton(ActionType.SprintHoldAction)
               || ReInput.players.GetPlayer(PlayerId - 1).controllers.Joysticks
                   .Any(joystick => joystick.GetButton(modKey));
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

                // (parameterInfo, index) =>
                // arguments[index].ParameterType == typeof(ParameterInfo));
                //         arguments[0].ParameterType == typeof(Vector3) &&
                //         arguments[1].ParameterType == typeof(GroundType) &&
                //         arguments[2].ParameterType == typeof(Location);
                // });
            });
        }
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

        protected static Configuration Config;
        protected static ConfigEntry<bool> IsEnabled;
        protected static string ModName;
        protected static ManualLogSource Log;
        protected static Harmony Harmony;

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

        public static void Awake(Harmony harmony, ConfigFile configFile, ManualLogSource logger)
        {
            BaseSetup(harmony, configFile, logger, nameof(SubModBase));

            // Add more here

            BaseFinish(typeof(SubModBase));
        }
    }