using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
namespace RenderLimits;

[HarmonyPatch]
public static class LightDistance
{
  static IEnumerable<System.Reflection.MethodBase> TargetMethods()
  {
    yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(LightLod), nameof(LightLod.UpdateLoop)));
    yield return AccessTools.Method(typeof(LightLod), nameof(LightLod.UpdateLights));
  }

  [HarmonyTranspiler]
  static IEnumerable<CodeInstruction> LightDistance_Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var field = AccessTools.Field(typeof(LightLod), nameof(LightLod.m_lightDistance));
    var method = AccessTools.Method(typeof(LightDistance), nameof(Distance));
    var lodField = AccessTools.Field(typeof(LightLod), nameof(LightLod.m_lightLod));
    var lodMethod = AccessTools.Method(typeof(LightDistance), nameof(Lod));
    foreach (var instruction in instructions)
    {
      yield return instruction;
      if (instruction.LoadsField(field))
        yield return new CodeInstruction(OpCodes.Call, method);
      if (instruction.LoadsField(lodField))
        yield return new CodeInstruction(OpCodes.Call, lodMethod);
    }
  }

  static bool Lod(bool lod) => lod || Configuration.LightDistance > 0;

  static float Distance(float distance) => Configuration.LightDistance > 0 ? Configuration.LightDistance : distance;
}
