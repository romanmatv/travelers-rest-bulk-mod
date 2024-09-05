using System;
using UnityEngine;

namespace BetterTools;

public partial class Plugin
{
    internal static int Reduction(int workAmount, int level)
    {
        var result = workAmount * (1f - (float)level / MaxLevel.Value);
        return Mathf.Max(1, (int)Math.Floor(result));
    }

    internal static Tier GetTier(int maxLevel, int level)
    {
        return level >= maxLevel
            ? Tier.Steel
            : level >= maxLevel * .6f
                ? Tier.Iron
                : Tier.Copper;
    }

    internal static int Tiered(int maxLevel, int maxModifier, int level)
    {
        return GetTier(maxLevel, level) switch
        {
            Tier.Copper => (int)Math.Ceiling(maxModifier / 3f),
            Tier.Iron => (int)Math.Ceiling(maxModifier / 3f * 2),
            Tier.Steel => maxModifier,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}