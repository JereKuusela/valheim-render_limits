using System;
using HarmonyLib;
using UnityEngine;
namespace RenderLimits;

[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.Initialize))]
public class TerrainVisibility
{
  static void Postfix(Heightmap __instance) => Set(__instance);
  static void Set(Heightmap obj)
  {
    var material = obj.m_materialInstance;
    var visibility = Configuration.RealTerrainVisibility;
    if (visibility == 0)
    {
      // Ideally fake terrain stars where loaded area ends.
      // However the actual distance varies based on where the player is in the zone.
      // Default value is 180, which is about (2 + 0.0) * 64. Probably a good approximation.
      visibility = (int)((Configuration.LoadedArea + 0.8) * 64);
    }
    if (material) material.SetFloat("_LodHideDistance", visibility);
  }
  public static void Update()
  {
    foreach (var obj in UnityEngine.Object.FindObjectsByType<Heightmap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) Set(obj);
  }
}
