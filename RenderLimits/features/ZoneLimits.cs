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
  [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateGhostZones)), HarmonyPrefix]
  static void CreateGhostZones_Prefix(ZoneSystem __instance, out SimulationDistance __state)
  {
    __state = __instance.m_simulationDistance;
    __instance.m_simulationDistance = PeerActive.Widest(__state);
  }

  // And restoring the original state to avoid unnecesary changes.
  [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateGhostZones)), HarmonyFinalizer]
  static Exception? CreateGhostZones_Finalizer(ZoneSystem __instance, SimulationDistance __state, Exception? __exception)
  {
    __instance.m_simulationDistance = __state;
    return __exception;
  }

  // This mod adds setting for active area, so must apply it here.
  // Only the initial value must be changed because this mod uses same logic so other values align automatically.
  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.PointInsideActiveArea)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> PointInsideActiveArea_Transpiler(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 1.5f))
      .Set(OpCodes.Call, AccessTools.Method(typeof(ZonePatches), nameof(ActiveRange)))
      .InstructionEnumeration();

  // Force zones are always active, so always return true for points inside them.
  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.PointInsideActiveArea)), HarmonyPostfix]
  static void PointInsideActiveArea_Postfix(Vector3 point, ref bool __result)
  {
    if (!__result && ForceZones.Current.Contains(ZoneSystem.GetZone(point)))
      __result = true;
  }

  // With m_visitedSectorIndices, force zones can be just directly added as it handles overlaps.
  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateObjects)), HarmonyPrefix]
  static void CreateObjects(List<ZDO> currentNearObjects)
  {
    var manager = ZDOMan.instance;
    foreach (var zone in ForceZones.Local)
    {
      if (ZoneSystem.instance.IsZoneLoaded(zone))
        manager.FindObjects(zone, currentNearObjects, manager.m_visitedSectorIndices);
    }
  }

  // Adds force zone handling, must be near start of function to ensure these get handled as near zones.
  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.FindSectorObjects)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> FindSectorObjects_Transpiler(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(HashSet<ZoneSystem.SectorIndex>), nameof(HashSet<ZoneSystem.SectorIndex>.Clear))))
      .Advance(1)
      .Insert(
        new CodeInstruction(OpCodes.Ldarg_0),
        new CodeInstruction(OpCodes.Ldarg_3),
        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ZonePatches), nameof(AddForceZones))))
      .InstructionEnumeration();

  static void AddForceZones(ZDOMan manager, List<ZDO> objects)
  {
    foreach (var zone in ForceZones.Current)
      manager.FindObjects(zone, objects, manager.m_visitedSectorIndices);
  }

  // All functions don't include peer id, so must manually track the context.
  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.CreateSyncList)), HarmonyPrefix]
  static void CreateSyncList_Prefix(ZDOMan.ZDOPeer peer, out ZNetPeer? __state) =>
    __state = PeerActive.Enter(peer.m_peer);

  // All functions don't include peer id, so must manually track the context.
  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ReleaseNearbyZDOS))]
  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.IsInPeerActiveArea))]
  [HarmonyPrefix]
  static void ActiveArea_Prefix(long uid, out ZNetPeer? __state) =>
    __state = PeerActive.Enter(ZNet.instance.GetPeer(uid));

  // All functions don't include peer id, so must manually track the context.
  [HarmonyPatch(typeof(ZDOMan.ZDOPeer), nameof(ZDOMan.ZDOPeer.ZDOSectorInvalidated)), HarmonyPrefix]
  static void ZDOSectorInvalidated(ZDOMan.ZDOPeer __instance, out ZNetPeer? __state) =>
    __state = PeerActive.Enter(__instance.m_peer);


  // Previous context restored to avoid affecting other code.
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

}
