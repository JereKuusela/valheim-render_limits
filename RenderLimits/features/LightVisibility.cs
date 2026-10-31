using HarmonyLib;
namespace RenderLimits;

[HarmonyPatch(typeof(LightLod), nameof(LightLod.Awake))]
public static class LightDistance
{
  static float applied = 1f;
  static float Multiplier => Configuration.LightLodMultiplier > 0 ? Configuration.LightLodMultiplier : 1f;

  [HarmonyPostfix]
  static void Postfix(LightLod __instance) => __instance.m_lightDistance *= applied;

  public static void Update()
  {
    var ratio = Multiplier / applied;
    applied = Multiplier;
    if (ratio == 1f) return;
    foreach (var light in LightLod.m_lights)
      light.m_lightDistance *= ratio;
  }
}
