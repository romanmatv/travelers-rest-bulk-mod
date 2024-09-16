using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CsvHelper;
using HarmonyLib;
using RestlessMods;
using UnityEngine;
// ReSharper disable UseStringInterpolation

namespace CustomItems;

/* Crafter list (google translated)
    668 - Bank
    670 - Distillery
    672 - Oven
    673 - Malting Machine
    674 - Cheese Factory
    675 - Fermentation Tank
    676 - Mash Barrel
    703 - Sawmill
    704 - Smelting Furnace
    706 - Stone Workshop
    709 - Press
    723 - Cutting Axe
    731 - Stonecutter Work Table
    733 - Mixing Tank
    728 - Blacksmith Table
    1232 - Forage Table
    1240 - Kitchen Table
    1380 - Preserves Table
    1451 - Tackle Table
    1532 - Cocktail Table
         */
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static ManualLogSource _log;
    private static ConfigFile _configFile;

    private static ConfigEntry<bool> Debugging => _configFile.Bind("DebuggingTools", "isEnabled", true,
        "whether or not you want to debug item ids");

    private static ConfigEntry<bool> RecipeTesting => _configFile.Bind("DebuggingTools", "recipeTesting", true,
        "whether or not you want to output the item into a testRecipe.csv file");

    private static ConfigEntry<KeyCode> HotKey => _configFile.Bind("DebuggingTools", "HotKey", KeyCode.F1,
        "Debugging tool: Key to send chest items to output file");

    private static ConfigEntry<bool> CraftAllSeeds => _configFile.Bind("Seed Maker", "isEnabled", false,
        "Whether or not you want seed making recipes generated and put in a file for review.");

    private static ConfigEntry<int> SeedInput => _configFile.Bind("Seed Maker", "input",
        5, "number of food to make seeds");
    private static ConfigEntry<int> SeedOutput => _configFile.Bind("Seed Maker", "output",
        10, "number of seeds to make");

    private static string[] Folders => _configFile.Bind(
        "CustomItems",
        "folders",
        "rbk-tr-CustomItems",
        "| separated list of folders that store Items/Recipes/Sprites, this is relative path inside the BepInEx/plugins folder"
    ).Value.Split('|');
    
    internal static int StartingItemId => _configFile.Bind("CustomItems", "FirstItemId", 182_000,
    "Id to start for custom items (game is using -60 to 99,999 so aim outside of that").Value;
    
    internal static int StartingRecipeId => _configFile.Bind("CustomItems", "FirstRecipeId", 2_000,
    "Id to start for custom recipes (game is using 0 to 1, 551 so aim outside of that").Value;

    internal static ConfigEntry<bool> AssignIds =>
        _configFile.Bind("CustomItems", "AssignIds", false, "If you want the mod to replace id=0 with an unique id not being used enable this.");
    
    

            //
        // // ReSharper disable once Unity.IncorrectMonoBehaviourInstantiation
        // var placeable = new Placeable
        // {
        //     name = "Seed Maker",
        //     rotatable = false,
        //     fourSidesRotation = false,
        //     multipleSkins = false,
        //     canCycleSkin = false,
        //     canCycleWhenSnapped = false,
        //     randomSkin = false,
        //     skinSpriteRenderer = null,
        //     skins = new Sprite[]
        //     {
        //     },
        //     skinsGameObjects = new SkinGameObject[]
        //     {
        //     },
        //     specificRules = SpecificType.None,
        //     placeableAnywhere = false,
        //     isPlaceableOnSurface = true,
        //     onlyInAllowedSurfaces = false,
        //     isPlaceableOnWall = false,
        //     selectAfterPlace = false,
        //     direction = Direction.Up,
        //     validLocations = Location.Tavern,
        //     zoneTypeNeeded = ZoneType.Anywhere,
        //     groundTypeNeeded = GroundType.AllExceptTilledEarth,
        //     // itemSpace = null,
        //     // areaSpace = null,
        //     // snapToGrid = null,
        //     // snapLeftRight = null,
        // };
        //
        // // ReSharper disable once Unity.IncorrectMonoBehaviourInstantiation
        // var seedMaker = new Crafter
        // {
        //     name = "Seed Maker",
        //     placeable = placeable,
        //     multipleCrafting = true,
        //     crafterSprite = null
        // };
        //
        //
        // foreach (var modLine in csv.GetRecords<CustomItemHelpers.ModRecipeLine>())
        // {
        //
        //
        // RecipeDatabaseAccessor.DGMLCMCCDHO.recipeDatabaseSO.crafterLists = RecipeDatabaseAccessor.DGMLCMCCDHO.recipeDatabaseSO.crafterLists.AddItem(seedMaker)
        //     Recipe.RecipePage.
        //
        // new Item()
        // {
        //     category = Category.Crafters,
        //
        // };
        //

    private void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(Plugin));
        _configFile = Config;
        _log = Logger;
        CustomItemHelpers.Log = _log;

        _log.LogDebug(string.Format("custom items start at {0}, custom recipes start at {1}", StartingItemId, StartingRecipeId));
        _log.LogDebug(string.Format("seed-maker {0} ({1} => {2})", CraftAllSeeds.Value, SeedInput.Value, SeedOutput.Value));

        Logger.LogInfo("\thave " + (Folders?.Length ?? 0) + " folder(s) to review");

        // Plugin startup logic
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    private static Dictionary<int, Item> _moddedItems = new();
    private static Dictionary<int, Recipe> _moddedRecipes = new();


    private void Update()
    {
        if (Debugging.Value && Input.GetKeyDown(HotKey.Value))
        {
            var slots = ActionBarInventory.GetPlayer(1).slots;
            var dict = new Dictionary<int, ItemAmount>();

            foreach (var slot in slots)
            {
                if (slot.Stack == 0) continue;

                if (RandomNameHelper.GetField<ItemInstance, Item>().GetValue(slot.itemInstance) is not Item item)
                    continue;

                var id = Traverse.Create(item).Field("id").GetValue<int>();

                if (dict.TryGetValue(id, out var itemAmount))
                {
                    dict[id] = new ItemAmount
                    {
                        item = item,
                        amount = itemAmount.amount + slot.Stack,
                    };
                }
                else
                {
                    dict.Add(id, new ItemAmount
                    {
                        item = item,
                        amount = slot.Stack,
                    });
                }
            }

            var strings = dict.Keys.Select(key => key + " - \"" + dict[key].item.name + "\" (" + dict[key].amount + ")")
                .ToArray();

            foreach (var str in strings)
                _log.LogInfo(str);

            if (RecipeTesting.Value)
            {
                const string testingFile = CustomItemHelpers.BepInExPluginPath + "testRecipe.csv";
                using var csvWriter = new CsvWriter(new StreamWriter(testingFile, true), CultureInfo.InvariantCulture);
                if (!File.Exists(testingFile))
                {
                    csvWriter.WriteHeader<CustomItemHelpers.ModRecipeLine>();
                    csvWriter.NextRecord();
                }

                using var writer = File.AppendText(testingFile);
                csvWriter.WriteRecord(new CustomItemHelpers.ModRecipeLine()
                {
                    ingredient1 = strings.Length > 0 ? strings[0] : "",
                    ingredient2 = strings.Length > 1 ? strings[1] : "",
                    ingredient3 = strings.Length > 2 ? strings[2] : "",
                    ingredient4 = strings.Length > 3 ? strings[3] : "",
                    ingredient5 = strings.Length > 4 ? strings[4] : "",
                });
                csvWriter.NextRecord();
            }
        }
    }


    [HarmonyPatch(typeof(ItemDatabaseAccessor), "Awake")]
    [HarmonyPrefix]
    private static void AddNewItems()
    {
        var filesToReviewIds = new List<FileInfo>();

        foreach (var folder in Folders)
            CustomItemHelpers.AddItemsFromDir(folder, ref _moddedItems, ref filesToReviewIds);
        
        if (AssignIds.Value)
            CustomItemHelpers.GenerateItemIds(filesToReviewIds, ref _moddedItems);
    }

    [HarmonyPatch(typeof(RecipeDatabaseAccessor), "Awake")]
    [HarmonyPrefix]
    private static void AddRecipes()
    {
        var filesToReviewIds = new List<FileInfo>();
        foreach (var folder in Folders)
            CustomItemHelpers.AddRecipesFromDir(folder, ref _moddedRecipes, ref filesToReviewIds);

        if (CraftAllSeeds.Value)
        {
            CustomItemHelpers.SeedMakerRecipes(ref _moddedRecipes, SeedInput.Value, SeedOutput.Value);
            filesToReviewIds.Add(new FileInfo(CustomItemHelpers.BepInExPluginPath + "seedMakerRecipesGenerated.csv"));
        }
        
        // replace 0's as needed
        if (AssignIds.Value)
            CustomItemHelpers.GenerateRecipeIds(filesToReviewIds, ref _moddedRecipes);
    }
}