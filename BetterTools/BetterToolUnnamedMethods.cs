using System.Linq;
using UnityEngine;

namespace BetterTools;

public partial class Plugin
{
    public static T LazyAndExpensiveSearch<T>(Vector3 position) where T : MonoBehaviour
    {
        return Physics2D.OverlapPointAll(position)
            .Select(component1 => component1.gameObject.GetComponent<T>())
            .FirstOrDefault(component2 => component2 != null);
    }
}