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