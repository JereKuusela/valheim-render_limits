using System;
using System.Collections.Generic;
using HarmonyLib;
namespace RenderLimits;

public static class PeerActive
{
  private const string RPC_REQUEST = "RPC_RequestValidSimulationDistance";
  private static readonly Dictionary<long, (int Requested, int Active, bool Single)> Peers = [];
  private static int sent = -1;

  public static void Apply(long uid)
  {
    if (Peers.TryGetValue(uid, out var peer))
      LimitManager.SetRange(peer.Active, peer.Single);
    else
      LimitManager.SetRange(LimitManager.Active, false);
  }

  public static void Remove(long uid) => Peers.Remove(uid);

  public static void Set(ZNetPeer peer, int? requested)
  {
    if (peer.m_uid == 0) return;
    var value = requested ?? (Peers.TryGetValue(peer.m_uid, out var previous) ? previous.Requested : 1);
    var near = peer.m_simulationDistance.NearSimulationDistance;
    Peers[peer.m_uid] = (value, Math.Max(0, Math.Min(value, near - 1)), near == 1);
  }

  public static void Send(ZNet net, bool force = false)
  {
    if (net.IsServer()) return;
    if (force) sent = -1;
    var rpc = net.GetServerRPC();
    if (rpc == null || sent == LimitManager.Active) return;
    sent = LimitManager.Active;
    ZPackage package = new();
    LimitManager.Current.Serialize(ref package);
    package.Write(LimitManager.Active);
    rpc.Invoke(RPC_REQUEST, package);
  }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_RequestValidSimulationDistance))]
public class ZNet_RequestValidSimulationDistance
{
  static void Postfix(ZNet __instance, ZRpc rpc, ZPackage pkg)
  {
    if (!__instance.IsServer()) return;
    PeerActive.Set(__instance.GetPeer(rpc), pkg.GetPos() < pkg.Size() ? pkg.ReadInt() : null);
  }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
public class ZNet_PeerInfoActive
{
  static void Postfix(ZNet __instance)
  {
    PeerActive.Send(__instance, true);
  }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
public class ZNet_DisconnectActive
{
  static void Postfix(ZNet __instance, ZNetPeer peer)
  {
    if (__instance.IsServer())
      PeerActive.Remove(peer.m_uid);
  }
}

[HarmonyPatch(typeof(ZDOMan))]
public class ZDOMan_PeerActive
{
  [HarmonyPatch(nameof(ZDOMan.ReleaseNearbyZDOS))]
  [HarmonyPatch(nameof(ZDOMan.IsInPeerActiveArea))]
  [HarmonyPrefix]
  static void Prefix(long uid, out (float Range, float Rounded) __state)
  {
    __state = (LimitManager.ActiveRange, LimitManager.ActiveRangeRounded);
    PeerActive.Apply(uid);
  }

  [HarmonyPatch(nameof(ZDOMan.ReleaseNearbyZDOS))]
  [HarmonyPatch(nameof(ZDOMan.IsInPeerActiveArea))]
  [HarmonyFinalizer]
  static Exception? Finalizer((float Range, float Rounded) __state, Exception? __exception)
  {
    LimitManager.ActiveRange = __state.Range;
    LimitManager.ActiveRangeRounded = __state.Rounded;
    return __exception;
  }
}
