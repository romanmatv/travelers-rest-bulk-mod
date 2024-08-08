using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace LargeBatchCooking
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<int> _cookBatchSize;
        public static Harmony _harmony;
        internal static ManualLogSource Log;

        
        private void Awake()
        {
            Log = Logger;
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            _cookBatchSize = Config.Bind("LargeBatch", "cook batch size", 5,
                "Change the amount of crafts to cook at once");
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        

        private static Dictionary<int, CacheRecipe> _originalRecipes = new Dictionary<int, CacheRecipe>();


        struct CacheRecipe
        {
            public int id;
            public int mins;
            public int hours;
            public int days;
            public int weeks;
            public int years;
            public int fuel;
            public int outputAmount;
            public List<int> ingredientCounts;

            public CacheRecipe(Recipe recipe)
            {
                id = recipe.id;
                mins = recipe.time.mins;
                hours = recipe.time.hours;
                days = recipe.time.days;
                weeks = recipe.time.weeks;
                years = recipe.time.years;
                fuel = recipe.fuel;
                outputAmount = recipe.output.amount;
                ingredientCounts = recipe.ingredientsNeeded.Select(ingredient => ingredient.amount).ToList();
            }
        }

        [HarmonyPatch(typeof(GameCraftingUI), "CloseUI")]
        [HarmonyPostfix]
        static void resetSlots(GameCraftingUI __instance, ref List<RecipeSlot> ___recipeSlots)
        {
            foreach (var recipeSlot in ___recipeSlots)
            {
                CacheRecipe original;
                if (_originalRecipes.TryGetValue(recipeSlot.recipe.id, out original) != true)
                {
                    // nothing to default back to
                    continue;
                }
                Log.LogInfo($"undo {recipeSlot.recipe.id.ToString()}: {original.outputAmount.ToString()}");


                recipeSlot.recipe.id = original.id;
                recipeSlot.recipe.time.mins = original.mins;
                recipeSlot.recipe.time.hours = original.hours;
                recipeSlot.recipe.time.days = original.days;
                recipeSlot.recipe.time.weeks = original.weeks;
                recipeSlot.recipe.time.years = original.years;
                recipeSlot.recipe.fuel = original.fuel;
                recipeSlot.recipe.output.amount = original.outputAmount;

                for (var j = 0; j < original.outputAmount; j++)
                {
                    recipeSlot.recipe.ingredientsNeeded[j].amount = original.ingredientCounts[j];
                }
            }
        }
        [HarmonyPatch(typeof(GameCraftingUI), "SetCrafter")]
        [HarmonyPostfix]
        static void testSetCrafter(GameCraftingUI __instance, ref List<RecipeSlot> ___recipeSlots)
        {
            bool largerRecipes = PlayerInputs.GetPlayer(1).GetButton("RightMouseDetect");

            if (largerRecipes != true || ___recipeSlots.Count == 0) return;
            
            var cookBatchSize = _cookBatchSize.Value;


            foreach (var recipeSlot in ___recipeSlots)
            {
                CacheRecipe original;
                if (!_originalRecipes.TryGetValue(recipeSlot.recipe.id, out original))
                    _originalRecipes.Add(recipeSlot.recipe.id, new CacheRecipe(recipeSlot.recipe));
                
                var cookMins = recipeSlot.recipe.time.mins * cookBatchSize;
                var cookHours = recipeSlot.recipe.time.hours * cookBatchSize + Mathf.FloorToInt(cookMins / GameDate.MIN_IN_HOUR);
                var cookDays = recipeSlot.recipe.time.days * cookBatchSize + Mathf.FloorToInt(cookHours / GameDate.HOUR_IN_DAY);
                var cookWeeks = recipeSlot.recipe.time.weeks * cookBatchSize + Mathf.FloorToInt(cookDays / GameDate.DAY_IN_WEEK);
                var cookYears = recipeSlot.recipe.time.years * cookBatchSize + Mathf.FloorToInt(cookWeeks / (GameDate.WEEK_IN_SEASON * 4));

                recipeSlot.recipe.time.mins = cookMins % GameDate.MIN_IN_HOUR;
                recipeSlot.recipe.time.hours = cookHours % GameDate.HOUR_IN_DAY;
                recipeSlot.recipe.time.days = cookDays % GameDate.DAY_IN_WEEK;
                recipeSlot.recipe.time.weeks = cookWeeks % GameDate.WEEK_IN_SEASON;
                recipeSlot.recipe.time.years = cookYears;

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
