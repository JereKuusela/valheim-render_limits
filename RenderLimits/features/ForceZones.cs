using System;
using System.Collections.Generic;
using HarmonyLib;
namespace RenderLimits;

public static class ForceZones
{
  public const string RPC_FORCE_ACTIVE = "RenderLimits_ForceActive";
  public const string RPC_FORCE_ACTIVE_REPLY = "RenderLimits_ForceActiveReply";

  private static readonly HashSet<Vector2s> None = [];
  private static readonly Dictionary<long, HashSet<Vector2s>> Peers = [];

  public static bool Accepted = true;
  public static HashSet<Vector2s>? Window = null;
  public static HashSet<Vector2s> Local => Accepted ? Configuration.ForceActive : None;
  public static HashSet<Vector2s> Current => Window ?? Local;

  public static HashSet<Vector2s> For(long uid) =>
    uid == ZDOMan.instance.m_sessionID ? Local : Peers.TryGetValue(uid, out var zones) ? zones : None;

  public static void Remove(long uid) => Peers.Remove(uid);

  public static void Send()
  {
    var net = ZNet.instance;
    if (!net || net.IsServer()) return;
    ZPackage package = new();
    package.Write(Configuration.ForceActive.Count);
    foreach (var zone in Configuration.ForceActive)
      package.Write(zone);
    net.GetServerRPC()?.Invoke(RPC_FORCE_ACTIVE, package);
  }

  public static void RPC_ForceActive(ZRpc rpc, ZPackage package)
  {
    var uid = ZNet.instance.GetPeer(rpc).m_uid;
    var previous = For(uid);
    HashSet<Vector2s> zones = [];
    var accepted = Configuration.AllowForceActive;
    if (accepted)
    {
      var count = package.ReadInt();
      var limit = (int)(WorldGenerator.worldSize * 1.1f / ZoneSystem.instance.m_zoneSize);
      accepted = count >= 0 && count <= Configuration.MaximumForceActive;
      for (var i = 0; accepted && i < count; i++)
      {
        var zone = package.ReadVector2s();
        accepted = Math.Abs(zone.x) <= limit && Math.Abs(zone.y) <= limit;
        zones.Add(zone);
      }
    }
    if (!accepted) zones.Clear();
    Peers[uid] = zones;
    Release(previous, zones, uid);
    rpc.Invoke(RPC_FORCE_ACTIVE_REPLY, accepted);
  }

  public static void RPC_ForceActiveReply(ZRpc rpc, bool accepted)
  {
    Accepted = accepted;
  }

  private static void Release(HashSet<Vector2s> previous, HashSet<Vector2s> current, long uid)
  {
    var manager = ZDOMan.instance;
    HashSet<ZoneSystem.SectorIndex> visited = [];
    List<ZDO> objects = [];
    foreach (var zone in previous)
    {
      if (!current.Contains(zone))
        manager.FindObjects(zone, objects, visited);
    }
    foreach (var zdo in objects)
    {
      if (zdo.GetOwner() == uid && !manager.IsInPeerActiveArea(zdo.GetPosition(), uid))
        zdo.SetOwner(0L);
    }
  }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
public class ZNet_PeerInfo
{
  static void Postfix(ZNet __instance, ZRpc rpc)
  {
    if (__instance.GetPeer(rpc).m_uid == 0) return;
    if (__instance.IsServer())
    {
      rpc.Register<ZPackage>(ForceZones.RPC_FORCE_ACTIVE, ForceZones.RPC_ForceActive);
      return;
    }
    rpc.Register<bool>(ForceZones.RPC_FORCE_ACTIVE_REPLY, ForceZones.RPC_ForceActiveReply);
    ForceZones.Accepted = true;
    Configuration.SelectWorld();
    ForceZones.Send();
  }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
public class ZNet_Disconnect
{
  static void Postfix(ZNet __instance, ZNetPeer peer)
  {
    if (__instance.IsServer())
      ForceZones.Remove(peer.m_uid);
  }
}
