# BetterTools

Until the Devs add better tiers of tools here is a collection of tool improvements based on TavernLevel

notes:
- Tool icon's change with level increases to Iron and Steel. (Note this is only for icon not the moving animation (TODO))


## Improved Ax/Pick
- Adds MAX_LEVEL to denote when this tool is now STEEL and can 1 hit Trees/Rocks

## Hoe/Spade/WateringCan
- Adds MAX_LEVEL and MAX_ROWS to denote when tool is now STEEL and can optionally till/dig/water MAX_ROWS at once with ModTrigger (RightClick/L3).
- NOTE: this **Does not update the highlighting**

With MAX_ROWS = 6
Watering can: default base tool waters 1x3, Modded with ModKey Copper 2x3, Iron 4x3, Steel would water 6x3
Hoe/Spade: default base tool does 1 tile, Modded with ModKey Copper 2, Iron 4, Steel 6



The config file is broken into 1 shared section for default enabled/disable/maxRows/ModKey

And a section for each tool (Ax,Hoe,Pick,Spade,WateringCan)

Ax and Pick only have an enable disable option
Hoe, Spade, and Watering can all have an active, maxLevel, maxRows options.


Default using ModKey (Run/RightClick/L3) with Hoe, Spade, or Watercan will do the action on 2 rows for copper, 4 for iron, 6 for steel.

When maxRows is changed it will be 100% at steel 66% for iron 33% for copper.


Configuration file:
```
## Settings file was created by plugin rbk-tr-BetterTools v1.1.0
## Plugin GUID: rbk-tr-BetterTools


# Shared settings for all tools
[BetterTools]

## L3 is KeyCode 11
# Setting type: Int32
# Default value: 11
ModKey for Controller = 11

## Flag enabling/disabling tool improvement
# Setting type: Boolean
# Default value: true
isActive = true

## Level to 1 hit everything.
# Setting type: Int32
# Default value: 40
maxLevel = 40

## Max rows target for BetterTools.maxLevel.
# Setting type: Int32
# Default value: 6
maxRows = 6



[BetterAx]

## Flag to enable or disable this mod
# Setting type: Boolean
# Default value: true
isEnabled = true

[BetterHoe]

## Flag to enable or disable this mod
# Setting type: Boolean
# Default value: true
isEnabled = true

## Change the max level for this tool only.
# Setting type: Int32
# Default value: 0
maxLevelOverride = 0

## Change the max rows target for this tool only.
# Setting type: Int32
# Default value: 0
maxRowsOverride = 0

[BetterPick]

## Flag to enable or disable this mod
# Setting type: Boolean
# Default value: true
isEnabled = true

[BetterSpade]

## Flag to enable or disable this mod
# Setting type: Boolean
# Default value: true
isEnabled = true

## Change the max level for this tool only.
# Setting type: Int32
# Default value: 0
maxLevelOverride = 0

## Change the max rows target for this tool only.
# Setting type: Int32
# Default value: 0
maxRowsOverride = 0

[BetterWateringCan]

## Flag to enable or disable this mod
# Setting type: Boolean
# Default value: true
isEnabled = true

## Change the max level for this tool only.
# Setting type: Int32
# Default value: 0
maxLevelOverride = 0

## Change the max rows target for this tool only.
# Setting type: Int32
# Default value: 0
maxRowsOverride = 0
```