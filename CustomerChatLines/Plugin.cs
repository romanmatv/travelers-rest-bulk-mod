using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using PixelCrushers.DialogueSystem;

namespace CustomerChatLines
{

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static readonly MethodInfo AddNewDialogueEntry = AccessTools.Method(typeof(Conversation),  "AddNewDialogueEntry", new []{typeof(DialogueEntry), typeof(string), typeof(int), typeof(bool)});
        public static Harmony _harmony;

        private record struct ReplacementConfig(ConfigEntry<string> AdditionalLines, ConfigEntry<bool> IsReplacement);

        private static Dictionary<string, ReplacementConfig> Dict { get; } = new();

        private static readonly string[] SupportedConversationReplacements = {
            "BirdCatInteraction",
            "BirdPositiveComments",
            "BirdNegativeComments",
            "TableDirty",
            "EnterTavernDrink",
            "EnterTavernFood",
            "EnterTavernNeutral",
            "Rowdy",
            "TavernClean",
            "NeutralInTavern",
            "TooHot",
            "TooCold",
            "TavernDirty",
            "TavernFilthy",
            "CalmRowdyCustomer",
            "OutHereRowdyCustomer",
            "AcceptRoomFirstFloor",
            "AcceptRoomSecondFloor",
            "HappyRentRoom",
        };


        public static void PlayerTestBark(int playerId, string conversationId)
        {
            DialogueManager.instance.Bark(conversationId, PlayerController.GetPlayer(playerId).transform);
        }

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));

            foreach (var conversationId in SupportedConversationReplacements)
            {
                var replacementConfig = new ReplacementConfig(
                    Config.Bind<string>(conversationId, "additions", null,
                        "Pipe '|' separated list of additional lines for this category"),
                    Config.Bind(conversationId, "replaceExisting", false,
                        "Change to true if you only want your additions to be used;")
                );
                Dict.Add(conversationId, replacementConfig);
            }

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        
        // On game finishing Load
        [HarmonyPatch(typeof(SaveUI), nameof(SaveUI.TitleFadeInFinished))]
        [HarmonyPostfix]
        private static void ApplyCustomerChatLineChanges()
        {
            foreach (var (conversationId, (additions, replaceExisting)) in Dict)
            {
                if (string.IsNullOrWhiteSpace(additions.Value)) continue;
                
                NewDialogue(conversationId, additions.Value, replaceExisting.Value);
            }
        }
        
        private static void NewDialogue(string conversationId, string newDialogueStrings, bool replace = false)
        {
            var conversation = DialogueManager.instance.databaseManager.masterDatabase.GetConversation(conversationId);
            var example = conversation.GetFirstDialogueEntry();

            if (replace)
            {
                for (var i = conversation.dialogueEntries.Count - 1; i >= 0; i--)
                {
                    if (string.IsNullOrEmpty(conversation.dialogueEntries[i].DialogueText)) continue;

                    conversation.dialogueEntries.RemoveAt(i);
                }
            }
            
            foreach (var dialogueStr in newDialogueStrings.Split('|'))
            {
                AddNewDialogueEntry.Invoke(conversation, new object[] { example, dialogueStr, 0, true });
            }
            
        }
    }
}
