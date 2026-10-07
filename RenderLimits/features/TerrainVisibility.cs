using HarmonyLib;
namespace RenderLimits;

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
