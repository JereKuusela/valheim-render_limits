using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
namespace RenderLimits;

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateLocalZones))]
public class CreateLocalZones
{
  static void Postfix(ZoneSystem __instance, ref bool __result)
  {
    if (__result) return;
    foreach (var zone in ForceZones.Local)
    {
      if (__instance.PokeLocalZone(zone))
      {
        __result = true;
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
    if (ForceZones.Current.Contains(ZoneSystem.GetZone(point)))
      __result = true;
  }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateObjects))]
public class CreateObjects
{
  static void Prefix(List<ZDO> currentNearObjects)
  {
    var zones = ForceZones.Local;
    if (zones.Count == 0) return;
    var manager = ZDOMan.instance;
    foreach (var zone in zones)
    {
      if (ZoneSystem.instance.IsZoneLoaded(zone))
        manager.FindObjects(zone, currentNearObjects, manager.m_visitedSectorIndices);
    }
  }
}

[HarmonyPatch(typeof(ZDOMan))]
public class ZDOManPatches
{
  [HarmonyPatch(nameof(ZDOMan.FindDistantObjects))]
  [HarmonyPrefix]
  static bool FindDistantObjects_Prefix(Vector2s sector) =>
    !ForceZones.Current.Contains(sector);

  [HarmonyPatch(nameof(ZDOMan.CreateSyncList))]
  [HarmonyPrefix]
  static void CreateSyncList_Prefix(ZDOMan.ZDOPeer peer)
  {
    if (ZNet.instance.IsServer())
      ForceZones.Window = ForceZones.For(peer.m_peer.m_uid);
  }

  [HarmonyPatch(nameof(ZDOMan.CreateSyncList))]
  [HarmonyPostfix]
  static void CreateSyncList_Postfix(ZDOMan __instance, ZDOMan.ZDOPeer peer, List<ZDO> toSync)
  {
    var zones = ForceZones.Window;
    if (zones == null || zones.Count == 0) return;
    List<ZDO> objects = [];
    foreach (var zone in zones)
      __instance.FindObjects(zone, objects, __instance.m_visitedSectorIndices);
    foreach (var zdo in objects)
    {
      if (peer.ShouldSend(zdo))
        toSync.Add(zdo);
    }
  }

  [HarmonyPatch(nameof(ZDOMan.CreateSyncList))]
  [HarmonyFinalizer]
  static Exception? CreateSyncList_Finalizer(Exception? __exception)
  {
    ForceZones.Window = null;
    return __exception;
  }

  [HarmonyPatch(nameof(ZDOMan.ReleaseNearbyZDOS))]
  [HarmonyPrefix]
  static void ReleaseNearbyZDOS_Prefix(long uid)
  {
    ForceZones.Window = ForceZones.For(uid);
  }

  [HarmonyPatch(nameof(ZDOMan.ReleaseNearbyZDOS))]
  [HarmonyPostfix]
  static void ReleaseNearbyZDOS_Postfix(ZDOMan __instance, long uid)
  {
    var zones = ForceZones.Window;
    if (zones == null || zones.Count == 0) return;
    HashSet<ZoneSystem.SectorIndex> visited = [];
    List<ZDO> objects = [];
    foreach (var zone in zones)
      __instance.FindObjects(zone, objects, visited);
    foreach (var zdo in objects)
    {
      if (!zdo.Persistent || zdo.GetOwner() == uid) continue;
      if (!zdo.HasOwner() || !__instance.IsInPeerActiveArea(zdo.GetPosition(), zdo.GetOwner()))
        zdo.SetOwner(uid);
    }
  }

  [HarmonyPatch(nameof(ZDOMan.ReleaseNearbyZDOS))]
  [HarmonyFinalizer]
  static Exception? ReleaseNearbyZDOS_Finalizer(Exception? __exception)
  {
    ForceZones.Window = null;
    return __exception;
  }

  [HarmonyPatch(nameof(ZDOMan.IsInPeerActiveArea))]
  [HarmonyPrefix]
  static void IsInPeerActiveArea_Prefix(long uid, out HashSet<Vector2s>? __state)
  {
    __state = ForceZones.Window;
    ForceZones.Window = ForceZones.For(uid);
  }

  [HarmonyPatch(nameof(ZDOMan.IsInPeerActiveArea))]
  [HarmonyFinalizer]
  static Exception? IsInPeerActiveArea_Finalizer(HashSet<Vector2s>? __state, Exception? __exception)
  {
    ForceZones.Window = __state;
    return __exception;
  }
}

[HarmonyPatch(typeof(ZDOMan.ZDOPeer), nameof(ZDOMan.ZDOPeer.ZDOSectorInvalidated))]
public class ZDOPeer_ZDOSectorInvalidated
{
  static void Prefix(ZDOMan.ZDOPeer __instance, out HashSet<Vector2s>? __state)
  {
    __state = ForceZones.Window;
    ForceZones.Window = ForceZones.For(__instance.m_peer.m_uid);
  }

  static Exception? Finalizer(HashSet<Vector2s>? __state, Exception? __exception)
  {
    ForceZones.Window = __state;
    return __exception;
  }
}
