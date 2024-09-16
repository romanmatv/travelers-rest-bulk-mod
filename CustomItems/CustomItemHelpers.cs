using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using CsvHelper;
using HarmonyLib;
using RestlessMods;
using UnityEngine;
// ReSharper disable UseStringInterpolation

namespace CustomItems;

public static class CustomItemHelpers
{
    internal const string BepInExPluginPath = "BepInEx/plugins/";
    internal static ManualLogSource Log;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    internal record struct ModItemLine(
        int id,
        string name,
        FoodType foodType,
        bool canBeUsedAsModifier,
        bool containsAlcohol,
        IngredientType ingredientType, /*IngredientModifier[] modifiers,*/
        int sellPrice,
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
    internal record struct ModRecipeLine(
        int id,
        string name,
        int itemId,
        Recipe.RecipePage page,
        Recipe.RecipeGroup recipeGroup,
        string ingredient1,
        string ingredient2,
        string ingredient3,
        string ingredient4,
        string ingredient5,
        int workstation,
        int fuel,
        int time,
        int outputAmount)
    {
        public string recipeIngredients()
        {
            var results = new[]
                    { ingredient1 ?? "", ingredient2 ?? "", ingredient3 ?? "", ingredient4 ?? "", ingredient5 }
                .Where(string.IsNullOrWhiteSpace);
            return string.Join(',', results);
        }
    }

    public static void SeedMakerRecipes(ref Dictionary<int, Recipe> outDictionary, int input, int outputAmount)
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
                ingredient1 = string.Format("{0} - \"{1}\" ({2})", foodId, food.name.Replace(foodId + " - ", ""), input),
                workstation = 1232,
                fuel = 0,
                time = 30,
                outputAmount = outputAmount
            };


