using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CsvHelper;
using HarmonyLib;
using I2.Loc;
using JetBrains.Annotations;
using RestlessMods;
using UnityEngine;
using UnityEngine.Tilemaps;

// ReSharper disable UseStringInterpolation

namespace CustomItems;

public static class CustomTavernTiles
{
    public static Dictionary<int, DecorationTile> ModdedWalls = new();
    public static Dictionary<string, int> ModdedItemNameToId = new();
    public static Dictionary<int, Recipe> ModdedRecipes = new();

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

    private static TileBase[] TileBasesFromTileTexture(string name, int col = 4, int rows = 4)
    {
        var result = new List<TileBase>();
        var tex = CustomSpriteSheets.GetTextureBySpriteSheetName("rbk-tr-custom-walls/TR_Wallpapers.png", width: 2048,
            height: 2048);

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < col; c++)
            {
                var rect = new Rect(r * 24, c * 24, 24, 24);
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = name;
                tile.sprite = Sprite.Create(tex, rect, Vector2.zero);
                tile.transform = new Matrix4x4();
                // tile.color = Color.clear;
                tile.colliderType = Tile.ColliderType.Grid;
                tile.flags = TileFlags.LockAll;
                tile.gameObject = new GameObject();
                result.Add(tile);
            }
        }

        return result.ToArray();
    }

    [HarmonyPatch(typeof(EditorActionsDBAccessor), "SetUpDatabase")]
    [HarmonyPostfix]
    private static void SetUpDb(EditorActionsDBAccessor __instance)
    {
        _editorActionsDBAccessor = __instance;

        var decorTile = ScriptableObject.CreateInstance<DecorationTile>();
        decorTile.id = 182000;
        decorTile.tileInfo = new TilesInfoBase
        {
            numTilesX = 4,
            numTilesY = 4,
        };
        // TODO: the sprites are loaded by do not work on the map (wall/floors)
        decorTile.tiles = TileBasesFromTileTexture(
            "rbk-tr-custom-walls/TR_Wallpapers.png",
            col: decorTile.tileInfo.numTilesX,
            rows: decorTile.tileInfo.numTilesY
            );
        // decorTile.roofTiles = null;
        // decorTile.floorLimit = new FloorLimit
        decorTile.materialType = MaterialType.Wood;
        decorTile.canAddTrim = true;

        decorTile.editorAction = EditorAction.ChangeDecoWall;
        decorTile.cost = new MoneyMaterials
        {
            mortar = 0,
            nails = 10,
            planks = 0,
            stones = 0,
        };
        
        HarmonyLib.Traverse.Create(decorTile.cost).Field("gold").SetValue(0);
        HarmonyLib.Traverse.Create(decorTile.cost).Field("silver").SetValue(1);
        var moneyCalc = new MoneyCalc(new Price(0, HarmonyLib.Traverse.Create(decorTile.cost).Field("silver").GetValue<int>(), HarmonyLib.Traverse.Create(decorTile.cost).Field("gold").GetValue<int>()))
            {
                Gold = 0,
                Silver = 0
            };

        HarmonyLib.Traverse.Create(decorTile.cost).Field("moneyCalc").SetValue(moneyCalc);
        
        
        
        if (decorTile.tiles is { Length: > 0 } && decorTile.tiles[0] is Tile tDecorTile)
        {
            decorTile.icon = tDecorTile.sprite;
        }

        HarmonyLib.Traverse.Create(decorTile).Field("offset").SetValue(Vector2.zero);
        // var baseTile = ScriptableObject.CreateInstance<ZoneTile>();
        // baseTile.id = -182000;
        var baseTile = decorTile.tiles[0];

        HarmonyLib.Traverse.Create(decorTile).Field("tileBase").SetValue(baseTile);

        
        // var rect = new Rect(0, 0, 24, 24);
        // var tex = CustomSpriteSheets.GetTextureBySpriteSheetName("rbk-tr-custom-walls/TR_Wallpapers.png", width: 2048,
            // height: 2048);

        // LocationTile tileBase = ScriptableObject.CreateInstance<LocationTile>();
        // tileBase.tileSprite = Sprite.Create(tex, rect, Vector2.zero);
        // tileBase.location = Location.Tavern;
        // tileBase.zoneType = ZoneType.Anywhere;
        // tileBase.zoneIndex = ;
        // tileBase.activationIndex = ;

        // var tile = ScriptableObject.CreateInstance<DecorationTile>();
        // var tileTraversal = HarmonyLib.Traverse.Create(tile);
        // tile.materialType = MaterialType.Wood;
        // tile.tileInfo = new TilesInfoBase
        // {
            // numTilesX = 1,
            // numTilesY = 2
        // };
        // tile.floorLimit = new FloorLimit
        // {
            // materialType = MaterialType.Wood,
            // All = tileBase,
        // };
        // tile.canAddTrim = true;
        // tile.tiles = new TileBase[] { tileBase };
        // tile.roofTiles = new RoofTiles
        // {

        // }
        // tileTraversal.Field("offset").SetValue(Vector2.zero);
        // tileTraversal.Field("tileBase").SetValue(tileBase);


        GameDecorationTiles.Add(decorTile.id, decorTile);
    }

    private static Tilemap ModdedTileMap = new Tilemap();

    // [HarmonyPatch(typeof(GameTileMaps), "SetTileAtPosition")]
    // [HarmonyPrefix]
    private static void XX(
        Vector3 __0,
        TileBase __1,
        ref Tilemap __2)
    {
        if (__1  is ZoneTile { id: -182000 })
        {
            __2 = ModdedTileMap;
        }
        
    }
}