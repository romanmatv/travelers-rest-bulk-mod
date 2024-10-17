using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using CsvHelper;
using HarmonyLib;
using RestlessMods;
using UnityEngine;
using UnityEngine.Tilemaps;

// ReSharper disable UseStringInterpolation

namespace CustomItems;

public static class CustomTavernTiles
{
    public static ManualLogSource Log;


    private static EditorActionsDBAccessor _editorActionsDBAccessor;

    private static EditorActionsDBAccessor EditorActionsDBAccessor
    {
        get
        {
            if (_editorActionsDBAccessor == null)
                _editorActionsDBAccessor = RandomNameHelper.GetSelfInstance<EditorActionsDBAccessor>();
            return _editorActionsDBAccessor;
        }
    }

    private static Dictionary<int, DecorationTile> NFHEPGFMNLL;

    private static Dictionary<int, DecorationTile> GameDecorationTiles
    {
        get
        {
            if (NFHEPGFMNLL != null) return NFHEPGFMNLL;

            if (EditorActionsDBAccessor != null)
                NFHEPGFMNLL = RandomNameHelper
                    .GetFieldValue<EditorActionsDBAccessor, Dictionary<int, DecorationTile>>(_editorActionsDBAccessor);

            return NFHEPGFMNLL;
        }
    }


    // [HarmonyPatch(typeof(EditorActionsDBAccessor), "SetUpDatabase")]
    // [HarmonyPostfix]
    private static void ReviewSetUpDb(EditorActionsDBAccessor __instance)
    {
        if (__instance == null) return;
        _editorActionsDBAccessor = __instance;
        var hashSet = new Dictionary<string, bool>();

        foreach (var key in GameDecorationTiles?.Keys)
        {
            var tile = GameDecorationTiles?[key];
            if (tile == null) continue;
            var tileBase = HarmonyLib.Traverse.Create(tile).Field("tileBase").GetValue<TileBase>();
            string size = string.Format("[{0} x {1}]", tile.tileInfo.numTilesX, tile.tileInfo.numTilesY);
            List<string> ss = new List<string>();
            foreach (var subTile in tile.tiles)
            {
                List<string> s = new List<string>();

                if (subTile is Tile t)
                {
                    if (!hashSet.ContainsKey(t.name))
                    {
                        SaveSprite(t.sprite, "tile_" + tile.id + "_" + t.name);
                        hashSet.Add(t.name, true);
                    }

                    s.Add("[" + t.name + " (" + t.sprite.rect.size + ")]");
                }

                ss.Add(string.Join("|", s));
            }


            Log.LogInfo(key + ", " + string.Join(",", ss));
        }
    }

    static Texture2D DuplicateTexture(Texture2D source)
    {
        RenderTexture renderTex = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(source, renderTex);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        Texture2D readableText = new Texture2D(source.width, source.height);
        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableText.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return readableText;
    }

    public static Texture2D CropTexture(Texture2D pSource, int left, int top, int width, int height)
    {
        if (left < 0)
        {
            width += left;
            left = 0;
        }

        if (top < 0)
        {
            height += top;
            top = 0;
        }

        if (left + width > pSource.width)
        {
            width = pSource.width - left;
        }

        if (top + height > pSource.height)
        {
            height = pSource.height - top;
        }

        if (width <= 0 || height <= 0)
        {
            return null;
        }

        Color[] aSourceColor = pSource.GetPixels(0);

        //*** Make New
        Texture2D oNewTex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        //*** Make destination array
        int xLength = width * height;
        Color[] aColor = new Color[xLength];

        int i = 0;
        for (int y = 0; y < height; y++)
        {
            int sourceIndex = (y + top) * pSource.width + left;
            for (int x = 0; x < width; x++)
            {
                aColor[i++] = aSourceColor[sourceIndex++];
            }
        }

        //*** Set Pixels
        oNewTex.SetPixels(aColor);
        oNewTex.Apply();

        //*** Return
        return oNewTex;
    }

