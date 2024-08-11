using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;

namespace QuickWaterWell
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;
        private static CommonReferences _commonReferences;
        private static ConfigEntry<int> _modGamepadHotKey;


        private static bool ModTrigger(int PlayerId)
        {
            return PlayerInputs.GetPlayer(PlayerId).GetButton("RightMouseDetect")
                   || ReInput.players.GetPlayer(PlayerId - 1).controllers.Joysticks.Any(Joystick => Joystick.GetButton(_modGamepadHotKey.Value))
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
            // Plugin startup logic
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            _modGamepadHotKey = Config.Bind("QuickWaterWell", "keycode for button trigger", 11,
                "Haven't mapped all buttons but L3 on Stadia controller is KeyCode 11 in the Rewire.JoyStick that is being used by TravellersRest.");
            
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        
        [HarmonyPatch(typeof(CommonReferences), "Start")]
        [HarmonyPostfix] //Has to be a postfix so that common ref is loaded 
        private static void CommonReferenceSavingPostfix(CommonReferences __instance)
        {
            _commonReferences = __instance;
        }
        
        [HarmonyPatch(typeof(Well), "MouseUp")]
        [HarmonyPostfix]
        static void MouseUpPrefix(Well __instance)
        {
            bool repeat = ModTrigger(1);
            var emptyBucket = _commonReferences?.bucketItem;

            if (!repeat || emptyBucket == null) return;
            
            while (__instance.IsAvailableByProximity(1) && PlayerInventory.GetPlayer(1).HasItem(emptyBucket))
            {
                __instance.MouseUp(1);
            }
        }
    }
}
