using HarmonyLib;
namespace RenderLimits;

[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.Initialize))]
public class TerrainVisibility
{
  static void Postfix(Heightmap __instance) => Set(__instance);
  static void Set(Heightmap obj)
  {
    var material = obj.m_materialInstance;
    if (material) material.SetFloat("_LodHideDistance", Configuration.RealTerrainVisibility);
  }
  public static void Update()
  {
    foreach (var obj in ObjectDB.FindObjectsOfType<Heightmap>()) Set(obj);
  }
}
