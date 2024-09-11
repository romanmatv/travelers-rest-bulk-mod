using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using RestlessMods;
using UnityEngine;

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

    private static string[] Folders => _configFile.Bind(
        "CustomItems",
        "folders",
        "rbk-tr-CustomItems",
        "| separated list of folders that store Items/Recipes/Sprites, this is relative path inside the BepInEx/plugins folder"
    ).Value.Split('|');

    private void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(Plugin));
        _configFile = Config;
        _log = Logger;
        CustomItemHelpers.Log = _log;

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

                var id = HarmonyLib.Traverse.Create(item).Field("id").GetValue<int>();

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
                if (!File.Exists(testingFile))
                    File.WriteAllText(testingFile,
                        "id,name,itemId,workstation,page,recipeGroup,recipeIngredients,fuel,time,outputAmount\n");

                using var writer = File.AppendText(testingFile);
                writer.WriteLine(",,,,,,{0},,,", string.Join('|', strings));
            }
        }
    }


    [HarmonyPatch(typeof(ItemDatabaseAccessor), "Awake")]
    [HarmonyPrefix]
    private static void AddNewItems()
    {
        foreach (var folder in Folders)
            CustomItemHelpers.AddItemsFromDir(folder, ref _moddedItems);
    }

    [HarmonyPatch(typeof(RecipeDatabaseAccessor), "Awake")]
    [HarmonyPrefix]
    private static void AddRecipes()
    {
        foreach (var folder in Folders)
            CustomItemHelpers.AddRecipesFromDir(folder, ref _moddedRecipes);
    }
}