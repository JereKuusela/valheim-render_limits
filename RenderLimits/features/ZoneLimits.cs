using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
namespace RenderLimits;

[HarmonyPatch]
public static class ZonePatches
{
  static float ActiveRange() => PeerActive.Active(PeerActive.Context) + 0.5f;

  [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateLocalZones))]
  [HarmonyPostfix]
  static void CreateLocalZones(ZoneSystem __instance, ref bool __result)
  {
    if (__result) return;
    foreach (var zone in ForceZones.Local)
    {
      if (!__instance.PokeLocalZone(zone)) continue;
      __result = true;
      break;
    }
  }

  // Client can have higher "generated zones" than the server, so must use the highest simulation distance available.
  [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateGhostZones))]
  [HarmonyPrefix]
  static void CreateGhostZones_Prefix(ZoneSystem __instance, out SimulationDistance __state)
  {
    __state = __instance.m_simulationDistance;
    __instance.m_simulationDistance = PeerActive.Widest(__state);
  }

  [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateGhostZones))]
  [HarmonyFinalizer]
  static Exception? CreateGhostZones_Finalizer(ZoneSystem __instance, SimulationDistance __state, Exception? __exception)
  {
    __instance.m_simulationDistance = __state;
    return __exception;
  }

  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.PointInsideActiveArea))]
  [HarmonyTranspiler]
  static IEnumerable<CodeInstruction> PointInsideActiveArea_Transpiler(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 1.5f))
      .Set(OpCodes.Call, AccessTools.Method(typeof(ZonePatches), nameof(ActiveRange)))
      .InstructionEnumeration();

  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.PointInsideActiveArea))]
  [HarmonyPostfix]
  static void PointInsideActiveArea_Postfix(Vector3 point, ref bool __result)
  {
    if (!__result && ForceZones.Current.Contains(ZoneSystem.GetZone(point)))
      __result = true;
  }

  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateObjects))]
  [HarmonyPrefix]
  static void CreateObjects(List<ZDO> currentNearObjects)
  {
    var manager = ZDOMan.instance;
    foreach (var zone in ForceZones.Local)
    {
      if (ZoneSystem.instance.IsZoneLoaded(zone))
        manager.FindObjects(zone, currentNearObjects, manager.m_visitedSectorIndices);
    }
  }

  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.FindDistantObjects))]
  [HarmonyPrefix]
  static bool FindDistantObjects(Vector2s sector) => !ForceZones.Current.Contains(sector);

  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.CreateSyncList))]
  [HarmonyPrefix]
  static void CreateSyncList_Prefix(ZDOMan.ZDOPeer peer, out ZNetPeer? __state) =>
    __state = PeerActive.Enter(peer.m_peer);

  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.CreateSyncList))]
  [HarmonyPostfix]
  static void CreateSyncList_Postfix(ZDOMan __instance, ZDOMan.ZDOPeer peer, List<ZDO> toSync)
  {
    List<ZDO> objects = [];
    foreach (var zone in ForceZones.Current)
      __instance.FindObjects(zone, objects, __instance.m_visitedSectorIndices);
    foreach (var zdo in objects)
    {
      if (peer.ShouldSend(zdo))
        toSync.Add(zdo);
    }
  }

  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.CreateSyncList))]
  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ReleaseNearbyZDOS))]
  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.IsInPeerActiveArea))]
  [HarmonyPatch(typeof(ZDOMan.ZDOPeer), nameof(ZDOMan.ZDOPeer.ZDOSectorInvalidated))]
  [HarmonyFinalizer]
  static Exception? RestoreContext(ZNetPeer? __state, Exception? __exception)
  {
    PeerActive.Context = __state;
    return __exception;
  }

  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ReleaseNearbyZDOS))]
  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.IsInPeerActiveArea))]
  [HarmonyPrefix]
  static void ActiveArea_Prefix(long uid, out ZNetPeer? __state) =>
    __state = PeerActive.Enter(ZNet.instance.GetPeer(uid));

  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ReleaseNearbyZDOS))]
  [HarmonyPostfix]
  static void ReleaseNearbyZDOS_Postfix(ZDOMan __instance, long uid)
  {
    HashSet<ZoneSystem.SectorIndex> visited = [];
    List<ZDO> objects = [];
    foreach (var zone in ForceZones.Current)
      __instance.FindObjects(zone, objects, visited);
    foreach (var zdo in objects)
    {
      if (!zdo.Persistent || zdo.GetOwner() == uid) continue;
      if (!zdo.HasOwner() || !__instance.IsInPeerActiveArea(zdo.GetPosition(), zdo.GetOwner()))
        zdo.SetOwner(uid);
    }
  }

  [HarmonyPatch(typeof(ZDOMan.ZDOPeer), nameof(ZDOMan.ZDOPeer.ZDOSectorInvalidated))]
  [HarmonyPrefix]
  static void ZDOSectorInvalidated(ZDOMan.ZDOPeer __instance, out ZNetPeer? __state) =>
    __state = PeerActive.Enter(__instance.m_peer);
}
