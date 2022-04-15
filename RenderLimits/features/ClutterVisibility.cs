using HarmonyLib;
namespace RenderLimits;

[HarmonyPatch(typeof(ClutterSystem), nameof(ClutterSystem.Awake))]
public class ClutterDistance {
  static void Postfix(ClutterSystem __instance) => Set(__instance);
  static void Set(ClutterSystem obj) {
    obj.m_distance = Configuration.ClutterVisibility;
    obj.ClearAll();
  }
  public static void Update() {
    if (ClutterSystem.instance) Set(ClutterSystem.instance);
  }
}
