using HarmonyLib;
using RestlessMods;
using UnityEngine;
using Object = UnityEngine.Object;


namespace BetterTools;

public class BetterHoe : SubModBase
{
    private static int _maxLevel;
    private static int _maxRows;
    private static bool Prepared()
    {
        return RandomNameHelper.GetStaticSelfReferenceProperty<CommonReferences>() != null
               && Plugin.ChangeWorldGridTileMethod != null
               && Plugin.GetWorldTileMethod != null;
    }

    public new static void Awake()
    {
        BaseSetup(nameof(BetterHoe));

        var maxLevel = Config.Bind("maxLevelOverride", 0, "Change the max level for this tool only.");
        var maxRows = Config.Bind("maxRowsOverride", 0, "Change the max rows target for this tool only.");
        _maxLevel = maxLevel.Value > 0 ? maxLevel.Value : Plugin.MaxLevel.Value;
        _maxRows = maxRows.Value > 0 ? maxRows.Value : Plugin.MaxRows.Value;

        if (!Prepared())
            LoadFailure();
        else
            BaseFinish(typeof(BetterHoe));
    }

    internal static Plugin.Tier CurrentTier => Plugin.GetTier(_maxLevel, TavernReputation.GetMilestone());


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
        var target = Plugin.GetWorldTileMethod(targetVector);

        if (target.isPath || target.farmable == false || target.groundType != GroundType.Ground) return false;

        Object.Instantiate(
            Plugin.CommonReferenceInstance.tilledEarthPrefab, targetVector,
            Plugin.CommonReferenceInstance.tilledEarthPrefab.transform.rotation);
        Plugin.ChangeWorldGridTileMethod(targetVector, GroundType.TilledEarth, Location.Road, Season.Spring, false);
        return true;
    }

    private static bool Bury(Hoe tool, int playerId, Vector2 targetVector)
    {
        var target = Plugin.GetWorldTileMethod(targetVector);

        if (target.isPath || target.farmable == false || target.groundType != GroundType.TilledEarth) return false;

        var soilToRemove = Plugin.LazyAndExpensiveSearch<FertileSoil>(targetVector);

        if (soilToRemove == null) return false;

        Plugin.ChangeWorldGridTileMethod(targetVector, GroundType.Ground, Location.Road, Season.Spring, false);
        Object.Destroy(soilToRemove.gameObject);
        return true;
    }


    [HarmonyPatch(typeof(Hoe), nameof(Hoe.Action))]
    [HarmonyPostfix]
    public static void ToolAction(Hoe __instance, int __0, Vector2 ___tilePosition, bool __result)
    {
        if (!__result || !ModTrigger(ModName, __0)) return;
        var directionVector = Plugin.GetDirectionVector(PlayerController.GetPlayerDirection(__0));
        if (directionVector == Vector2.zero) return;

        int extraRows = Plugin.Tiered(_maxLevel, _maxRows, TavernReputation.GetMilestone());

        var methodToRepeat = Plugin.GetWorldTileMethod(___tilePosition).groundType == GroundType.TilledEarth
            ? "Bury"
            : "AddSoil";

        for (var i = 1; i < extraRows; i++)
            if (!Action(__instance, __0, ___tilePosition + directionVector * .5f * i, methodToRepeat))
                break;
    }
}