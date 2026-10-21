using System;
using System.Collections.Generic;
namespace RenderLimits;

public class PeerData
{
  public SimulationDistance? Requested;
  public int RequestedActive = 1;
  public HashSet<int> DistantLights = [];
  public HashSet<Vector2s> Zones = [];
}

// Some functions don't have peer id as parameter (because game handling is bugged and always uses server settings).
// So must manage global context manually.
public static class PeerActive
{
  private const string RPC_REQUEST = "RPC_RequestValidSimulationDistance";
  private const string RPC_VALIDATED = "RPC_ValidatedSimulationDistance";
  private static readonly Dictionary<ZNetPeer, PeerData> Peers = [];

  // Peer being evaluated by the server's active area checks. Null is the local player.
  public static ZNetPeer? Context;

  public static ZNetPeer? Enter(ZNetPeer? peer)
  {
    var previous = Context;
    Context = peer;
    return previous;
  }

  public static PeerData Get(ZNetPeer peer)
  {
    if (!Peers.TryGetValue(peer, out var data))
      Peers[peer] = data = new();
    return data;
  }

  public static void Remove(ZNetPeer peer) => Peers.Remove(peer);

  public static int Active(ZNetPeer? peer)
  {
    if (peer == null)
      return Math.Min(LimitManager.RequestedActive, LimitManager.MaxActive(ZNet.instance.GetSyncedSimulationDistance()));
    return Math.Min(Get(peer).RequestedActive, LimitManager.MaxActive(peer.m_simulationDistance));
  }

  public static void Send(ZNet net)
  {
    var rpc = net.GetServerRPC();
    if (rpc == null) return;
    ZPackage package = new();
    LimitManager.Current.Serialize(ref package);
    package.Write(LimitManager.RequestedActive);
    rpc.Invoke(RPC_REQUEST, package);
  }

  public static void Apply(ZNetPeer peer, SimulationDistance validated)
  {
    peer.m_simulationDistance = validated;
    ZPackage package = new();
    validated.Serialize(ref package);
    peer.m_rpc.Invoke(RPC_VALIDATED, package);
  }

  // Server can change the limit, which means client settings must be reconstrained.
  public static void Revalidate(ZNet net)
  {
    foreach (var peer in net.GetPeers())
    {
      if (!Peers.TryGetValue(peer, out var data) || data.Requested is not SimulationDistance requested) continue;
      var validated = LimitManager.Limit(requested);
      if (!validated.Equals(peer.m_simulationDistance))
        Apply(peer, validated);
    }
  }

  // Server generates ghost zones around every peer, so it must cover the widest of them.
  public static SimulationDistance Widest(SimulationDistance own)
  {
    foreach (var peer in ZNet.instance.GetPeers())
      own = LimitManager.Max(own, peer.m_simulationDistance);
    return own;
  }
}

