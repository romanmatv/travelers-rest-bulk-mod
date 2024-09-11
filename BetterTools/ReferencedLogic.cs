using System;
using System.Collections.Generic;
using RestlessMods;
using UnityEngine;

namespace BetterTools;

public partial class Plugin
{
    // Change world grid tile
    internal static Action<Vector3, GroundType, Location, Season, bool> ChangeWorldGridTileMethod =>
        (vector3, groundType, location, arg4, arg5) => _changeWorldGridTileMethodFinder.GetMethod()
            .Invoke(null, parameters: new object[] { vector3, groundType, location, arg4, arg5 });

    /* public static void KPPJEDCGAPH(
           Vector3 PGMKEFFJIED,
           GroundType JPOGHNELJJD,
           Location MFDJCAMALLC,
           Season BGJGDGGKMBD = Season.Spring,
           bool OGPMKLFELKK = false);
    */
    private static RandomNameHelper.MethodFinder _changeWorldGridTileMethodFinder = new(
        typeof(WorldGrid),
        new[] { typeof(Vector3), typeof(GroundType), typeof(Location), typeof(Season), typeof(bool) }
    );


    // Get world tile from position
    public static Func<Vector3, WorldTile> GetWorldTileMethod
    {
        get
        {
            if (GameTileMapsInstance != default && GameTileMapWorldTileDict != default)
                return position => GameTileMapWorldTileDict
                    .GetValueOrDefault(new Vector3(
                        Mathf.Floor(position.x * 2f) / 2f, Mathf.Floor(position.y * 2f) / 2f, 0f));
            return _ => default;
        }
    }

    private static Dictionary<Vector3, WorldTile> GameTileMapWorldTileDict =>
        RandomNameHelper.GetPropertyValue<GameTileMaps, Dictionary<Vector3, WorldTile>>(GameTileMapsInstance);

    // Needed Instances
    private static GameTileMaps GameTileMapsInstance => RandomNameHelper.GetSelfInstance<GameTileMaps>();
    public static CommonReferences CommonReferenceInstance => RandomNameHelper.GetSelfInstance<CommonReferences>();
}