using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using RestlessMods;
using UnityEngine;

namespace LargeBatchCooking;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : ModBase
{
    private static ConfigEntry<int> _cookBatchSize;
    private static ConfigEntry<bool> _cookTimeMultiplier;

    private const string ModName = PluginInfo.PLUGIN_NAME;

    //     // ReInput. button 11 == left joystick idk
    //
    //     /*
    //      * L3 11
    //      * R3 12
    //      */
    //
    //     /*
    //      * button 0 a (4)
    //      * button 1 b (5)
    //      * button 2 x (6)
    //      * button 3 y (7)
    //      * button 4 l1 (10)
    //      * button 5 r1 (11)
    //      * l2(12) r2(13)
    //      * button 6 select (14)
    //      * button 7 start (13)
    //      * button 8 talk (15)
    //      * button 9 screenshot (16)
    //      * button 10 windows (17)
    //      * button 11 left stick(18)
    //      * button 12 right stick(19)
    //      * button 13 up
    //      * button 14 right (21)
    //      * button 15 down
    //      * button 16 left (23)
    //      */
    // }

    private void Awake()
    {
        Setup(typeof(Plugin), PluginInfo.PLUGIN_NAME);
            
        _cookBatchSize = Config.Bind("LargeBatch", "multiplierSize", 5,
            "Change the amount of crafts to cook per modClick");
        _cookTimeMultiplier = Config.Bind("LargeBatch", "bool if increased time is desired", false,
            "Enable Multiplier to change craft time, NOTE: Current the mod cannot keep recipe after day reset so extra materials are lost.");

        SubModBase.NewModTrigger(ModName, Config, Log);
            
        // Plugin startup logic
        Console.Out.WriteLine($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }


    private struct CacheRecipe
    {
        public int ID;
        public int Mins;
        public int Hours;
        public int Days;
        public int Weeks;
        public int Years;
        public int Fuel;
        public int OutputAmount;
        public List<Tuple<int, int>> IngredientRequirement;

        public CacheRecipe(Recipe recipe)
        {
            ID = recipe.id;
            Mins = recipe.time.mins;
            Hours = recipe.time.hours;
            Days = recipe.time.days;
            Weeks = recipe.time.weeks;
            Years = recipe.time.years;
            Fuel = recipe.fuel;
            OutputAmount = recipe.output.amount;
            IngredientRequirement = recipe.ingredientsNeeded
                .Select(ingredient => Tuple.Create(ingredient.item.GetHashCode(), ingredient.amount)).ToList();
        }
    }

    private static readonly Dictionary<Recipe.RecipeGroup, Dictionary<int, CacheRecipe>> RecipeCache =
        new();

    [HarmonyPatch(typeof(RecipeDatabaseAccessor), "Awake")]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    public static void SaveBaseRecipes(RecipeDatabaseAccessor __instance, RecipeDatabase ___recipeDatabaseSO)
    {
        foreach (var recipe in ___recipeDatabaseSO.recipes)
        {
            AddRecipe(recipe);
        }
    }

    private static void AddRecipe(Recipe recipe)
    {
        if (!RecipeCache.ContainsKey(recipe.recipeGroup))
            RecipeCache.Add(recipe.recipeGroup, new());

        if (!RecipeCache[recipe.recipeGroup].ContainsKey(recipe.id))
            RecipeCache[recipe.recipeGroup].Add(recipe.id, new CacheRecipe(recipe));
    }

    static bool GetOriginal(Recipe recipe, out CacheRecipe original)
    {
        original = default;

        return recipe != null && RecipeCache.TryGetValue(recipe.recipeGroup, out var recipeGroupDict) &&
               recipeGroupDict.TryGetValue(recipe.id, out original);
    }

    [HarmonyPatch(typeof(Crafter), "Awake")]
    [HarmonyPostfix]
    static void CrafterAwake(Crafter __instance)
    {
        __instance.multipleCrafting = true;
    }

    [HarmonyPatch(typeof(RandomOrderQuestsManager), nameof(RandomOrderQuestsManager.CreateQuest))]
    [HarmonyPrefix]
    static void CheckQuestsNotBroken(RandomOrderQuestsManager __instance, ref RandomOrderQuestInfo __0)
    {
        ResetRecipe(__0.item.recipe);
        __0.requiredAmount = __0.item.recipe.output.amount;
    }

    [HarmonyPatch(typeof(Crafter), nameof(Crafter.GetAreaBonifications))]
    [HarmonyPostfix]
    static void AdjustAreaBonifications(Crafter __instance, ref float __result, AreaBonificationType __0,
        Recipe __1)
    {
        if (__0 == AreaBonificationType.TimeReduction) return;

        if (!GetOriginal(__1, out var original)) return;

        var recipeMultiplier = (float)Math.Round((float)__1.output.amount / original.OutputAmount);
        if (recipeMultiplier <= 1) return;

        __result *= recipeMultiplier;
    }

    [HarmonyPatch(typeof(GameCraftingUI), "CloseUI")]
    [HarmonyPrefix]
    static void ResetSlots(GameCraftingUI __instance, ref List<RecipeSlot> ___recipeSlots)
    {
        if (SubModBase.ModTrigger(ModName, 1) || ___recipeSlots.Count == 0) return;

        foreach (var recipeSlot in ___recipeSlots)
            ResetRecipe(recipeSlot.recipe);
    }

    private static void ResetRecipe(Recipe recipe)
    {
        if (!GetOriginal(recipe, out var original)) return;

        recipe.id = original.ID;
        recipe.time.mins = original.Mins;
        recipe.time.hours = original.Hours;
        recipe.time.days = original.Days;
        recipe.time.weeks = original.Weeks;
        recipe.time.years = original.Years;
        recipe.fuel = original.Fuel;
        recipe.output.amount = original.OutputAmount;

        for (var index = 0; index < recipe.ingredientsNeeded.Length; index++)
        {
            var neededIngredientId = recipe.ingredientsNeeded[index].item.GetHashCode();
            var originalReq = original.IngredientRequirement.Find(ingredientReq =>
                ingredientReq.Item1 == neededIngredientId);
            recipe.ingredientsNeeded[index].amount = originalReq.Item2;
        }
    }

    [HarmonyPatch(typeof(GameCraftingUI), "SetCrafter")]
    [HarmonyPostfix]
    [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
    static void TestSetCrafter(GameCraftingUI __instance, ref List<RecipeSlot> ___recipeSlots)
    {
        var largerRecipes = SubModBase.ModTrigger(ModName, 1);

        if (largerRecipes != true || ___recipeSlots.Count == 0) return;

        foreach (var recipeSlot in ___recipeSlots)
        {
            var cookBatchSize = _cookBatchSize.Value;

            if (!GetOriginal(recipeSlot.recipe, out var original))
            {
                Log.LogWarning(
                    $"{recipeSlot.recipe.name} not cached {recipeSlot.recipe.recipeGroup.ToString()}");
                AddRecipe(recipeSlot.recipe);

                continue;
            }
            // var recipeBook = pullRecipeBook(recipeSlot.recipe.output.item.category);
            //
            // if (!recipeBook.TryGetValue(recipeSlot.recipe.id, out var original))
            // {
            //     recipeBook.Add(recipeSlot.recipe.id, new CacheRecipe(recipeSlot.recipe));
            // }


            if (recipeSlot.recipe.output.amount == original.OutputAmount)
                cookBatchSize -= 1;
                
            if (_cookTimeMultiplier.Value)
            {
                // can't keep recipe past day start not as big of an issue with EndlessLateNights though
                var cookMins = recipeSlot.recipe.time.mins + original.Mins * cookBatchSize;
                var cookHours = recipeSlot.recipe.time.hours + original.Hours * cookBatchSize +
                                Mathf.FloorToInt(cookMins / GameDate.MIN_IN_HOUR);
                var cookDays = recipeSlot.recipe.time.days + original.Days * cookBatchSize +
                               Mathf.FloorToInt(cookHours / GameDate.HOUR_IN_DAY);
                var cookWeeks = recipeSlot.recipe.time.weeks + original.Weeks * cookBatchSize +
                                Mathf.FloorToInt(cookDays / GameDate.DAY_IN_WEEK);
                var cookYears = recipeSlot.recipe.time.years + original.Years * cookBatchSize +
                                Mathf.FloorToInt(cookWeeks / (GameDate.WEEK_IN_SEASON * 4));

                recipeSlot.recipe.time.mins = cookMins % GameDate.MIN_IN_HOUR;
                recipeSlot.recipe.time.hours = cookHours % GameDate.HOUR_IN_DAY;
                recipeSlot.recipe.time.days = cookDays % GameDate.DAY_IN_WEEK;
                recipeSlot.recipe.time.weeks = cookWeeks % GameDate.WEEK_IN_SEASON;
                recipeSlot.recipe.time.years = cookYears;
            }

            for (var index = 0; index < recipeSlot.recipe.ingredientsNeeded.Length; index++)
            {
                var neededIngredientId = recipeSlot.recipe.ingredientsNeeded[index].item.GetHashCode();
                var originalReq = original.IngredientRequirement.Find(ingredientReq =>
                    ingredientReq.Item1 == neededIngredientId);
                recipeSlot.recipe.ingredientsNeeded[index].amount += originalReq.Item2 * cookBatchSize;
            }
                
            // for (var j = 0; j < recipeSlot.recipe.ingredientsNeeded.Length; j++)
            // {
            // recipeSlot.recipe.ingredientsNeeded[j].amount += original.cookBatchSize;
            // }

            recipeSlot.recipe.fuel += original.Fuel * cookBatchSize;
            recipeSlot.recipe.output.amount += original.OutputAmount * cookBatchSize;
        }
    }
}