using HarmonyLib;
using UnityEngine;


namespace BetterTools;

public class BetterSpade : RestlessMods.SubModBase
{
    private static int _maxLevel;
    private static int _maxRows;

    private static bool Prepared()
    {
        return Plugin.ChangeWorldGridTileMethod != null
               && Plugin.GetWorldTileMethod != null
            ;
    }

    public new static void Awake()
    {
        BaseSetup(nameof(BetterSpade));

        var maxLevel = Config.Bind("maxLevelOverride", 0, "Change the max level for this tool only.");
        var maxRows = Config.Bind("maxRowsOverride", 0, "Change the max rows target for this tool only.");
        _maxLevel = maxLevel.Value > 0 ? maxLevel.Value : Plugin.MaxLevel.Value;
        _maxRows = maxRows.Value > 0 ? maxRows.Value : Plugin.MaxRows.Value;

        if (!Prepared())
            LoadFailure();
        else
            BaseFinish(typeof(BetterSpade));
    }

    internal static Plugin.Tier CurrentTier => Plugin.GetTier(_maxLevel, TavernReputation.GetMilestone());


    /* Code snippet
         if (WorldGrid.NAGEGPJKLKN((Vector3) MDJGJIADKNH) == GroundType.Grass)
         {
             WorldGrid.HHFJCKCJIJH((Vector3) MDJGJIADKNH, GroundType.Ground, Location.Road, Season.Spring, true);
         }
         else if (WorldGrid.NAGEGPJKLKN((Vector3) MDJGJIADKNH) == GroundType.Ground)
         {
             WorldGrid.HHFJCKCJIJH((Vector3) MDJGJIADKNH, GroundType.Grass, Location.Road, Season.Spring, true);
         }
                 PlayerController.TeleportPlayer(1, new Vector3(20f, 0f, 0), Location.Road);

    */

    private static bool Action(Spade tool, int playerId, Vector2 targetVector, string actionStr)
    {
        var target = Plugin.GetWorldTileMethod(targetVector);

        if (target.isPath || target.farmable == false) return false;

        switch (actionStr)
        {
            case "RemoveGrass" when target.groundType == GroundType.Grass:
                Plugin.ChangeWorldGridTileMethod(targetVector, GroundType.Ground, Location.Road, Season.Spring,
                    false);
                return true;
            case "AddGrass" when target.groundType == GroundType.Ground:
                Plugin.ChangeWorldGridTileMethod(targetVector, GroundType.Grass, Location.Road, Season.Spring, false);
                return true;
            default:
                return false;
        }
    }

    [HarmonyPatch(typeof(Spade), nameof(Spade.Action))]
    [HarmonyPostfix]
    public static void ToolAction(Spade __instance, int __0, Vector2 ___tilePosition, bool __result)
    {
        if (!__result || !ModTrigger(ModName, __0)) return;
        var directionVector = Plugin.GetDirectionVector(PlayerController.GetPlayerDirection(__0));
        if (directionVector == Vector2.zero) return;

        int extraRows = Plugin.Tiered(_maxLevel, _maxRows, TavernReputation.GetMilestone());

        var methodToRepeat = Plugin.GetWorldTileMethod(___tilePosition).groundType == GroundType.Grass
            ? "RemoveGrass"
            : "AddGrass";

        for (var i = 1; i < extraRows; i++)
            if (!Action(__instance, __0, ___tilePosition + directionVector * .5f * i, methodToRepeat))
                break;
    }
}