    private static void SaveSprite(Sprite sprite, string name)
    {
        Texture2D tex = DuplicateTexture(sprite.texture);
        // HarmonyLib.Traverse.Create(tex).Property("isReadable").SetValue(true);
        Rect r = sprite.textureRect;
        Texture2D subtex = CropTexture(tex, (int)r.x, (int)r.y, (int)r.width, (int)r.height);
        byte[] data = subtex.EncodeToPNG();
        // File.WriteAllBytes (Application.persistentDataPath + "/" + sprite.name + ".png", data);
        File.WriteAllBytes($"BepInEx/plugins/rbk-tr-Output/tiles/{name}.png", data);

        // byte[] wholeData = Array.Empty<byte>();
        // tex.LoadRawTextureData(wholeData);
        // byte[] wholeData = tex.GetRawTextureData();
        // byte[] wholeData = tex.EncodeToPNG();
        // Log.LogInfo("Writing to " + (Application.persistentDataPath + "/whole_" + name + ".png"));
        // File.WriteAllBytes ($"BepInEx/plugins/rbk-tr-Output/whole_${name}.png", wholeData);
        // var text = new Texture2D(tex.width, tex.height, tex.format, false)
        // {
        //     
        //     filterMode = FilterMode.Point,
        //     wrapMode = TextureWrapMode.Clamp,
        // };
        // tex.EncodeToPNG()
        // tex.LoadImage(ToolTextureSheetBytes);
        // tool.icon = Sprite.Create(tex, new Rect(x * 32, ((int)tier * 32), 32, 32), new Vector2(0f, 0f)).;
    }

    enum DecorType
    {
        Wall,
        Floor
    }

    record struct CustomTileBase(
        int? id,
        string name,
        int startX,
        int startY,
        int cols,
        int rows,
        DecorType DecorType,
        string spriteSheetName,
        int? goldCost = 0,
        int? silverCost = 0,
        int? mortarCost = 0,
        int? plankCost = 0,
        int? nailCost = 0,
        int? stoneCost = 0,
        int? customTileSize = 52,
        MaterialType? materialType = MaterialType.None
    );

    private static TileBase[] TileBasesFromTileTexture(CustomTileBase customTile)
    {
        var result = new List<TileBase>();
        var tex = CustomSpriteSheets.GetTextureBySpriteSheetName(customTile.spriteSheetName, width: 2048, height: 2048);


        switch (customTile.DecorType)
        {
            case DecorType.Wall: return WallTexture(customTile, result, tex).ToArray();
            case DecorType.Floor: return FloorTexture(customTile, result, tex).ToArray();
        }

        return result.ToArray();
    }

    private static List<TileBase> FloorTexture(CustomTileBase customTile, List<TileBase> result, Texture2D tex)
    {
        int cnt = 0;
        int tileSize = customTile.customTileSize ?? 52;

        for (int r = customTile.rows - 1; r >= 0; r--)
        {
            for (int c = 1; c < customTile.cols; c++)
            {
                var num = cnt++;
                var rect = new Rect(customTile.startX + c * tileSize, customTile.startY + r * tileSize, tileSize,
                    tileSize);
                WallTile(ref result, customTile.name + "_" + num, tex, rect);
            }

            var n = cnt++;
            var rect2 = new Rect(customTile.startX + 0 * tileSize, customTile.startY + r * tileSize, tileSize,
                tileSize);
            WallTile(ref result, customTile.name + "_" + n, tex, rect2);
        }

        return result;
    }

    private static List<TileBase> WallTexture(CustomTileBase customTile, List<TileBase> result, Texture2D tex)
    {
        /*
         * [ 0,3  1,3 ]
         * [ 0,2  1,2 ]
         * [ 0,1  1,1 ]
         * [ 0,0  1,0 ]
         *
         * start with 1,0 -> 0,0 -> 1,1
         */
        int cnt = 0;
        int tileSize = customTile.customTileSize ?? 52;

        for (int r = 0; r < customTile.rows; r++)
        {
            for (int c = 1; c < customTile.cols; c++)
            {
                var num = cnt++;
                var rect = new Rect(customTile.startX + c * tileSize, customTile.startY + r * tileSize, tileSize,
                    tileSize);
                WallTile(ref result, customTile.name + "_" + num, tex, rect);
            }

            var n = cnt++;
            var rect2 = new Rect(customTile.startX + 0 * tileSize, customTile.startY + r * tileSize, tileSize,
                tileSize);
            WallTile(ref result, customTile.name + "_" + n, tex, rect2);
        }

        return result;
    }

    private static readonly Vector2 MagicNumber = new(0.461538f, 0.461538f);

