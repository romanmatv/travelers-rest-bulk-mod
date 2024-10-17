# CustomItems

Always players to add more chat lines for many different situations, as more situations are found I can add them to the mod but here is the current list of Conversation Groups for the NPCs to pull from.

- BirdCatInteraction
- BirdPositiveComments
- BirdNegativeComments
- TableDirty
- EnterTavernDrink
- EnterTavernFood
- EnterTavernNeutral
- Rowdy
- TavernClean
- NeutralInTavern
- TooHot
- TooCold
- TavernDirty
- TavernFilthy
- CalmRowdyCustomer
- OutHereRowdyCustomer
- AcceptRoomFirstFloor
- AcceptRoomSecondFloor
- HappyRentRoom


This mod will add a Block for each in the cfg file example changing to only have 2 options
```
[TooCold]

## Pipe '|' separated list of additional lines for this category
# Setting type: String
# Default value:
additions = should have brought me sweater|did someone leave the door open

## Change to true if you only want your additions to be used;
# Setting type: Boolean
# Default value: false
replaceExisting = true
```

For the more technical:

If you are using UnityExplorer you can test the different Conversation Groups even those not listed in the above like so
// definition: public static void PlayerTestBark(int playerId, string conversationId);


using CustomerChatLines;
Plugin.PlayerTestBark(1, "Rowdy");
// this will have your character "bark" a chat line.