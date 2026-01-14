using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
namespace RenderLimits;


[HarmonyPatch(typeof(SpawnArea), nameof(SpawnArea.UpdateSpawn))]
public class SpawnArea_UpdateSpawn
{
  static float GetRange(SpawnArea sa)
  {
    if (Configuration.ForceActive.Count == 0)
      return sa.m_triggerDistance;
    var zone = ZoneSystem.GetZone(sa.transform.position);
    if (!Configuration.ForceActive.Contains(zone))
      return sa.m_triggerDistance;
    return 999999f;
  }
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false,
        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SpawnArea), nameof(SpawnArea.m_triggerDistance)))
      )
      .Set(OpCodes.Call, AccessTools.Method(typeof(SpawnArea_UpdateSpawn), nameof(GetRange)))
      .InstructionEnumeration();
  }
}


[HarmonyPatch(typeof(CreatureSpawner), nameof(CreatureSpawner.UpdateSpawner))]
public class CreatureSpawner_UpdateSpawner
{
  static float GetRange(CreatureSpawner cs)
  {
    if (Configuration.ForceActive.Count == 0)
      return cs.m_triggerDistance;
    var zone = ZoneSystem.GetZone(cs.transform.position);
    if (!Configuration.ForceActive.Contains(zone))
      return cs.m_triggerDistance;
    return 999999f;
  }
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false,
        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CreatureSpawner), nameof(CreatureSpawner.m_triggerDistance)))
      )
      .Set(OpCodes.Call, AccessTools.Method(typeof(CreatureSpawner_UpdateSpawner), nameof(GetRange)))
      .InstructionEnumeration();
  }
}