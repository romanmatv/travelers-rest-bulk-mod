using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;

namespace LargeBatchCooking
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<int> _cookBatchSize;
        private static ConfigEntry<bool> _cookTimeMultiplier;
        private static ConfigEntry<int> _modGamepadHotKey;
        public static Harmony _harmony;

        private static bool ModTrigger(int PlayerId)
        {
            return PlayerInputs.GetPlayer(PlayerId).GetButton("RightMouseDetect")
                   || PlayerInputs.GetPlayer(PlayerId).GetButton(ActionType.SprintHoldAction)
                   || ReInput.players.GetPlayer(0).controllers.Joysticks.Any(Joystick => Joystick.GetButton(11))
                   ;
            
            // ReInput. button 11 == left joystick idk
            
            /*
             * L3 11
             * R3 12
             */
            
            /*
             * button 0 a (4)
             * button 1 b (5)
             * button 2 x (6)
             * button 3 y (7)
             * button 4 l1 (10)
             * button 5 r1 (11)
             * l2(12) r2(13)
             * button 6 select (14)
             * button 7 start (13)
             * button 8 talk (15)
             * button 9 screenshot (16)
             * button 10 windows (17)
             * button 11 left stick(18)
             * button 12 right stick(19)
             * button 13 up
             * button 14 right (21)
             * button 15 down
             * button 16 left (23)
             */
        }
        
        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            _cookBatchSize = Config.Bind("LargeBatch", "cook batch size", 5,
                "Change the amount of crafts to cook at once");
            _cookTimeMultiplier = Config.Bind("LargeBatch", "bool if increased time is desired", false,
                "Enable Multiplier to change craft time, NOTE: Current the mod cannot keep recipe after day reset so extra materials are lost.");
            _modGamepadHotKey = Config.Bind("LargeBatch", "keycode for button trigger", 11,
                "Haven't mapped all buttons but L3 on Stadia controller is KeyCode 11 in the Rewire.JoyStick that is being used by TravellersRest.");
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        

        private static Dictionary<Category, Dictionary<int, CacheRecipe>> _recipeBooks = new Dictionary<Category, Dictionary<int, CacheRecipe>>();


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

        private static Dictionary<int, CacheRecipe> pullRecipeBook(Category category)
        {
            _recipeBooks.TryGetValue(category, out var recipeBook);
            if (recipeBook == null)
            {
                recipeBook = new Dictionary<int, CacheRecipe>();
                _recipeBooks.Add(category, recipeBook);
            }

            return recipeBook;
        }


        [HarmonyPatch(typeof(GameCraftingUI), "CloseUI")]
        [HarmonyPrefix]
        static void ResetSlots(GameCraftingUI __instance, ref List<RecipeSlot> ___recipeSlots)
        {
            bool largerRecipes = ModTrigger(1);

            if (largerRecipes != true)
            {
                return;
            }

            if (___recipeSlots.Count == 0)
            {
                return;
            }
            
            foreach (var recipeSlot in ___recipeSlots)
            {
                var recipeBook = pullRecipeBook(recipeSlot.recipe.output.item.category);
                
                if (!recipeBook.TryGetValue(recipeSlot.recipe.id, out var original))
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
                var recipeBook = pullRecipeBook(recipeSlot.recipe.output.item.category);
                
                if (!recipeBook.TryGetValue(recipeSlot.recipe.id, out var original))
                {
                    recipeBook.Add(recipeSlot.recipe.id, new CacheRecipe(recipeSlot.recipe));
                }

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
