using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BetterTools;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public partial class Plugin : BaseUnityPlugin
{
    private static Harmony _harmony;
    internal static ConfigEntry<bool> LessenWorkByLevel;
    internal static ConfigEntry<int> MaxLevel;
    internal static ConfigEntry<int> MaxRows;
    static ConfigFile _configFile;

    private const string ModName = "BetterTools";

    private void Awake()
    {
        _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
        _configFile = Config;
        RestlessMods.ModTrigger.Config = Config;

        RestlessMods.ModTrigger.GetModKeyEntry(ModName);
        LessenWorkByLevel = Config.Bind(ModName, "isActive", true,
            "Flag enabling/disabling tool improvement");
        MaxLevel = Config.Bind(ModName, "maxLevel", 40, "Level to 1 hit everything.");
        MaxRows = Config.Bind(ModName, "maxRows", 6, "Max rows target for BetterTools.maxLevel.");
        

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

        Console.Out.WriteLine($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
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
    
    
    private static byte[] _newItems;

    private static Dictionary<string, byte[]> SpriteSheets = new();

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
            if (name.StartsWith("BetterTools.Textures"))
            {
                using var ms = new MemoryStream();
                Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("BetterTools.Textures.Sheet.png")
                    ?.CopyTo(ms);
                image = ms.ToArray();
            }
            else
            {
                image = File.ReadAllBytes(@"BepInEx\plugins\rbk-tr-CustomSprites\" + name);
            }
            SpriteSheets.Add(name, image);

        }
        
        tex.LoadImage(image);
        return tex;
    }


    private static byte[] NewItems
    {
        get
        {
            if (_newItems != null) return _newItems;

            var info = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("BetterTools.Textures.Sheet.png");
            using var ms = new MemoryStream();
            info?.CopyTo(ms);
            _newItems = ms.ToArray();
            return _newItems;
        }
    }
}