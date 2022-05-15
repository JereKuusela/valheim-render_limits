using HarmonyLib;
using UnityEngine;
namespace RenderLimits;

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Awake))]
public class ZoneSystemActive {
  static void Postfix(ZoneSystem __instance) => Set(__instance);
  static void Set(ZoneSystem obj) {
    obj.m_activeArea = Configuration.LoadedArea - 1;
    obj.m_activeDistantArea = Configuration.DistantArea - Configuration.LoadedArea;
  }
  public static void Update() {
    if (ZoneSystem.instance) Set(ZoneSystem.instance);
  }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.InActiveArea), new[] { typeof(Vector2i), typeof(Vector2i) })]
public class InActiveArea {
  static bool Prefix(Vector2i zone, Vector2i refCenterZone, ref bool __result) {
    var num = Configuration.ActiveArea - 1;
    __result = zone.x >= refCenterZone.x - num && zone.x <= refCenterZone.x + num && zone.y <= refCenterZone.y + num && zone.y >= refCenterZone.y - num;
    return false;
  }
}
[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.OutsideActiveArea), new[] { typeof(Vector3), typeof(Vector3) })]
public class OutsideActiveArea {
  static bool Prefix(Vector3 point, Vector3 refPoint, ref bool __result) {
    var num = Configuration.ActiveArea;
    var zone = ZoneSystem.instance.GetZone(refPoint);
    var zone2 = ZoneSystem.instance.GetZone(point);
    __result = zone2.x <= zone.x - num || zone2.x >= zone.x + num || zone2.y >= zone.y + num || zone2.y <= zone.y - num;
    return false;
  }
}
