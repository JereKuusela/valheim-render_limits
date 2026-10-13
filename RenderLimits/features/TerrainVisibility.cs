using HarmonyLib;
namespace RenderLimits;

// Game already handles terrain visibility, but custom support is kept for backward compatibility.
[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetLodHideDistance))]
public class TerrainVisibility
{
  static bool Prefix(ref float __result)
  {
    var visibility = Configuration.RealTerrainVisibility;
    if (visibility == 0) return true;
    __result = visibility;
    return false;
  }
}
