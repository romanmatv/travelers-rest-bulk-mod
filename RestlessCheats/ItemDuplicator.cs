using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace RestlessCheats;

public class ItemDuplicator : SampleSubModBase
{
    private enum DuplicationType
    {
        Double,
        FillStack,
    }
    
    private static DuplicationType _duplicationType => Config.Bind("duplicationType", DuplicationType.FillStack,
        "Double will double amount for each slot in container (can overflow), FillStack will make every stack filled completely.").Value;
    
    public new static void Awake(Harmony harmony, ConfigFile configFile, ManualLogSource logger)
    {
        BaseSetup(harmony, configFile, logger, nameof(ItemDuplicator));
        
        // Add more here
        
        
        BaseFinish(typeof(ItemDuplicator));
    }

    public static void FillStack(Slot[] slots)
    {
        foreach (var slot in slots)
        {
            if (slot.Stack == 0) continue;
            var maxStack = Traverse.Create(slot.itemInstance).Field("item").GetValue<Item>()?.amountStack ?? 0;
            if (maxStack == 0) continue;

            slot.Stack = maxStack;
        }
    }
    
    
    public static void DuplicateAllItems(Container container, Slot[] slots)
    {
        var toAdd = (from slot in slots where slot.Stack != 0 select Tuple.Create(slot.Stack, slot.itemInstance)).ToList();

        foreach (var (stack, itemInstance) in toAdd)
        {
            
            // Add Item Instance only does 1 no matter the stack so just repeat for each stack num
            for (var i=0; i < stack; i++)
                container.AddItemInstance(stack, itemInstance);
        }
    }

    [HarmonyPatch(typeof(ContainerUI), nameof(ContainerUI.OpenUI))]
    [HarmonyPostfix]
    public static void OpenUI(ContainerUI __instance)
    {
        if (!Plugin.ModTrigger(1)) return;
        
        var container = __instance.container;
        var slots = __instance.containerSlots;

        switch (_duplicationType)
        {
            case DuplicationType.Double:
                DuplicateAllItems(container, slots);
                break;
            case DuplicationType.FillStack:
                FillStack(slots);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        };

    }
    
}