using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RestlessMods;


namespace ImprovedClicks;

public abstract class OtherSortingOptions : SubModBase
{
    public new static void Awake()
    {
        BaseSetup(nameof(OtherSortingOptions), true);
        
        BaseFinish(typeof(OtherSortingOptions));
    }
    
    private static RandomNameHelper.MethodFinder _resetSlots = new(
        typeof(Utils),
        new[]
        {
            typeof(int),
            typeof(Slot[]),
            typeof(List<ItemInstanceAmount>)
        },
        methodInfo => methodInfo.IsStatic && methodInfo.IsPrivate
    );

    [HarmonyPatch(typeof(Container), nameof(Container.OrderItemsByType))]
    [HarmonyPrefix]
    public static bool NewSort(Container __instance)
    {
        if (__instance == null || !Plugin.ModTrigger(1)) return true;


        var itemInstanceAmounts = __instance.slots
            .Where(slot =>
            {
                if (slot.itemInstance == null) return false;
                var item = Traverse.Create(slot.itemInstance).Field("item").GetValue<Item>();
                return item != null;
            }).Select(slot => new ItemInstanceAmount(slot.itemInstance, slot.Stack))
            .OrderBy(instanceAmount =>
            {
                var price = Money.CalculateSellPrice(instanceAmount.itemInstance, false, true);
                return price.copper + price.silver * 100 + price.gold * 10000;
            })
            .ThenBy(x => x.amount)
            .ThenBy(instanceAmount =>
            {
                var item = Traverse.Create(instanceAmount.itemInstance).Field("item").GetValue<Item>();
                return item.name;
            })
            .ToList();

        _resetSlots.GetMethod()
            .Invoke(null, new object[] {1, __instance.slots, itemInstanceAmounts});
        
        return false;
    }

}