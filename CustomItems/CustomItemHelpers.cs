using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using HarmonyLib;
using JetBrains.Annotations;
using RestlessMods;
using UnityEngine;

// ReSharper disable UseStringInterpolation

namespace CustomItems;

public static class CustomItemHelpers
{
    internal const string BepInExPluginPath = "BepInEx/plugins/";
    internal static ManualLogSource Log;

    // "-5 - "Any Bread" (20)","184303 - "Corned Beef" (1)","184304 - "Thousand Island Dressing" (1)","1378 - "Pickles in Vinegar" (1)","-4 - "Any Cheese" (1)"
    // "1x 1272 "Tomato Sauce"","1x 235 - "Garlic"","1x 220 - "Onion"","1x 3055 - "Aromatic Plants"","1x 1291 - "Oil""


    // 10 of any fruit
    // -2 - "any fruit" (10)
    // 20 of any bread with a rye modifier
    // -4 - "any bread" [66 - "rye"] (20)
    // ReSharper disable once InconsistentNaming
    private static readonly Regex Id_Note_Amount = new(
        """(?<itemId>[-]*\d+) - (\"(?<itemName>\D*)\"){0,1}\s*(\[(?<modifierId>[-]*\d+)( - \"(?<modifierName>\D*)\"){0,1}\]){0,1}\s*\((?<itemAmount>\d+)\)""",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(150));

    // 10 of any fruit
    // 10x -2 "any fruit"
    // 20 of any bread with a rye modifier
    // 20x -4 "any bread" [66 "rye"]
    // ReSharper disable once InconsistentNaming
    private static readonly Regex Amount_Id_Note = new(
        """(?<itemAmount>\d+)x (?<itemId>[-]{0,1}\d+)\s*(\"(?<itemName>\D*)\"){0,1}\s*(\[(?<modifierId>\d+)(\s*\"(?<modifierName>\D*)\"){0,1}\]){0,1}""",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(150));

    private static ItemDatabase _itemDatabase;

    private static ItemDatabase ItemDatabase
    {
        get
        {
            if (_itemDatabase == null)
            {
                _itemDatabase = ItemDatabaseAccessor.GetDatabaseSO();
            }

            return _itemDatabase;
        }
    }

    private static Dictionary<int, Item> _itemDictionary;

    private static Dictionary<int, Item> ItemDictionary
    {
        get
        {
            if (_itemDictionary == null)
            {
                var itemDictionaryField = Traverse.Create(ItemDatabaseAccessor.GetInstance())
                    .Field(RandomNameHelper.GetField<ItemDatabaseAccessor, Dictionary<int, Item>>().Name);
                _itemDictionary = itemDictionaryField
                                      .GetValue<Dictionary<int, Item>>()
                                  // If not initialized, initialize
                                  ?? itemDictionaryField
                                      .SetValue(new Dictionary<int, Item>())
                                      .GetValue<Dictionary<int, Item>>();
            }

            return _itemDictionary;
        }
    }

