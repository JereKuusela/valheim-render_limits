using System;
using BepInEx.Configuration;
using HarmonyLib;

namespace RenderLimits;

public static class LimitManager
{
  public static SimulationDistance Current = SimulationDistance.OriginalDistance;
  public static int Active = 1;
  public static float ActiveRange = 1.5f;
  public static float ActiveRangeRounded = 1.75f;

  private static bool batching = false;
  private static bool changed = false;
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
    Current = new SimulationDistance(near, far, true);
    Active = Math.Max(0, Math.Min(Configuration.ActiveArea, near - 1));
    SetRange(Active, near == 1);
    var net = ZNet.instance;
    if (!net) return;
    if (net.IsDedicated())
    {
      net.ApplySimulationDistance(Current);
      ZPackage package = new();
      Current.Serialize(ref package);
      foreach (var peer in net.GetPeers())
        peer.m_rpc.Invoke("RPC_ValidatedSimulationDistance", package);
    }
    else
    {
      net.SimulationDistanceServerHandshake();
      PeerActive.Send(net);
    }
  }
  public static void SetRange(int active, bool single)
  {
    ActiveRange = single ? 1f : active + 0.5f;
    ActiveRangeRounded = active + 0.75f;
  }
}

[HarmonyPatch(typeof(GraphicsSettingsManager), nameof(GraphicsSettingsManager.GetSimulationDistance))]
public class GetSimulationDistance
{
  static bool Prefix(ref SimulationDistance __result)
  {
    __result = LimitManager.Current;
    return false;
  }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.Start))]
public class ZNet_Start
{
  static void Postfix(ZNet __instance)
  {
    Configuration.SelectWorld();
    if (__instance.IsDedicated())
      __instance.ApplySimulationDistance(LimitManager.Current);
  }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_ValidatedSimulationDistance))]
public class ZNet_ValidatedSimulationDistance
{
  static SimulationDistance Last;
  static void Postfix(ZNet __instance)
  {
    var synced = __instance.GetSyncedSimulationDistance();
    if (synced.Equals(Last)) return;
    Last = synced;
    __instance.SimulationDistanceServerHandshake();
  }
}