            csv.WriteRecord(recipeLine);
            csv.NextRecord();
        }
    }

    public static void GenerateItemIds(List<FileInfo> fileInfos, ref Dictionary<int, Item> outDictionary)
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
                CultureInfo.InvariantCulture);
            foreach (var modLine in csvReader.GetRecords<ModItemLine>())
            {
                if (modLine.id != 0)
                {
                    result.Add(modLine);
                    continue;
                }

                var newLine = modLine with { id = nextItemId++ };

                outDictionary.Add(newLine.id, AddItem(newLine));
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
        ref List<FileInfo> filesToReviewIds)
    {
        foreach (var file in GetFiles(relativePath))
        {
            if (!file.Name.ToLowerInvariant().Contains("item") || !file.Name.EndsWith(".csv")) continue;

            AddItems(file, ref outDictionary, out var needsIdAssignment);

            if (needsIdAssignment)
            {
                filesToReviewIds.Add(file);
            }
        }
    }

    private static void AddItems(FileInfo fileInfo, ref Dictionary<int, Item> moddedItems, out bool needsIdAssignment)
    {
        needsIdAssignment = false;

        using var csv = new CsvReader(new StreamReader(new MemoryStream(File.ReadAllBytes(fileInfo.FullName))),
            CultureInfo.InvariantCulture);
        foreach (var modLine in csv.GetRecords<ModItemLine>())
        {
            // 0 means the mod needs to generate an id
            if (modLine.id == 0) needsIdAssignment = true;
            else moddedItems.Add(modLine.id, AddItem(modLine));
        }
    }

    private static Item AddItem(ModItemLine modItem)
    {
        return AddItem(
            itemId: modItem.id,
            foodType: modItem.foodType,
            canBeAged: modItem.canBeAged,
            canBeUsedAsModifer: modItem.canBeUsedAsModifier,
            containsAlcohol: modItem.containsAlcohol,
            ingredientType: modItem.ingredientType,
            x: modItem.spriteX,
            y: modItem.spriteY,
            silverPrice: modItem.sellPrice,
            name: modItem.name,
            mustBeAged: modItem.hasToBeAgedMeal,
            spriteSheetName: modItem.spriteSheetName
        );
    }

    private static Item AddItem(int itemId, FoodType foodType, bool canBeAged, bool canBeUsedAsModifer,
        bool containsAlcohol, IngredientType ingredientType, int x, int y, int silverPrice, string name,
        bool mustBeAged, string spriteSheetName)
    {
        var itemDatabaseAccessor = ItemDatabaseAccessor.GetInstance();
        var itemDatabase = ItemDatabaseAccessor.GetDatabaseSO();
        var itemDictionaryField = Traverse.Create(itemDatabaseAccessor)
            .Field(RandomNameHelper.GetField<ItemDatabaseAccessor, Dictionary<int, Item>>().Name);
        var itemDictionary = itemDictionaryField
                                 .GetValue<Dictionary<int, Item>>()
                             // If not initialized, initialize
                             ?? itemDictionaryField
                                 .SetValue(new Dictionary<int, Item>())
                                 .GetValue<Dictionary<int, Item>>();

        if (itemDictionary.TryGetValue(itemId, out var result))
        {
            Log.LogError("Item " + result.nameId + " already exists in Database.");
            return result;
        }


        var tex = CustomSpriteSheets.GetTextureBySpriteSheetName(spriteSheetName);

        // ReSharper disable once Unity.IncorrectScriptableObjectInstantiation
        var item = ScriptableObject.CreateInstance<Food>();

        item.canBeAged = canBeAged;
        item.canBeUsedAsModifier = canBeUsedAsModifer;
        item.containsAlcohol = containsAlcohol;
        item.foodType = foodType;
        item.ingredientType = ingredientType;
        item.modifiers = Array.Empty<IngredientModifier>();
        item.appearsInOrders = true;
        item.sellPrice = new Price() { silver = silverPrice };
        item.translationByID = false;
        item.name = name;
        item.nameId = itemId + " - " + name;
        item.excludedFromTrends = false;
        item.hasToBeAgedMeal = mustBeAged;

        item.canBeSold = true;
        item.held = false;
        item.savedAsAPlaceable = false;

        // todo:
        // item.dirtySprite =
        // item.heldSprite =
        item.ingredientIcon = Sprite.Create(tex, new Rect(x * 33, y * 33, 33, 33), new Vector2(0f, 0f));
        item.icon = Sprite.Create(tex, new Rect(x * 33, y * 33, 33, 33), new Vector2(0f, 0f));
        item.sprite = Sprite.Create(tex, new Rect(x * 33, y * 33, 33, 33), new Vector2(0f, 0f));

        Traverse.Create(item).Field("id").SetValue(itemId);

        itemDatabase.items = itemDatabase.items.AddToArray(item);
        itemDictionary
            .Add(itemId, item);

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

    public static void GenerateRecipeIds(List<FileInfo> fileInfos, ref Dictionary<int, Recipe> outDictionary)
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
            using var csvReader = new CsvReader(
                new StreamReader(new MemoryStream(File.ReadAllBytes(fileInfo.FullName))),
                CultureInfo.InvariantCulture);
            foreach (var modLine in csvReader.GetRecords<ModRecipeLine>())
            {
                if (modLine.id != 0 || modLine.itemId == 0)
                {
                    result.Add(modLine);
                    continue;
                }

                var newLine = modLine with { id = nextRecipeId++ };

                var item = ItemDatabaseAccessor.GetItem(modLine.itemId);

                if (item == null)
                {
                    result.Add(modLine);
                    continue;
                }

                outDictionary.Add(newLine.id, AddRecipe(newLine, item));
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

    public static void AddRecipesFromDir(string relativePath, ref Dictionary<int, Recipe> outDictionary,
        ref List<FileInfo> filesToReviewIds)
    {
        foreach (var file in GetFiles(relativePath))
            if (file.Name.ToLowerInvariant().Contains("recipe") && file.Name.EndsWith(".csv"))
            {
                AddRecipes(file, ref outDictionary, out var needsIdAssignment);
                if (needsIdAssignment)
                {
                    filesToReviewIds.Add(file);
                }
            }
    }

    private static void AddRecipes(FileInfo fileInfo, ref Dictionary<int, Recipe> moddedRecipes,
        out bool needsIdAssignment)
    {
        needsIdAssignment = false;
        using var csv = new CsvReader(new StreamReader(new MemoryStream(File.ReadAllBytes(fileInfo.FullName))),
            CultureInfo.InvariantCulture);
        foreach (var modLine in csv.GetRecords<ModRecipeLine>())
        {
            var item = ItemDatabaseAccessor.GetItem(modLine.itemId);
            if (modLine.id == 0)
                needsIdAssignment = true;
            else if (item != null)
                moddedRecipes.Add(modLine.id, AddRecipe(modLine, item));
        }
    }

    private static Recipe AddRecipe(ModRecipeLine modRecipe, Item item)
    {
        return AddRecipe(item, modRecipe.id, modRecipe.page,
            modRecipe.recipeGroup, modRecipe.recipeIngredients(), modRecipe.workstation,
            modRecipe.fuel,
            new GameDate.Time { mins = modRecipe.time }, modRecipe.outputAmount);
    }

    private static Recipe AddRecipe(Item item, int recipeId, Recipe.RecipePage page, Recipe.RecipeGroup recipeGroup,
        string recipeIngredientStrings, int workstation, int fuel, GameDate.Time time, int outputAmount)
    {
        var recipeDatabaseAccessor = RecipeDatabaseAccessor.GetInstance();
        var recipeDatabase = Traverse.Create(recipeDatabaseAccessor).Field("recipeDatabaseSO")
            .GetValue<RecipeDatabase>();
        var recipeDictionaryField = Traverse.Create(recipeDatabaseAccessor)
            .Field(RandomNameHelper.GetField<RecipeDatabaseAccessor, Dictionary<int, Recipe>>().Name);
        var recipeDictionary = recipeDictionaryField
                                   .GetValue<Dictionary<int, Recipe>>()
                               // If not initialized, initialize
                               ?? recipeDictionaryField
                                   .SetValue(new Dictionary<int, Recipe>())
                                   .GetValue<Dictionary<int, Recipe>>();

        // ReSharper disable once Unity.IncorrectScriptableObjectInstantiation
        var recipe = ScriptableObject.CreateInstance<Recipe>();

        if (recipeDictionary.TryGetValue(recipeId, out var result))
        {
            Log.LogError("Recipe " + result.id + " already exists in Database.");
            return result;
        }


        // optionally there will be an "item name" in the csv just ignore it
        var r = new Regex("""(?<itemId>[-]*\d+) - (\"\D*\" ){0,1}\((?<itemAmount>\d+)\)""", RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(150));

        var recipeIngredientLists = new List<RecipeIngredient>();
        for (var m = r.Match(recipeIngredientStrings); m.Success; m = m.NextMatch())
        {
            recipeIngredientLists.Add(new RecipeIngredient
            {
                amount = int.Parse(m.Groups["itemAmount"].Value),
                item = ItemDatabaseAccessor.GetItem(int.Parse(m.Groups["itemId"].Value)),
            });
        }

        var recipeIngredients = recipeIngredientLists.ToArray();

        recipe.id = recipeId;

        recipe.name = item.name;
        recipe.page = page;
        recipe.recipeGroup = recipeGroup;
        recipe.fuel = fuel;
        recipe.time = time;
        recipe.output = new ItemAmount()
        {
            item = item,
            amount = outputAmount,
        };
        recipe.ingredientsNeeded = recipeIngredients;


        // Add to RecipeDatabase
        recipeDictionary
            .Add(recipe.id, recipe);
        recipeDatabase.recipes = recipeDatabase.recipes.AddToArray(recipe);

        // Add to Crafters
        RecipeDatabaseAccessor
            .GetCraftersList()
            ?.First(crafter => crafter.ID == workstation)
            .recipes
            .Add(recipe);

        // Just make all recipes auto unlocked for now
        RecipesManager.UnlockRecipe(recipe, false);
        // Add to Favorite for ease of use
        RecipesManager.AddFavoriteRecipe(recipe.id);

        return recipe;
    }
}