    private static readonly Matrix4x4 SingleMatrix = new(
        new Vector4(1, 0, 0, 0),
        new Vector4(0, 1, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
    );

    private static readonly Matrix4x4 DoubleItMatrix = new(
        new Vector4(2, 0, 0, 0),
        new Vector4(0, 2, 0, 0),
        new Vector4(0, 0, 2, 0),
        new Vector4(0, 0, 0, 2)
    );

    private static TileBase WallTile(ref List<TileBase> result, string tileName, Texture2D tex, Rect rect)
    {
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = tileName;
        tile.sprite = Sprite.Create(tex, rect, MagicNumber);
        tile.sprite.name = tile.name;
        // tile.color = new Color(1, 1, 1, 1);
        // only wall uses this :/
        tile.transform = rect.width < 50 ? DoubleItMatrix : SingleMatrix;

        tile.colliderType = Tile.ColliderType.Sprite;
        tile.flags = TileFlags.LockColor;
        // todo: maybe update?
        tile.gameObject = new GameObject();
        result.Add(tile);
        // TODO: remove
        //SaveSprite(tile.sprite, "custom/" + tile.sprite.name);
        return tile;
    }

    // private static TileBase[] TileBasesFromTileTextureOld(string filename, int col = 4, int rows = 4, int x = 0,
    //     int y = 0)
    // {
    //     var result = new List<TileBase>();
    //     var tex = CustomSpriteSheets.GetTextureBySpriteSheetName(filename, width: 2048,
    //         height: 2048);
    //
    //     for (var r = 0; r < rows; r++)
    //     {
    //         for (var c = 0; c < col; c++)
    //         {
    //             var rect = new Rect((c + 4 * x) * 25, (r + y) * 25, 25, 25);
    //             var tile = ScriptableObject.CreateInstance<Tile>();
    //             tile.name = "TR_Wallpapers" + ("_" + x + "_" + y) + "_" + (r * 4 + c);
    //             tile.sprite = Sprite.Create(tex, rect, new Vector2(0.461538f, 0.461538f));
    //             tile.sprite.name = tile.name;
    //             //tile.transform = new Matrix4x4();
    //             // tile.color = Color.clear;
    //             // tile.color = new Color(1, 1, 1, 1);
    //             tile.transform = new Matrix4x4(
    //                 new Vector4(2, 0, 0, 0),
    //                 new Vector4(0, 2, 0, 0),
    //                 new Vector4(0, 0, 2, 0),
    //                 new Vector4(0, 0, 0, 2)
    //             );
    //             /*
    //             UnityExplorer.InspectorManager.Inspect(customTile);
    //             UnityExplorer.InspectorManager.Inspect(baseTile);
    //             */
    //
    //             tile.colliderType = Tile.ColliderType.Sprite;
    //             tile.flags = TileFlags.LockColor;
    //             tile.gameObject = new GameObject();
    //             result.Add(tile);
    //             SaveSprite(tile.sprite, "custom/" + tile.sprite.name);
    //         }
    //     }
    //
    //     return result.ToArray();
    // }

    private static void ReadFile(ref int id, FileInfo fileInfo)
    {
        using var csv = new CsvReader(new StreamReader(new MemoryStream(File.ReadAllBytes(fileInfo.FullName))),
            Plugin.Conf);
        IEnumerable<CustomTileBase> records;
        try
        {
            records = csv.GetRecords<CustomTileBase>();
        }
        catch (Exception _)
        {
            Log.LogError(string.Format("tile file '{0}' couldn't be processed (exception: {1})", fileInfo.Name, _.Message));
            return;
        }
        
        LogDebug(string.Format("found file: {0} with {1} customTiles", fileInfo.FullName, records.Count()));

        foreach (var t in records)
        {
            if (t.id is null or 0)
            {
                CreateDecorationTile(id++, t);
            }
            else
            {
                CreateDecorationTile((int)t.id, t);
            }
        }
    }

    [HarmonyPatch(typeof(EditorActionsDBAccessor), "SetUpDatabase")]
    [HarmonyPostfix]
    private static void SetUpDb(EditorActionsDBAccessor __instance)
    {
        _editorActionsDBAccessor = __instance;

        var id = 182000;

        var customTileFiles = new List<FileInfo>();
        
        foreach (var folder in Plugin.DecorTilesFolders)
            CustomItemHelpers.DeepFileSearch(folder, ref customTileFiles,"tile");

        foreach (var file in customTileFiles)
        {
            ReadFile(ref id, file);
        }

        // var ts = new[]
        // {
        //     new CustomTileBase(0, "Sakura 4x4 (small)", 0, 0, 4, 4, DecorType.Wall,
        //         "rbk-tr-custom-walls/TR_Wallpapers.png", customTileSize: 26),
        //     new CustomTileBase(0, "Sakura 2x4 (small)", 0, 0, 2, 4, DecorType.Wall,
        //         "rbk-tr-custom-walls/TR_Wallpapers.png", customTileSize: 26),
        //
        //     new CustomTileBase(0, "Test checker 4x4", 208, 0, 4, 4, DecorType.Floor,
        //         "rbk-tr-custom-walls/TR_Wallpapers.png"),
        //     new CustomTileBase(0, "Test checker 2x2", 208, 0, 2, 2, DecorType.Floor,
        //         "rbk-tr-custom-walls/TR_Wallpapers.png"),
        //
        //     new CustomTileBase(0, "Seashell 2x4 (small)", 0, 104, 2, 4, DecorType.Wall,
        //         "rbk-tr-custom-walls/TR_Wallpapers.png", customTileSize: 26),
        //     new CustomTileBase(0, "Seashell 2x4", 0, 104, 2, 4, DecorType.Wall,
        //         "rbk-tr-custom-walls/TR_Wallpapers.png"),
        // };
        //
        // foreach (var t in ts)
        // {
        //     if (t.id is null or 0)
        //     {
        //         CreateDecorationTile(id++, t);
        //     }
        //     else
        //     {
        //         CreateDecorationTile((int)t.id, t);
        //     }
        // }

        // Foo(ts);
    }

    private static void LogDebug(string format, params object?[] args)
    {
        if (Plugin.DebugIds)
        {
            Log.LogDebug(string.Format(format, args));
        }
    }

    private static void Foo(CustomTileBase[] result)
    {
        using var csvWriter = new CsvWriter(new StreamWriter(CustomItemHelpers.BepInExPluginPath + "wallpapers.csv"),
            CultureInfo.InvariantCulture);
        csvWriter.WriteHeader<CustomTileBase>();
        csvWriter.NextRecord();

        foreach (var modLine in result)
        {
            csvWriter.WriteRecord(modLine);
            csvWriter.NextRecord();
        }
    }

    private static DecorationTile CreateDecorationTile(int id, CustomTileBase __customTileBase)
    {
        LogDebug(string.Format("{1} - {0} [{2}]", __customTileBase.name, __customTileBase.id, id));
        
        var decorTile = ScriptableObject.CreateInstance<DecorationTile>();
        decorTile.id = id;
        decorTile.name = Plugin.DebugIds ? ("[" + id + "] " + __customTileBase.name) : __customTileBase.name;
        decorTile.tileInfo = new TilesInfoBase
        {
            numTilesX = __customTileBase.cols,
            numTilesY = __customTileBase.rows,
        };
        // TODO: the sprites are loaded by do not work on the map (wall/floors)
        decorTile.tiles = TileBasesFromTileTexture(
            __customTileBase
        );
        // todo:
        // decorTile.roofTiles = null;
        // todo:
        // decorTile.floorLimit = new FloorLimit
        decorTile.materialType = __customTileBase.materialType ?? MaterialType.None;
        decorTile.canAddTrim = __customTileBase.DecorType == DecorType.Wall;


        decorTile.editorAction = __customTileBase.DecorType == DecorType.Wall
            ? EditorAction.ChangeDecoWall
            : EditorAction.ChangeDecoFloor;
        decorTile.cost = new MoneyMaterials
        {
            mortar = __customTileBase.mortarCost ?? 0,
            nails = __customTileBase.nailCost ?? 0,
            planks = __customTileBase.plankCost ?? 0,
            stones = __customTileBase.stoneCost ?? 0,
        };

        HarmonyLib.Traverse.Create(decorTile.cost).Field("gold").SetValue(__customTileBase.goldCost);
        HarmonyLib.Traverse.Create(decorTile.cost).Field("silver").SetValue(__customTileBase.silverCost);
        var moneyCalc = new MoneyCalc(new Price(0,
            HarmonyLib.Traverse.Create(decorTile.cost).Field("silver").GetValue<int>(),
            HarmonyLib.Traverse.Create(decorTile.cost).Field("gold").GetValue<int>()))
        {
            Gold = __customTileBase.goldCost ?? 0,
            Silver = __customTileBase.silverCost ?? 0
        };

        HarmonyLib.Traverse.Create(decorTile.cost).Field("moneyCalc").SetValue(moneyCalc);


        if (decorTile.tiles is { Length: > 0 } && decorTile.tiles[0] is Tile tDecorTile)
        {
            decorTile.icon = tDecorTile.sprite;
        }

        HarmonyLib.Traverse.Create(decorTile).Field("offset").SetValue(Vector2.zero);
        HarmonyLib.Traverse.Create(decorTile).Field("tileBase").SetValue(decorTile.tiles[0]);
        GameDecorationTiles.Add(decorTile.id, decorTile);

        return decorTile;
    }

    private static Dictionary<int, bool> barkedGetDecoFails = new();

    [HarmonyPatch(typeof(EditorActionsDBAccessor), "GetDecoTile")]
    [HarmonyPostfix]
    private static void GetDecoTile(ref DecorationTile __result, int __0)
    {
        if (__result != null) return;
        
        if (!barkedGetDecoFails.ContainsKey(__0))
        {
            Log.LogError("Unable to find a tile with id=" + __0 + ", replacing it with fallback of tile id=" +
                         Plugin.fallbackTileId);
            barkedGetDecoFails[__0] = true;
        }

        __result = EditorActionsDBAccessor.GetDecoTile(Plugin.fallbackTileId);
    }
}