using System;
using BepInEx.Configuration;
using HarmonyLib;

namespace RenderLimits;

public static class LimitManager
{
  public static SimulationDistance Current = SimulationDistance.OriginalDistance;
  public static int RequestedActive => Math.Max(0, Configuration.ActiveArea);

  private static bool batching = false;
  private static bool changed = false;
  private static bool maximumChanged = false;
  public static void Reload(ConfigFile config)
  {
    batching = true;
    try
    {
      config.Reload();
    }
    finally
    {
      batching = false;
    }
    if (!changed) return;
    changed = false;
    Update();
  }
  public static void UpdateMaximum()
  {
    maximumChanged = true;
    Update();
  }
  public static void Update()
  {
    if (batching)
    {
      changed = true;
      return;
    }
    var total = Math.Min(Math.Max(1, Configuration.GeneratedArea), Configuration.MaximumGeneratedArea);
    var near = Math.Min(Math.Max(1, Configuration.LoadedArea), total);
    var far = total - near;
    Current = new SimulationDistance(near, far, Configuration.ClassicShape);
    if (ZNet.instance) Sync(ZNet.instance);
    if (maximumChanged && ZNet.instance && ZNet.instance.IsServer())
      PeerActive.Revalidate(ZNet.instance);
    maximumChanged = false;
  }

  // Replaces the vanilla handshake, which makes the host's own settings the limit for every client.
  public static void Sync(ZNet net)
  {
    if (net.IsServer())
      net.ApplySimulationDistance(Current);
    else
      PeerActive.Send(net);
  }

  // The only server limit: Maximum generated zones, whatever the host plays with.
  public static SimulationDistance Limit(SimulationDistance requested)
  {
    var max = Configuration.MaximumGeneratedArea;
    var near = Math.Max(1, Math.Min(requested.NearSimulationDistance, max));
    var far = Math.Max(0, Math.Min(requested.FarSimulationDistance, max - near));
    return new SimulationDistance(near, far, requested.IsClassic);
  }

  public static SimulationDistance Min(SimulationDistance a, SimulationDistance b) =>
    new(Math.Min(a.NearSimulationDistance, b.NearSimulationDistance), Math.Min(a.FarSimulationDistance, b.FarSimulationDistance), a.IsClassic && b.IsClassic);

  public static SimulationDistance Max(SimulationDistance a, SimulationDistance b) =>
    new(Math.Max(a.NearSimulationDistance, b.NearSimulationDistance), Math.Max(a.FarSimulationDistance, b.FarSimulationDistance), a.IsClassic || b.IsClassic);

  // Active zones must stay inside loaded zones: every zone within the active square has to be loaded.
  public static int MaxActive(SimulationDistance distance)
  {
    var near = distance.NearSimulationDistance;
    if (near <= 1) return 0;
    if (distance.IsClassic) return near - 1;
    var radius = near + 0.5f;
    var active = 0;
    while (2 * (active + 1) * (active + 1) < radius * radius) active++;
    return active;
  }
}

[HarmonyPatch]
public static class ZNetPatches
{
  [HarmonyPatch(typeof(GraphicsSettingsManager), nameof(GraphicsSettingsManager.GetSimulationDistance))]
  [HarmonyPrefix]
  static bool GetSimulationDistance(ref SimulationDistance __result)
  {
    __result = LimitManager.Current;
    return false;
  }

  [HarmonyPatch(typeof(ZNet), nameof(ZNet.SimulationDistanceServerHandshake))]
  [HarmonyPrefix]
  static bool SimulationDistanceServerHandshake(ZNet __instance)
  {
    LimitManager.Sync(__instance);
    return false;
  }

  // Vanilla keeps the larger of the two when they are not strictly comparable, so a cap would not hold.
  [HarmonyPatch(typeof(ZNet), nameof(ZNet.GetSyncedSimulationDistance))]
  [HarmonyPrefix]
  static bool GetSyncedSimulationDistance(ZNet __instance, ref SimulationDistance __result)
  {
    __result = PeerActive.Context?.m_simulationDistance ?? LimitManager.Min(__instance.m_simulationDistance, LimitManager.Current);
    return false;
  }

  // Server returns valid settings for the client, so they can be applied directly (vanilla doesn't constrain them correctly).
  [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_ValidatedSimulationDistance))]
  [HarmonyPrefix]
  static bool ValidatedSimulationDistance(ZNet __instance, ZPackage pkg)
  {
    __instance.ApplySimulationDistance(SimulationDistance.Deserialize(ref pkg));
    return false;
  }

  // Vanilla clients send no active value, so the previous (default 1) stays.
  [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_RequestValidSimulationDistance))]
  [HarmonyPrefix]
  static bool RequestValidSimulationDistance(ZNet __instance, ZRpc rpc, ZPackage pkg)
  {
    if (!__instance.IsServer()) return false;
    var peer = __instance.GetPeer(rpc);
    var data = PeerActive.Get(peer);
    var requested = SimulationDistance.Deserialize(ref pkg);
    data.Requested = requested;
    if (pkg.GetPos() < pkg.Size())
      data.RequestedActive = Math.Max(0, pkg.ReadInt());
    if (pkg.GetPos() < pkg.Size())
      data.DistantLights = pkg.ReadBool();
    PeerActive.Apply(peer, LimitManager.Limit(requested));
    return false;
  }

  [HarmonyPatch(typeof(ZNet), nameof(ZNet.Start))]
  [HarmonyPostfix]
  static void Start(ZNet __instance)
  {
    Configuration.SelectWorld();
    ForceZones.Accepted = __instance.IsServer();
    if (__instance.IsDedicated())
      __instance.ApplySimulationDistance(LimitManager.Current);
  }

  [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
  [HarmonyPostfix]
  static void Disconnect(ZNetPeer peer) => PeerActive.Remove(peer);

  [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
  [HarmonyPostfix]
  static void PeerInfo(ZNet __instance, ZRpc rpc)
  {
    if (__instance.GetPeer(rpc).m_uid == 0) return;
    if (__instance.IsServer())
    {
      rpc.Register<ZPackage>(ForceZones.RPC_FORCE_ACTIVE, ForceZones.RPC_ForceActive);
      return;
    }
    rpc.Register<bool>(ForceZones.RPC_FORCE_ACTIVE_REPLY, ForceZones.RPC_ForceActiveReply);
    Configuration.SelectWorld();
    PeerActive.Send(__instance);
    ForceZones.Send();
  }
}
