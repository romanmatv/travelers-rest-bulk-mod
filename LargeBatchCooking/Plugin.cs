using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace LargeBatchCooking
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<int> _cookBatchSize;
        private static ConfigEntry<bool> _cookTimeMultiplier;
        public static Harmony _harmony;

        private static bool ModTrigger(int PlayerId)
        {
            return PlayerInputs.GetPlayer(PlayerId).GetButton("RightMouseDetect")
                   || PlayerInputs.GetPlayer(PlayerId).GetButton(ActionType.SprintHoldAction);
        }
        
        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            _cookBatchSize = Config.Bind("LargeBatch", "cook batch size", 5,
                "Change the amount of crafts to cook at once");
            _cookTimeMultiplier = Config.Bind("LargeBatch", "bool if increased time is desired", false,
                "Enable Multiplier to change craft time, NOTE: Current the mod cannot keep recipe after day reset so extra materials are lost.");
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        

        private static Dictionary<int, CacheRecipe> _originalRecipes = new Dictionary<int, CacheRecipe>();


        struct CacheRecipe
        {
            public int ID;
            public int Mins;
            public int Hours;
            public int Days;
            public int Weeks;
            public int Years;
            public int Fuel;
            public int OutputAmount;
            public List<int> IngredientCounts;

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
                IngredientCounts = recipe.ingredientsNeeded.Select(ingredient => ingredient.amount).ToList();
            }
        }

        [HarmonyPatch(typeof(GameCraftingUI), "CloseUI")]
        [HarmonyPostfix]
        static void ResetSlots(GameCraftingUI __instance, ref List<RecipeSlot> ___recipeSlots)
        {
            bool largerRecipes = ModTrigger(1);

            if (largerRecipes != true || ___recipeSlots.Count == 0) return;

            foreach (var recipeSlot in ___recipeSlots)
            {
                CacheRecipe original;
                if (_originalRecipes.TryGetValue(recipeSlot.recipe.id, out original) != true)
                {
                    // nothing to default back to
                    continue;
                }

                recipeSlot.recipe.id = original.ID;
                recipeSlot.recipe.time.mins = original.Mins;
                recipeSlot.recipe.time.hours = original.Hours;
                recipeSlot.recipe.time.days = original.Days;
                recipeSlot.recipe.time.weeks = original.Weeks;
                recipeSlot.recipe.time.years = original.Years;
                recipeSlot.recipe.fuel = original.Fuel;
                recipeSlot.recipe.output.amount = original.OutputAmount;

                for (var j = 0; j < original.OutputAmount && j < recipeSlot.recipe.ingredientsNeeded.Length; j++)
                {
                    recipeSlot.recipe.ingredientsNeeded[j].amount = original.IngredientCounts[j];
                }
            }
        }
        [HarmonyPatch(typeof(GameCraftingUI), "SetCrafter")]
        [HarmonyPostfix]
        static void TestSetCrafter(GameCraftingUI __instance, ref List<RecipeSlot> ___recipeSlots)
        {
            bool largerRecipes = ModTrigger(1);

            if (largerRecipes != true || ___recipeSlots.Count == 0) return;
            
            var cookBatchSize = _cookBatchSize.Value;


            foreach (var recipeSlot in ___recipeSlots)
            {
                CacheRecipe original;
                if (!_originalRecipes.TryGetValue(recipeSlot.recipe.id, out original))
                    _originalRecipes.Add(recipeSlot.recipe.id, new CacheRecipe(recipeSlot.recipe));
                
                if (_cookTimeMultiplier.Value)
                {
                    // can't keep recipe past day start not as big of an issue with EndlessLateNights though
                    var cookMins = recipeSlot.recipe.time.mins * cookBatchSize;
                    var cookHours = recipeSlot.recipe.time.hours * cookBatchSize +
                                    Mathf.FloorToInt(cookMins / GameDate.MIN_IN_HOUR);
                    var cookDays = recipeSlot.recipe.time.days * cookBatchSize +
                                   Mathf.FloorToInt(cookHours / GameDate.HOUR_IN_DAY);
                    var cookWeeks = recipeSlot.recipe.time.weeks * cookBatchSize +
                                    Mathf.FloorToInt(cookDays / GameDate.DAY_IN_WEEK);
                    var cookYears = recipeSlot.recipe.time.years * cookBatchSize +
                                    Mathf.FloorToInt(cookWeeks / (GameDate.WEEK_IN_SEASON * 4));

                    recipeSlot.recipe.time.mins = cookMins % GameDate.MIN_IN_HOUR;
                    recipeSlot.recipe.time.hours = cookHours % GameDate.HOUR_IN_DAY;
                    recipeSlot.recipe.time.days = cookDays % GameDate.DAY_IN_WEEK;
                    recipeSlot.recipe.time.weeks = cookWeeks % GameDate.WEEK_IN_SEASON;
                    recipeSlot.recipe.time.years = cookYears;
                }

                for (var j = 0; j < recipeSlot.recipe.ingredientsNeeded.Length; j++)
                {
                    recipeSlot.recipe.ingredientsNeeded[j].amount *= cookBatchSize;
                }

                recipeSlot.recipe.fuel *= cookBatchSize;
                recipeSlot.recipe.output.amount *= cookBatchSize;
            }
        }
    }
}
