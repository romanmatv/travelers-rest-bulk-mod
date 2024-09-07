using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterTools
{
    public partial class Plugin
    {
        private static void TextureChange(Tool tool)
        {
            var x = tool.GetHashCode() switch
            {
                1435 => CheckEnabled(nameof(BetterWateringCan)) ? 0 : -1, // watering can
                1060 => CheckEnabled(nameof(BetterAx)) ? 4 : -1, // axe
                // ReSharper disable once AccessToStaticMemberViaDerivedType
                1061 => CheckEnabled(nameof(BetterHoe)) ? 3 : -1, // hoe
                // ReSharper disable once AccessToStaticMemberViaDerivedType
                1062 => CheckEnabled(nameof(BetterSpade)) ? 2 : -1, // spade
                1063 => CheckEnabled(nameof(BetterPick)) ? 1 : -1, // pick
                _ => -1
            };
            if (x == -1) return;
            // if (!_lessenWorkByLevel.Value) return;

            Tier tier = x switch
                {
                    0 => BetterWateringCan.CurrentTier,
                    4 => BetterAx.CurrentTier,
                    3 => BetterHoe.CurrentTier,
                    2 => BetterSpade.CurrentTier,
                    1 => BetterPick.CurrentTier,
                    _ => Tier.Copper
                };

            var tex = new Texture2D(512, 128, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.LoadImage(ToolTextureSheetBytes);
            tool.icon = Sprite.Create(tex, new Rect(x * 32, ((int)tier * 32), 32, 32), new Vector2(0f, 0f));
        }

        // On game finishing Load
        [HarmonyPatch(typeof(SaveUI), nameof(SaveUI.TitleFadeInFinished))]
        [HarmonyPostfix]
        static void ToolUpdates(SaveUI __instance, SaveSlotUI ___lastSlotSelected, int __0)
        {
            // ShopDatabaseAccessor s;
            // SaveTexture(tool.icon?.texture, $"{tool.name}_{tool.bodyPart}_iconTexture");
            // SaveTexture(tool.sprite?.texture, tool.name + "_spriteTexture");
            // SaveTexture(tool.skin?.icon?.texture, tool.name + "_skin_iconTexture");
            // SaveTexture(tool.skin?.sprites?[0].texture, tool.name + "_skin_spriteTexture");

            // TODO: color animation
            // CharacterAnimation, CharacterAnimator, CharacterController, CharacterSprite, CharacterSpritesDatabase;
            // CharacterSprite cs;
            // cs.
            // Go through all items and apply tool changes
            foreach (var toolId in _toolIdList)
            {
                var tool = (Tool)ItemDatabaseAccessor.GetItem(toolId);

                var inBag = PlayerInventory.GetPlayer(__0)
                    .GetSlotsWithItem(tool, null, false, -1);
                var onHand = ActionBarInventory.GetPlayer(__0)
                    .GetSlotsWithItem(tool, null, false, -1);
                var onIce = BuildingInventory.GetInstance()
                    .GetSlotsWithItem(tool, null, false, -1);

                ApplyTextureChanges(inBag);
                ApplyTextureChanges(onHand);
                ApplyTextureChanges(onIce);
            }
        }

        private static void ApplyTextureChanges(List<Slot> itemInstances)
        {
            foreach (var slot in itemInstances)
            {
                TextureChange(Traverse.Create(slot.itemInstance).Field("item").GetValue<Tool>());
            }
        }
    }
}