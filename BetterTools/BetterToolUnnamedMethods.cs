using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BetterTools
{
    public partial class Plugin
    {
        public record struct StaticSelfInstance<T>(bool Found = default, T Instance = default);

        public record struct GetterCache<T>(bool Populated, T Result)
        {
            public GetterCache(T Result) : this(Result != null, Result)
            {
            }
        }

        public record struct MethodCache<T>(bool Populated = false, T Result = default);


        private static GetterCache<GameTileMaps> __gateTileMaps__instance;

        public static GameTileMaps GameTileMaps__Instance
        {
            get
            {
                if (__gateTileMaps__instance.Populated == false)
                {
                    __gateTileMaps__instance = new GetterCache<GameTileMaps>(GetSelfInstance<GameTileMaps>());
                }

                return __gateTileMaps__instance.Result;
            }
        }

        private static GetterCache<Dictionary<Vector3, WorldTile>> __gateTimeMap_WorldTileDict;

        public static Dictionary<Vector3, WorldTile> GameTileMap__WorldTileDict
        {
            get
            {
                if (__gateTimeMap_WorldTileDict.Populated == false)
                    __gateTimeMap_WorldTileDict = new(
                        GetFirstPropertyValue<GameTileMaps, Dictionary<Vector3, WorldTile>>(GameTileMaps__Instance));
                return __gateTimeMap_WorldTileDict.Result;
            }
        }

        private static MethodCache<Func<Vector3, WorldTile>> GetWorldTile__cache;

        public static Func<Vector3, WorldTile> GetWorldTile__method
        {
            get
            {
                if (GetWorldTile__cache.Populated) return GetWorldTile__cache.Result;

                if (GameTileMaps__Instance != default && GameTileMap__WorldTileDict != default)
                    GetWorldTile__cache = new(true, _GetWorldTile);
                else
                    return _ => default;

                return GetWorldTile__cache.Result;
            }
        }

        // GameTileMaps.HMKMBKPOBOM.AFFEMGDEPOE.TryGetValue(vector, out);
        // GameTileMaps.<instance>.<Dictionary<Vector3, int>>.TryGetValue(vector, out)
        private static WorldTile _GetWorldTile(Vector3 position)
        {
            return GameTileMap__WorldTileDict
                .GetValueOrDefault(
                    new Vector3(Mathf.Floor(position.x * 2f) / 2f, Mathf.Floor(position.y * 2f) / 2f, 0f));
        }

        public static T LazyAndExpensiveSearch<T>(Vector3 position) where T : MonoBehaviour
        {
            return Physics2D.OverlapPointAll(position)
                .Select(component1 => component1.gameObject.GetComponent<T>())
                .FirstOrDefault(component2 => component2 != null);
        }

        private static MethodCache<Action<Vector3, GroundType, Location, Season, bool>> ChangeWorldGridTile__cache;

        internal static Action<Vector3, GroundType, Location, Season, bool> ChangeWorldGridTile__method
        {
            get
            {
                if (!ChangeWorldGridTile__cache.Populated)
                    ChangeWorldGridTile__cache = new(true,
                        (vector3, groundType, location, arg4, arg5) => GetChangeWorldTileMethod2().Invoke(null,
                            parameters: new object[] { vector3, groundType, location, arg4, arg5 }));
                return ChangeWorldGridTile__cache.Result;
            }
        }

        private static MethodInfo GetChangeWorldTileMethod2()
        {
            return AccessTools.FirstMethod(typeof(WorldGrid), methodInfo =>
            {
                if (!methodInfo.IsStatic) return false;

                var arguments = methodInfo.GetParameters();
                if (arguments.Length < 3) return false;
                return
                    arguments[0].ParameterType == typeof(Vector3) &&
                    arguments[1].ParameterType == typeof(GroundType) &&
                    arguments[2].ParameterType == typeof(Location);
            });
        }


        internal static StaticSelfInstance<CommonReferences> CommonReferenceInstanceCache;

        public static CommonReferences CommonReferenceInstance
        {
            get
            {
                if (CommonReferenceInstanceCache.Instance == null)
                    CommonReferenceInstanceCache = new(GetSelfInstanceProperty<CommonReferences>() != null,
                        GetSelfInstance<CommonReferences>());
                return CommonReferenceInstanceCache.Instance;
            }
        }
    }
}