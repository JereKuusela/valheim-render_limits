using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
namespace RenderLimits;

[HarmonyPatch(typeof(ZoneSystem))]
public class ZoneSystemPatches
{
  [HarmonyPatch(nameof(ZoneSystem.Awake))]
  [HarmonyPostfix]
  static void Awake_Postfix(ZoneSystem __instance) => Set(__instance);

  static void Set(ZoneSystem obj)
  {
    var limits = LimitManager.GetLocalLimits();
    obj.m_activeArea = limits.LoadedArea;
    obj.m_activeDistantArea = limits.GeneratedArea;
  }

  public static void Update()
  {
    if (ZoneSystem.instance) Set(ZoneSystem.instance);
  }

  [HarmonyPatch(nameof(ZoneSystem.CreateLocalZones))]
  [HarmonyPostfix]
  static void CreateLocalZones_Postfix(ZoneSystem __instance, ref bool __result)
  {
    var forced = LimitManager.GetLocalLimits().ForceActive;
    if (forced.Count == 0) return;
    if (__result) return;
    foreach (var zone in forced)
    {
      if (__instance.PokeLocalZone(zone))
      {
        __result = true;
        break;
      }
    }
  }

  static int GetTotalArea() => LimitManager.GetMaxTotal();
  [HarmonyPatch(nameof(ZoneSystem.CreateGhostZones))]
  [HarmonyTranspiler]
  static IEnumerable<CodeInstruction> CreateGhostZones_Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false,
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ZoneSystem), nameof(ZoneSystem.m_activeArea)))
      )
      .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(ZoneSystemPatches), nameof(GetTotalArea)))
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .InstructionEnumeration();
  }

  [HarmonyPatch(nameof(ZoneSystem.IsActiveAreaLoaded))]
  [HarmonyPostfix]
  static void IsActiveAreaLoaded_Postfix(ZoneSystem __instance, ref bool __result)
  {
    var forced = LimitManager.GetLocalLimits().ForceActive;
    if (forced.Count == 0) return;
    if (!__result) return;
    foreach (var zone in forced)
    {
      if (!__instance.m_zones.ContainsKey(zone))
      {
        __result = false;
        break;
      }
    }
  }
}

[HarmonyPatch(typeof(ZNetScene))]
public class ZNetScenePatches
{
  [HarmonyPatch(nameof(ZNetScene.InActiveArea), [typeof(Vector2i), typeof(Vector2i)])]
  [HarmonyPrefix]
  static bool InActiveArea_Prefix(Vector2i zone, Vector2i refCenterZone, ref bool __result)
  {
    var limits = LimitManager.GetCurrent();
    if (limits.ForceActive.Contains(zone))
    {
      __result = true;
      return false;
    }
    var num = limits.ActiveArea;
    __result = zone.x >= refCenterZone.x - num && zone.x <= refCenterZone.x + num && zone.y <= refCenterZone.y + num && zone.y >= refCenterZone.y - num;
    return false;
  }

  [HarmonyPatch(nameof(ZNetScene.InActiveArea), [typeof(Vector2i), typeof(Vector2i), typeof(int)])]
  [HarmonyPrefix]
  static bool InActiveArea2_Prefix(Vector2i zone, Vector2i refCenterZone, ref bool __result)
  {
    var limits = LimitManager.GetCurrent();
    if (limits.ForceActive.Contains(zone))
    {
      __result = true;
      return false;
    }
    var num = limits.ActiveArea;
    __result = zone.x >= refCenterZone.x - num && zone.x <= refCenterZone.x + num && zone.y <= refCenterZone.y + num && zone.y >= refCenterZone.y - num;
    return false;
  }

  [HarmonyPatch(nameof(ZNetScene.OutsideActiveArea), [typeof(Vector3), typeof(Vector3)])]
  [HarmonyPrefix]
  static bool OutsideActiveArea_Prefix(Vector3 point, Vector3 refPoint, ref bool __result)
  {
    var limits = LimitManager.GetCurrent();
    var num = limits.ActiveArea;
    var zone = ZoneSystem.GetZone(point);
    if (limits.ForceActive.Contains(zone))
    {
      __result = false;
      return false;
    }
    var refZone = ZoneSystem.GetZone(refPoint);
    __result = zone.x < refZone.x - num || zone.x > refZone.x + num || zone.y > refZone.y + num || zone.y < refZone.y - num;
    return false;
  }
}


[HarmonyPatch(typeof(ZDOMan))]
public class ZDOManPatches
{
  [HarmonyPatch(nameof(ZDOMan.CreateSyncList))]
  [HarmonyPrefix]
  static void CreateSyncList_Prefix(ZDOMan.ZDOPeer peer)
  {
    LimitManager.SetCurrentPlayer(peer.m_peer.m_uid);
  }
  [HarmonyPatch(nameof(ZDOMan.CreateSyncList))]
  [HarmonyPostfix]
  static void CreateSyncList_Postfix()
  {
    LimitManager.SetCurrentPlayer(0);
  }

  [HarmonyPatch(nameof(ZDOMan.ReleaseNearbyZDOS))]
  [HarmonyPrefix]
  static void ReleaseNearbyZDOS_Prefix(long uid)
  {
    LimitManager.SetCurrentPlayer(uid);
  }
  [HarmonyPatch(nameof(ZDOMan.ReleaseNearbyZDOS))]
  [HarmonyPostfix]
  static void ReleaseNearbyZDOS_Postfix()
  {
    LimitManager.SetCurrentPlayer(0);
  }


  [HarmonyPatch(nameof(ZDOMan.FindDistantObjects))]
  [HarmonyPrefix]
  // Force active should not be marked as distant, so have to skip them here.
  static bool FindDistantObjects_Prefix(Vector2i sector) =>
    !LimitManager.GetCurrent().ForceActive.Contains(sector);


  [HarmonyPatch(nameof(ZDOMan.FindSectorObjects))]
  [HarmonyPrefix]
  // Simpler to override here instead of transpiling on parent method.
  static void FindSectorObjects_Prefix(ref int area, ref int distantArea)
  {
    var limits = LimitManager.GetCurrent();
    area = limits.LoadedArea;
    if (distantArea != 0)
      distantArea = limits.GeneratedArea;
  }
  [HarmonyPatch(nameof(ZDOMan.FindSectorObjects))]
  [HarmonyPostfix]
  static void FindSectorObjects_Postfix(ZDOMan __instance, Vector2i sector, int area, List<ZDO> sectorObjects)
  {
    var forced = LimitManager.GetCurrent().ForceActive;
    if (forced.Count == 0) return;
    foreach (var zone in forced)
    {
      // Zones included in the area can be skipped as already processed.
      if (zone.x >= sector.x - area && zone.x <= sector.x + area && zone.y <= sector.y + area && zone.y >= sector.y - area)
        continue;
      __instance.FindObjects(zone, sectorObjects);
    }
  }
}


[HarmonyPatch(typeof(ZDOMan.ZDOPeer))]
public class ZDOPeerPatches
{
  [HarmonyPatch(nameof(ZDOMan.ZDOPeer.ZDOSectorInvalidated))]
  [HarmonyPrefix]
  static void CreateSyncList_Prefix(ZDOMan.ZDOPeer __instance)
  {
    LimitManager.SetCurrentPlayer(__instance.m_peer.m_uid);
  }
  [HarmonyPatch(nameof(ZDOMan.ZDOPeer.ZDOSectorInvalidated))]
  [HarmonyPostfix]
  static void CreateSyncList_Postfix()
  {
    LimitManager.SetCurrentPlayer(0);
  }
}