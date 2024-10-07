# 0.1.1
- use shared ModKey logic

# 0.1.0
- Fixed: discounts ("Bonifications") are no longer ignored on large batches
- Fixed: Recipes **are always reset on Exiting the crafter** (Exit with Mod Key to keep modifier)
- Changed: Batch increase has been changed from multiple current to add multiple
  - was 1 -> 5x -> 25x -> 125x -> ...
  - now 1x -> 5x -> 10x -> 15x -> ...
- Fixed: issue on Quest Orders amounts


# 0.0.4
- inorder to fix endless chain of bugs, only one multiplication is applied and is un-applied on crafter exit
- in place of multiple large crafters all crafter now support multi-craft so more than just one recipe "can" be applied

# 0.0.3
- fixed configurable controller being ignored

# 0.0.2
- Fixed controller support (configurable) now able to back out
- fixed issue with different Item categories overlapping and breaking recipe reset.

# 0.0.1
- Added controller support (not configurable yet)
- Added config options to skip time extension to help against recipes not rolling over
- testing changes on non oven crafters (smelter/wood table) and seeing functionality now (mostly) stable