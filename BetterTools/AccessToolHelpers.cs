using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BetterTools
{
    public partial class Plugin
    {
        private static bool CheckEnabled(string key)
        {
            _configFile.TryGetEntry<bool>(key, "isEnabled", out var isEnabled);
            return isEnabled.Value;
        }
        internal static PropertyInfo GetSelfInstanceProperty<T>()
        {
            return AccessTools.FirstProperty(typeof(T), property => property.GetMethod.ReturnType == typeof(T));
        }

        private static PropertyInfo GetProperty<T, R>()
        {
            // return AccessTools.FirstProperty(typeof(GameTileMaps), property => property.GetMethod.ReturnType == typeof(GameTileMaps));
            return AccessTools.FirstProperty(typeof(T), property => property.GetMethod.ReturnType == typeof(R));
        }

        private static T GetSelfInstance<T>()
        {
            return GetFirstPropertyValue<T, T>(default);
        }

        private static R GetFirstPropertyValue<T, R>(T obj)
        {
            return (R)GetProperty<T, R>().GetValue(obj);
        }
        
        internal static Vector2 GetDirectionVector(Direction direction)
        {
            return direction switch
            {
                Direction.Up => Vector2.up,
                Direction.Down => Vector2.down,
                Direction.Left => Vector2.left,
                Direction.Right => Vector2.right,
                _ => Vector2.zero
            };
        }
    }
}