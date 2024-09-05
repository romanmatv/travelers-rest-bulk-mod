using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;


namespace BetterTools;

public class BetterHoe : SampleSubModBase
{
    private static int _maxLevel;
    private static int _maxRows;
    private static bool Prepared()
    {
        return Plugin.GetSelfInstanceProperty<CommonReferences>() != null
               && Plugin.ChangeWorldGridTile__method != null
               && Plugin.GetWorldTile__method != null;
    }

    public new static void Awake(Harmony _harmony, ConfigFile configFile, ManualLogSource logger)
    {
        BaseSetup(_harmony, configFile, logger, nameof(BetterHoe));

        var maxLevel = Config.Bind("maxLevelOverride", 0, "Change the max level for this tool only.");
        var maxRows = Config.Bind("maxRowsOverride", 0, "Change the max rows target for this tool only.");
        _maxLevel = maxLevel.Value > 0 ? maxLevel.Value : Plugin.MaxLevel.Value;
        _maxRows = maxRows.Value > 0 ? maxRows.Value : Plugin.MaxRows.Value;

        if (!Prepared())
            LoadFailure();
        else
            BaseFinish(typeof(BetterHoe));
    }

    internal static Plugin.Tier currentTier => Plugin.GetTier(_maxLevel, TavernReputation.GetMilestone());


    // Hoe
    /* Code snippet ne
        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(CommonReferences.HMKMBKPOBOM.tilledEarthPrefab, (Vector3) tool.EAILONMGFJC(FALFKEPOBBP), CommonReferences.HMKMBKPOBOM.tilledEarthPrefab.transform.rotation);
        WorldGrid.HHFJCKCJIJH((Vector3) tool.EAILONMGFJC(FALFKEPOBBP), GroundType.TilledEarth, PlayerController.GetPlayer(FALFKEPOBBP).FAJGACNMLBA);
        gameObject.GetComponent<FertileSoil>().PickUpPlaceables();

        // or
        var soil = Utils.FEJCJDFBPJD<FertileSoil>(PlayerController.GetPlayer(FALFKEPOBBP).transform.position);
        WorldGrid.HHFJCKCJIJH((Vector3) tool.EAILONMGFJC(FALFKEPOBBP), GroundType.Ground, PlayerController.GetPlayer(FALFKEPOBBP).FAJGACNMLBA);
        UnityEngine.Object.Destroy(soil);
    */

    private static bool Action(Hoe tool, int playerId, Vector2 targetVector, string actionStr)
    {
        return actionStr switch
        {
            "AddSoil" => AddSoil(tool, playerId, targetVector),
            "Bury" => Bury(tool, playerId, targetVector),
            _ => false
        };
    }

    private static bool AddSoil(Hoe tool, int playerId, Vector2 targetVector)
    {
        var target = Plugin.GetWorldTile__method(targetVector);

        if (target.isPath || target.farmable == false || target.groundType != GroundType.Ground) return false;

        Object.Instantiate(
            Plugin.CommonReferenceInstance.tilledEarthPrefab, targetVector,
            Plugin.CommonReferenceInstance.tilledEarthPrefab.transform.rotation);
        Plugin.ChangeWorldGridTile__method(targetVector, GroundType.TilledEarth, Location.Road, Season.Spring, false);
        // gameObject.GetComponent<FertileSoil>().PickUpPlaceables();
        return true;
    }

    private static bool Bury(Hoe tool, int playerId, Vector2 targetVector)
    {
        var target = Plugin.GetWorldTile__method(targetVector);

        if (target.isPath || target.farmable == false || target.groundType != GroundType.TilledEarth) return false;

        var soilToRemove = Plugin.LazyAndExpensiveSearch<FertileSoil>(targetVector);

        if (soilToRemove == null) return false;

        Plugin.ChangeWorldGridTile__method(targetVector, GroundType.Ground, Location.Road, Season.Spring, false);
        Object.Destroy(soilToRemove.gameObject);
        return true;
    }


    [HarmonyPatch(typeof(Hoe), nameof(Hoe.Action))]
    [HarmonyPostfix]
    public static void ToolAction(Hoe __instance, int __0, Vector2 ___tilePosition, bool __result)
    {
        if (!__result || !Plugin.ModTrigger(__0)) return;
        var directionVector = Plugin.GetDirectionVector(PlayerController.GetPlayerDirection(__0));
        if (directionVector == Vector2.zero) return;

        int extraRows = Plugin.Tiered(_maxLevel, _maxRows, TavernReputation.GetMilestone());

        var methodToRepeat = Plugin.GetWorldTile__method(___tilePosition).groundType == GroundType.TilledEarth
            ? "Bury"
            : "AddSoil";

        for (var i = 1; i < extraRows; i++)
            if (!Action(__instance, __0, ___tilePosition + directionVector * .5f * i, methodToRepeat))
                break;
    }
}