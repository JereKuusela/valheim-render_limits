using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
namespace RenderLimits;

[HarmonyPatch(typeof(ZoneSystem))]
public class ZoneSystemPatches
{
  [HarmonyPatch(nameof(ZoneSystem.CreateLocalZones))]
  [HarmonyPostfix]
  static void CreateLocalZones_Postfix(ZoneSystem __instance, ref bool __result)
  {
    if (__result) return;
    foreach (var zone in Configuration.ForceActive)
    {
      if (__instance.PokeLocalZone(zone))
      {
        __result = true;
        break;
      }
    }
  }

  [HarmonyPatch(nameof(ZoneSystem.IsActiveAreaLoaded))]
  [HarmonyPostfix]
  static void IsActiveAreaLoaded_Postfix(ZoneSystem __instance, ref bool __result)
  {
    if (!__result) return;
    foreach (var zone in Configuration.ForceActive)
    {
      if (!__instance.m_zones.ContainsKey(zone))
      {
        __result = false;
        break;
      }
    }
  }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.PointInsideActiveArea))]
public class PointInsideActiveArea
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 1.5f))
      .Set(OpCodes.Ldsfld, AccessTools.Field(typeof(LimitManager), nameof(LimitManager.ActiveRange)))
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 1.75f))
      .Set(OpCodes.Ldsfld, AccessTools.Field(typeof(LimitManager), nameof(LimitManager.ActiveRangeRounded)))
      .InstructionEnumeration();
  }
  static void Postfix(Vector3 point, ref bool __result)
  {
    if (__result) return;
    if (Configuration.ForceActive.Contains(ZoneSystem.GetZone(point)))
      __result = true;
  }
}

[HarmonyPatch(typeof(ZDOMan))]
public class ZDOManPatches
{
  [HarmonyPatch(nameof(ZDOMan.FindDistantObjects))]
  [HarmonyPrefix]
  static bool FindDistantObjects_Prefix(Vector2s sector) =>
    !Configuration.ForceActive.Contains(sector);

  [HarmonyPatch(nameof(ZDOMan.FindSectorObjects))]
  [HarmonyPostfix]
  static void FindSectorObjects_Postfix(ZDOMan __instance, List<ZDO> sectorObjects)
  {
    foreach (var zone in Configuration.ForceActive)
      __instance.FindObjects(zone, sectorObjects, __instance.m_visitedSectorIndices);
  }
}