    public static void SeedMakerRecipes(int input, int outputAmount)
    {
        const string seedRecipesFile = BepInExPluginPath + "seedMakerRecipesGenerated.csv";
        if (!File.Exists(seedRecipesFile))
            File.WriteAllText(seedRecipesFile,
                "id,name,itemId,workstation,page,recipeGroup,recipeIngredients,fuel,time,outputAmount\n");

        var items = ItemDatabaseAccessor.GetDatabaseSO().items;

        using var csv = new CsvWriter(new StreamWriter(seedRecipesFile), CultureInfo.InvariantCulture);
        csv.WriteHeader<ModRecipeLine>();
        csv.NextRecord();

        foreach (var possibleFood in items)
        {
            if (possibleFood is not Food { seed: { } seed } food) continue;
            var foodId = Traverse.Create(possibleFood).Field("id").GetValue<int>();
            // if (foodId != 255 && foodId != 277) continue;
            if (string.IsNullOrEmpty(seed.name)) continue;
            var seedId = Traverse.Create(seed).Field("id").GetValue<int>();

            var recipeLine = new ModRecipeLine
            {
                id = 0,
                name = "Harvest " + seed.name.Replace(seedId + " - ", ""),
                itemId = seedId,
                page = Recipe.RecipePage.All,
                recipeGroup = Recipe.RecipeGroup.None,
                ingredient1 =
                    // string.Format("{0} - \"{1}\" ({2})", foodId, food.name.Replace(foodId + " - ", ""), input),
                    string.Format("{2}x {0} \"{1}\"", foodId, food.name.Replace(foodId + " - ", ""), input),
                workstation = 1232,
                fuel = 0,
                time = 30,
                outputAmount = outputAmount
            };

            csv.WriteRecord(recipeLine);
            csv.NextRecord();
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    private static void GenerateItemIds(List<FileInfo> fileInfos, ref Dictionary<int, Item> outDictionary,
        ref Dictionary<string, int> itemNameToIdDictionary, bool replace = false
    )
    {
        if (fileInfos.Count == 0) return;

        var ids = outDictionary.Keys.ToList();
        var nextItemId = Plugin.StartingItemId;


        if (ids.Count > 0)
        {
            ids.Sort();
            nextItemId = ids.Last() + 1;
        }

        foreach (var fileInfo in fileInfos)
        {
            var result = new List<ModItemLine>();
            using var csvReader = new CsvReader(
                new StreamReader(new MemoryStream(File.ReadAllBytes(fileInfo.FullName))),
                Plugin.Conf);
            csvReader.Context.TypeConverterOptionsCache.GetOptions<bool>().BooleanFalseValues.Add("");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<bool>().BooleanFalseValues.Add("NULL");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<bool>().BooleanFalseValues.Add(null);
            foreach (var modLine in csvReader.GetRecords<ModItemLine>())
            {
                if (modLine.id != 0)
                {
                    result.Add(modLine);
                    continue;
                }

                var newLine = modLine with { id = nextItemId++ };

                try
                {
                    outDictionary[newLine.id] = AddItem(newLine, replace);
                    itemNameToIdDictionary[modLine.name] = modLine.id;
                }
                catch (Exception _)
                {
                    Log.LogError(
                        string.Format("item '{0}'{2} couldn't be added (exception: {1})", modLine.name, _.Message,
                            modLine.id));
                }

                result.Add(newLine);
            }

            using var csvWriter = new CsvWriter(new StreamWriter(fileInfo.FullName), CultureInfo.InvariantCulture);
            csvWriter.WriteHeader<ModItemLine>();
            csvWriter.NextRecord();

            foreach (var modLine in result)
            {
                csvWriter.WriteRecord(modLine);
                csvWriter.NextRecord();
            }
        }
    }

    public static void AddItemsFromDir(string relativePath, ref Dictionary<int, Item> outDictionary,
        ref Dictionary<string, int> itemNameToIdDictionary,
        ref List<FileInfo> filesToReviewIds)
    {
        foreach (var file in GetFiles(relativePath))
        {
            if (!file.Name.ToLowerInvariant().Contains("item") || !file.Name.EndsWith(".csv")) continue;

            if (AddItems(file, ref outDictionary, ref itemNameToIdDictionary))
                filesToReviewIds.Add(file);
        }
    }

    public static void AddItems(List<FileInfo> files, bool replace = false)
    {
        var filesToReview = files
            .Where(file => AddItems(file, ref Plugin.ModdedItems, ref Plugin.ModdedItemNameToId, replace))
            .ToList();

        if (Plugin.AssignIds.Value)
            GenerateItemIds(filesToReview, ref Plugin.ModdedItems, ref Plugin.ModdedItemNameToId, replace);
    }

    private static bool AddItems(FileInfo fileInfo, ref Dictionary<int, Item> moddedItems,
        ref Dictionary<string, int> itemNameToIdDictionary, bool replace = false)
    {
        var needsIdAssignment = false;

        using var csv = new CsvReader(new StreamReader(new MemoryStream(File.ReadAllBytes(fileInfo.FullName))),
            Plugin.Conf);
        IEnumerable<ModItemLine> records;
        try
        {
            records = csv.GetRecords<ModItemLine>();
        }
        catch (Exception _)
        {
            Log.LogError(string.Format("item file '{0}' couldn't be processed (exception: {1})", fileInfo.Name,
                _.Message));
            return false;
        }

        foreach (var modLine in records)
            try
            {
                // 0 means the mod needs to generate an id
                if (modLine.id == 0) needsIdAssignment = true;
                else
                {
                    moddedItems[modLine.id] = AddItem(modLine, replace);
                    itemNameToIdDictionary[modLine.name] = modLine.id;
                }
            }
            catch (Exception _)
            {
                Log.LogError(string.Format("item '{0}'{2} couldn't be added (exception: {1})", modLine.name,
                    _.Message,
                    modLine.id));
            }

        return needsIdAssignment;
    }

    private static Item AddItem(ModItemLine modItem, bool replace = false)
    {
        var itemId = modItem.id;
        if (ItemDictionary.TryGetValue(itemId, out var oldItem))
        {
            if (!replace)
            {
                Log.LogError("Item " + oldItem.nameId + " already exists in Database.");
                return oldItem;
            }
        }

        var rect = new Rect(modItem.spriteX * 33, modItem.spriteY * 33, 33, 33);
        var tex = CustomSpriteSheets.GetTextureBySpriteSheetName(modItem.spriteSheetName);
        var price = new Price
        {
            gold = 0,
            silver = modItem.silverCoins,
            copper = modItem.copperCoins,
        };
        var foodType = modItem.foodType ?? FoodType.None;

        // ReSharper disable once Unity.IncorrectScriptableObjectInstantiation
        var item = modItem.foodType == null
            ? ScriptableObject.CreateInstance<Item>()
            : ScriptableObject.CreateInstance<Food>();

        Traverse.Create(item).Field("id").SetValue(itemId);

        item.tags = Array.Empty<Tag>();
        item.appearsInOrders = modItem.appearsInOrders;
        item.price = price;
        item.sellPrice = price;
        item.translationByID = false;
        item.name = modItem.name;
        item.nameId = itemId + " - " + modItem.name;
        item.excludedFromTrends = false;
        item.hasToBeAgedMeal = modItem.hasToBeAgedMeal;
        item.savedAsAPlaceable = false;
        item.icon = Sprite.Create(tex, rect, Vector2.zero);
        item.sprite = Sprite.Create(tex, rect, Vector2.zero);

        // food fields
        if (modItem.foodType != null && item is Food food)
        {
            food.containsAlcohol = modItem.containsAlcohol;
            food.foodType = foodType;
            food.ingredientType = modItem.ingredientType ?? IngredientType.None;
            food.modifiers = Array.Empty<IngredientModifier>();
            food.canBeUsedAsModifier = modItem.isIngredient ?? modItem.canBeUsedAsModifier ?? false;
            food.ingredientIcon = Sprite.Create(tex, rect, Vector2.zero);
            food.canBeAged = modItem.canBeAged;
            food.canBeSold = foodType != FoodType.None;
            food.held = false;
            // TODO: held sprite so lessen log complains
            // food.heldSprite = ScriptableObject.CreateInstance<CharacterSprite>()
            // {
                
            // }
            
            item = food;
        }
        else
        {
            Log.LogInfo(string.Format("item {0} is not food still WIP", item.name));
            item.savedAsAPlaceable = true;
            // 380	Candelabra
            // UnityExplorer.InspectorManager.Inspect(typeof())
        }

        if (replace)
        {
            Log.LogDebug("Attempting to Replace old item " + RandomNameHelper.GetItemIdAndName(oldItem));
            var items = ItemDatabase.items.ToList();
            items.Remove(oldItem);
            ItemDatabase.items = items.ToArray();
        }

        ItemDatabase.items = ItemDatabase.items.AddToArray(item);
        ItemDictionary[itemId] = item;
        
        var terms = Plugin.NewItemTranslations(item, modItem.description);

        Plugin.LanguageSourceDataTermDataDictionary[terms.Item1.Term] = terms.Item1;
        Plugin.LanguageSourceDataTermDataDictionary[terms.Item2.Term] = terms.Item2;

        return item;
    }

    private static FileInfo[] GetFiles(string relativePath)
    {
        var directory = new DirectoryInfo(BepInExPluginPath + relativePath);
        if (!directory.Exists)
        {
            directory.Create();
            directory = new DirectoryInfo(BepInExPluginPath + relativePath);
        }

        return directory.GetFiles();
    }

    private static void GenerateRecipeIds(List<FileInfo> fileInfos, ref Dictionary<int, Recipe> outDictionary,
        ref Dictionary<string, int> itemNameToIdDictionary, bool replace = false)
    {
        if (fileInfos.Count == 0) return;

        var ids = outDictionary.Keys.ToList();
        var nextRecipeId = Plugin.StartingRecipeId;

        if (ids.Count > 0)
        {
            ids.Sort();
            nextRecipeId = ids.Last() + 1;
        }

        foreach (var fileInfo in fileInfos)
        {
            var result = new List<ModRecipeLine>();
            using var csvReader =
                new CsvReader(new StreamReader(new MemoryStream(File.ReadAllBytes(fileInfo.FullName))), Plugin.Conf);
            csvReader.Context.TypeConverterCache.AddConverter(new BoolConverter());
            IEnumerable<ModRecipeLine> records;
            try
            {
                records = csvReader.GetRecords<ModRecipeLine>();
            }
            catch (Exception _)
            {
                Log.LogError(string.Format("recipe file '{0}' couldn't be processed (exception: {1})",
                    fileInfo.Name,
                    _.Message));
                return;
            }

            foreach (var modLine in records)
            {
                if (modLine.id != 0)
                {
                    result.Add(modLine);
                    continue;
                }

                var itemId = modLine.itemId;
                if (modLine.itemId == 0)
                {
                    if (itemNameToIdDictionary.TryGetValue(modLine.name, out var dictionaryNameId))
                    {
                        itemId = dictionaryNameId;
                    }
                    else
                    {
                        Log.LogError(string.Format("Could not find item based on name: {0} for a recipe",
                            modLine.name));
                    }
                }

                var newLine = modLine with { id = nextRecipeId++ };

                var item = ItemDatabaseAccessor.GetItem(itemId);

                if (item == null)
                {
                    result.Add(modLine);
                    continue;
                }

                try
                {
                    outDictionary[newLine.id] = AddRecipe(newLine, item, ref itemNameToIdDictionary, replace);
                }
                catch (Exception _)
                {
                    Log.LogError(string.Format("recipe '{0}' couldn't be added (exception: {1})", modLine.name,
                        _.Message));
                }

                result.Add(newLine);
            }

            using var csvWriter = new CsvWriter(new StreamWriter(fileInfo.FullName), CultureInfo.InvariantCulture);
            csvWriter.WriteHeader<ModRecipeLine>();
            csvWriter.NextRecord();

            foreach (var modLine in result)
            {
                csvWriter.WriteRecord(modLine);
                csvWriter.NextRecord();
            }
        }
    }

    private class BoolConverter : TypeConverter<bool>
    {
        public override bool ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return !string.IsNullOrWhiteSpace(text) && bool.Parse(text);
        }

        public override string ConvertToString(bool value, IWriterRow row, MemberMapData memberMapData)
        {
            return value.ToString();
        }
    }

    public static void AddRecipesFromDir(string relativePath, ref Dictionary<int, Recipe> outDictionary,
        ref Dictionary<string, int> itemNameToIdDictionary,
        ref List<FileInfo> filesToReviewIds)
    {
        foreach (var file in GetFiles(relativePath))
            if (file.Name.ToLowerInvariant().Contains("recipe") && file.Name.EndsWith(".csv"))
            {
                if (AddRecipes(file, ref outDictionary, ref itemNameToIdDictionary))
                    filesToReviewIds.Add(file);
            }
    }

    public static void DeepFileSearch(string folderPath, ref List<FileInfo> result, string fileGroup)
    {
        DeepFileSearch(new DirectoryInfo(BepInExPluginPath + folderPath), ref result, fileGroup);
    }
    
    // ReSharper disable once MemberCanBePrivate.Global
    public static void DeepFileSearch(DirectoryInfo directoryInfo, ref List<FileInfo> result, string fileGroup)
    {
        if (!directoryInfo.Exists) return;

        foreach (var directory in directoryInfo.GetDirectories())
        {
            DeepFileSearch(directory, ref result, fileGroup);
        }
        
        result.AddRange(directoryInfo.GetFiles()
            .Where(fileInfo => fileInfo.Name.ToLowerInvariant().Contains(fileGroup) &&
                               fileInfo.Extension.ToLowerInvariant() == ".csv"));
    }

    public static void ReloadFile(string fileName)
    {
        var itemFiles = new List<FileInfo>();
        var recipeFiles = new List<FileInfo>();

        var dir = new DirectoryInfo(BepInExPluginPath + fileName);
        if (dir.Exists)
        {
            Log.LogInfo("Reloading a Directory digging in.");
            DeepFileSearch(dir, ref itemFiles, "item");
            DeepFileSearch(dir, ref recipeFiles, "recipe");
        }
        else
        {
            var fileInfo = new FileInfo(BepInExPluginPath + fileName);
            if (!fileInfo.Exists || !fileInfo.Extension.ToLowerInvariant().Contains(".csv"))
            {
                Log.LogError(string.Format("file '{0}' was not found.", fileName));
                return;
            }

            if (fileInfo.Name.ToLowerInvariant().Contains("item"))
            {
                itemFiles.Add(fileInfo);
            }
            else if (fileInfo.Name.ToLowerInvariant().Contains("recipe"))
            {
                recipeFiles.Add(fileInfo);
            }
            else
            {
                Log.LogError("idk what this file is " + fileName);
                return;
            }
        }

        var itemsToGenerateIds = new List<FileInfo>();
        foreach (var itemFile in itemFiles)
        {
            if (AddItems(itemFile, ref Plugin.ModdedItems, ref Plugin.ModdedItemNameToId, true))
                itemsToGenerateIds.Add(itemFile);
        }

        GenerateItemIds(itemsToGenerateIds, ref Plugin.ModdedItems, ref Plugin.ModdedItemNameToId, true);


        var recipesToGenerateIds = new List<FileInfo>();
        foreach (var recipeFile in recipeFiles)
        {
            if (AddRecipes(recipeFile, ref Plugin.ModdedRecipes, ref Plugin.ModdedItemNameToId, true))
                recipesToGenerateIds.Add(recipeFile);
        }

        /*
        using CustomItems;
        CustomItems.CustomItemHelpers.ReloadFile("rbk-tr-CustomItems/addonRecipes.csv");
        */
        GenerateRecipeIds(recipesToGenerateIds, ref Plugin.ModdedRecipes, ref Plugin.ModdedItemNameToId, true);
    }

    public static void AddRecipes(List<FileInfo> files, bool replace = false)
    {
        var filesToReview = files
            .Where(file => AddRecipes(file, ref Plugin.ModdedRecipes, ref Plugin.ModdedItemNameToId, replace))
            .ToList();

        if (Plugin.AssignIds.Value)
            GenerateRecipeIds(filesToReview, ref Plugin.ModdedRecipes, ref Plugin.ModdedItemNameToId, replace);
    }

    private static bool AddRecipes(FileInfo fileInfo, ref Dictionary<int, Recipe> moddedRecipes,
        ref Dictionary<string, int> itemNameToIdDictionary, bool replace = false)
    {
        var needsIdAssignment = false;
        using var csv = new CsvReader(new StreamReader(new MemoryStream(File.ReadAllBytes(fileInfo.FullName))),
            Plugin.Conf);

        IEnumerable<ModRecipeLine> records;
        try
        {
            records = csv.GetRecords<ModRecipeLine>();
        }
        catch (Exception _)
        {
            Log.LogError(string.Format("recipe file '{0}' couldn't be processed (exception: {1})", fileInfo.Name,
                _.Message));
            return false;
        }

        foreach (var modLine in records)
            try
            {
                var itemId = modLine.itemId;
                if (modLine.itemId == 0)
                {
                    if (itemNameToIdDictionary.TryGetValue(modLine.name, out var dictionaryNameId))
                    {
                        itemId = dictionaryNameId;
                    }
                    else
                    {
                        Log.LogError(string.Format("Could not find item based on name: {0} for a recipe",
                            modLine.name));
                    }
                }

                var item = ItemDatabaseAccessor.GetItem(itemId);
                if (modLine.id == 0)
                    needsIdAssignment = true;
                else if (item != null)
                    moddedRecipes[modLine.id] = AddRecipe(modLine, item, ref itemNameToIdDictionary, replace);
            }
            catch (Exception _)
            {
                Log.LogError(string.Format("recipe '{0}' couldn't be added (exception: {1})", modLine.name,
                    _.Message));
            }

        return needsIdAssignment;
    }

    private static void GetRecipeDatabaseAndDict(out RecipeDatabase recipeDatabase,
        out Dictionary<int, Recipe> recipeDictionary)
    {
        var recipeDatabaseAccessor = RecipeDatabaseAccessor.GetInstance();
        recipeDatabase = Traverse.Create(recipeDatabaseAccessor).Field("recipeDatabaseSO")
            .GetValue<RecipeDatabase>();
        var recipeDictionaryField = Traverse.Create(recipeDatabaseAccessor)
            .Field(RandomNameHelper.GetField<RecipeDatabaseAccessor, Dictionary<int, Recipe>>().Name);
        recipeDictionary = recipeDictionaryField
                               .GetValue<Dictionary<int, Recipe>>()
                           // If not initialized, initialize
                           ?? recipeDictionaryField
                               .SetValue(new Dictionary<int, Recipe>())
                               .GetValue<Dictionary<int, Recipe>>();
    }

    private static Dictionary<int, Recipe> _placeHolderRecipeDictionary;
    private static RecipeDatabase _placeHolderRecipeDatabase;

    public static RecipeDatabase RecipeDatabase
    {
        get
        {
            if (_placeHolderRecipeDatabase == null)
            {
                GetRecipeDatabaseAndDict(out _placeHolderRecipeDatabase, out _placeHolderRecipeDictionary);
            }

            return _placeHolderRecipeDatabase;
        }
        set => _placeHolderRecipeDatabase = value;
    }

    public static Dictionary<int, Recipe> RecipeDictionary
    {
        get
        {
            if (_placeHolderRecipeDictionary == null)
            {
                GetRecipeDatabaseAndDict(out _placeHolderRecipeDatabase, out _placeHolderRecipeDictionary);
            }

            return _placeHolderRecipeDictionary;
        }
        set => _placeHolderRecipeDictionary = value;
    }

    private static void RemoveRecipe(Recipe recipe)
    {
        var recipes = RecipeDatabase.recipes.ToList();
        recipes.Remove(recipe);
        RecipeDatabase.recipes = recipes.ToArray();

        foreach (var recipeList in RecipeDatabase.crafterLists)
        {
            if (recipeList.recipes.Remove(recipe))
                Log.LogDebug("Removed recipe " + recipe.id + " from crafterID " + recipeList.ID);
        }
    }

    private static Recipe AddRecipe(ModRecipeLine modRecipe, Item item,
        ref Dictionary<string, int> itemNameToIdDictionary, bool replace = false)
    {
        if (RecipeDictionary.TryGetValue(modRecipe.id, out var oldRecipe))
        {
            if (!replace)
            {
                Log.LogError("663 - Recipe " + oldRecipe.id + " already exists in Database.");
                return oldRecipe;
            }
        }

        var recipe = ScriptableObject.CreateInstance<Recipe>();


        recipe.id = modRecipe.id;

        recipe.name = item.name;
        recipe.page = modRecipe.page;
        recipe.recipeGroup = modRecipe.recipeGroup;
        recipe.fuel = modRecipe.fuel;
        recipe.time = new GameDate.Time { mins = modRecipe.time };
        recipe.modiferNeeded = Array.Empty<IngredientType>();
        recipe.modiferTypes = Array.Empty<IngredientType>();
        recipe.output = new ItemAmount
        {
            item = item,
            amount = modRecipe.outputAmount
        };
        recipe.ingredientsNeeded = ReadRecipeIngredients(modRecipe, itemNameToIdDictionary).ToArray();

        if (modRecipe.dropModifiers == true)
        {
            Log.LogDebug("drop mod for item: " + RandomNameHelper.GetItemIdAndName(item) + " using old recipe system.");
            recipe.usingNewRecipesSystem = false;
        }


        if (replace && oldRecipe != null)
        {
            Log.LogDebug("Attempting to Replace old recipe " + oldRecipe.name);
            RemoveRecipe(oldRecipe);
        }

        // Add to RecipeDatabase
        RecipeDictionary[recipe.id] = recipe;
        RecipeDatabase.recipes = RecipeDatabase.recipes.AddToArray(recipe);

        // only add recipe stuff for mod created items without a recipe set
        if (itemNameToIdDictionary.ContainsKey(item.name) &&
            ItemDictionary.Remove(RandomNameHelper.GetItemId(item), out var itemInDB))
        {
            // updating item in db with recipe info
            if (itemInDB.recipe == null)
                itemInDB.recipe = recipe;
            ItemDictionary.Add(RandomNameHelper.GetItemId(item), itemInDB);
        }

        // Add to Crafters
        RecipeDatabaseAccessor
            .GetCraftersList()
            ?.First(crafter => crafter.ID == modRecipe.workstation)
            .recipes
            .Add(recipe);

        // Just make all recipes auto unlocked for now
        RecipesManager.UnlockRecipe(recipe, false);

        return recipe;
    }

    private static List<RecipeIngredient> ReadRecipeIngredients(ModRecipeLine modRecipe,
        Dictionary<string, int> itemNameToIdDictionary)
    {
        // optionally there will be an "item name" in the csv just ignore it
        var r = new Regex("""(?<itemAmount>\d+)x [-]{0,1}\d+""").Match(modRecipe.recipeIngredients()).Success
            ? Amount_Id_Note
            : Id_Note_Amount;

        var recipeIngredientLists = new List<RecipeIngredient>();
        for (var m = r.Match(modRecipe.recipeIngredients()); m.Success; m = m.NextMatch())
        {
            var itemId = int.Parse(m.Groups["itemId"].Value);
            if (itemId == 0 && m.Groups["itemName"].Success)
            {
                if (itemNameToIdDictionary.TryGetValue(m.Groups["itemName"].Value, out var foundId))
                {
                    itemId = foundId;
                }
            }

            int.TryParse(m.Groups["modifierId"].Value, out int modifierId);
            ItemDictionary.TryGetValue(modifierId, out var modifier);

            recipeIngredientLists.Add(new RecipeIngredient
            {
                amount = int.Parse(m.Groups["itemAmount"].Value),
                item = ItemDatabaseAccessor.GetItem(itemId),
                mod = modifier
            });
        }

        return recipeIngredientLists;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    // ReSharper disable once MemberCanBePrivate.Global
    internal record struct ModItemLine(
        int id,
        string name,
        [CanBeNull]
        string description,
        FoodType? foodType,
        bool? isIngredient,
        bool? canBeUsedAsModifier,
        bool containsAlcohol,
        IngredientType? ingredientType, /*IngredientModifier[] modifiers,*/
        int silverCoins,
        int copperCoins,
        bool canBeAged,
        bool hasToBeAgedMeal,
        bool appearsInOrders,
        bool excludedFromTrends,
        string spriteSheetName,
        int spriteX,
        int spriteY);

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    // ReSharper disable once MemberCanBePrivate.Global
    internal record struct ModRecipeLine(
        int id,
        string name,
        int itemId,
        Recipe.RecipePage page,
        Recipe.RecipeGroup recipeGroup,
        // ingredient is a string with ingredient (id, amount) and optional [modifier] and optional [ingredient_name, modifier_name]
        string ingredient1,
        string ingredient2,
        string ingredient3,
        string ingredient4,
        string ingredient5,
        int workstation,
        bool? dropModifiers,
        int fuel,
        int time,
        int outputAmount)
    {
        public string recipeIngredients()
        {
            var results = new[]
                    { ingredient1, ingredient2, ingredient3, ingredient4, ingredient5 }
                .Where(x => !string.IsNullOrWhiteSpace(x));
            return string.Join('|', results);
        }
    }
}