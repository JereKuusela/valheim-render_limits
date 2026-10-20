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

[HarmonyPatch]
public static class DistantLights
{
  private static readonly Dictionary<int, bool> Lights = [];

  [HarmonyPatch(typeof(ZDOMan), "FindDistantObjects"), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> FindDistantObjects_Transpiler(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ZDO), nameof(ZDO.Distant))))
      .Set(OpCodes.Call, AccessTools.Method(typeof(DistantLights), nameof(IsDistant)))
      .InstructionEnumeration();

  static bool IsDistant(ZDO zdo) => zdo.Distant || (LoadsLights() && IsLight(zdo.GetPrefab()));

  static bool LoadsLights()
  {
    var peer = PeerActive.Context;
    if (peer == null) return Configuration.LightDistance > 0;
    return Configuration.AllowDistantLights && PeerActive.Get(peer).DistantLights;
  }

  static bool IsLight(int prefab)
  {
    if (Lights.TryGetValue(prefab, out var light)) return light;
    var obj = ZNetScene.instance.GetPrefab(prefab);
    light = obj && obj.GetComponentInChildren<LightLod>(true);
    Lights[prefab] = light;
    return light;
  }
}
