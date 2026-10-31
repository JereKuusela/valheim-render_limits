using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
namespace RenderLimits;

[HarmonyPatch]
public static class LightDistance
{
  [HarmonyPatch(typeof(LightLod), nameof(LightLod.UpdateLoop), MethodType.Enumerator), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> UpdateLoop_Transpiler(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);

  [HarmonyPatch(typeof(LightLod), nameof(LightLod.UpdateLights)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> UpdateLights_Transpiler(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);

  static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
  {
    var matcher = new CodeMatcher(instructions);
    WrapField(matcher, nameof(LightLod.m_lightDistance), nameof(Distance));
    WrapField(matcher, nameof(LightLod.m_lightLod), nameof(Lod));
    return matcher.InstructionEnumeration();
  }

  static void WrapField(CodeMatcher matcher, string field, string method) =>
    matcher.Start()
      .MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LightLod), field)))
      .Repeat(m => m.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LightDistance), method))));

  static bool Lod(bool lod) => lod || Configuration.LightDistance > 0;

  static float Distance(float distance) => Configuration.LightDistance > 0 ? Configuration.LightDistance : distance;
}
