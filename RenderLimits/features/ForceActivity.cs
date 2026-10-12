using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
namespace RenderLimits;

[HarmonyPatch]
public static class SpawnerPatches
{
  static float Range(Component spawner, float triggerDistance)
  {
    var zones = ForceZones.Local;
    if (zones.Count == 0 || !zones.Contains(ZoneSystem.GetZone(spawner.transform.position)))
      return triggerDistance;
    return 9999999f;
  }

  static float SpawnAreaRange(SpawnArea spawner) => Range(spawner, spawner.m_triggerDistance);

  static float CreatureSpawnerRange(CreatureSpawner spawner) => Range(spawner, spawner.m_triggerDistance);

  [HarmonyPatch(typeof(SpawnArea), nameof(SpawnArea.UpdateSpawn))]
  [HarmonyTranspiler]
  static IEnumerable<CodeInstruction> SpawnAreaUpdateSpawn(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SpawnArea), nameof(SpawnArea.m_triggerDistance))))
      .Set(OpCodes.Call, AccessTools.Method(typeof(SpawnerPatches), nameof(SpawnAreaRange)))
      .InstructionEnumeration();

  [HarmonyPatch(typeof(CreatureSpawner), nameof(CreatureSpawner.UpdateSpawner))]
  [HarmonyTranspiler]
  static IEnumerable<CodeInstruction> CreatureSpawnerUpdateSpawner(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CreatureSpawner), nameof(CreatureSpawner.m_triggerDistance))))
      .Repeat(matcher => matcher.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(SpawnerPatches), nameof(CreatureSpawnerRange))))
      .InstructionEnumeration();
}
