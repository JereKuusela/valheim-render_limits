using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace RenderLimits;

public class PlayerLimits(int loadedArea, int activeArea, int generatedArea, HashSet<Vector2i> forceActive)
{

  // Default is 2 for active and 2 for active distant.
  // Active area is actually the loaded area (just named poorly).

  public int ActiveArea = Math.Max(0, activeArea);
  // One buffer zone is needed to prevent things dropping out of map or falling apart.
  public int LoadedArea = Math.Max(activeArea + 1, loadedArea);
  public int GeneratedArea = Math.Max(0, generatedArea - loadedArea);
  public int TotalArea = generatedArea;
  public HashSet<Vector2i> ForceActive { get; set; } = forceActive != null ? [.. forceActive] : [];
}

public static class LimitManager
{
  public static string RPC_PLAYERSETTINGS = "RenderLimits_PlayerSettings";
  private static readonly Dictionary<long, PlayerLimits> PlayerLimits = [];
  private static long CurrentId = 0;
  private static int MaxTotalArea = 0;
  public static void SetCurrentPlayer(long uid)
  {
    CurrentId = uid;
  }

  // Get or create player limits
  public static PlayerLimits GetPlayerLimits(long uid)
  {
    if (!PlayerLimits.ContainsKey(uid))
    {
      // Unmodded clients should use default limits.
      PlayerLimits[uid] = new PlayerLimits(1, 2, 4, []);
    }
    return PlayerLimits[uid];
  }

  // Update player limits
  public static void SetPlayerLimits(long uid, int loadedArea, int activeArea, int generatedArea, HashSet<Vector2i> forceActive)
  {
    PlayerLimits[uid] = new PlayerLimits(loadedArea, activeArea, generatedArea, forceActive);
    MaxTotalArea = CalculateMaxTotal();
  }

  // Remove player limits when they disconnect
  public static void RemovePlayer(long uid)
  {
    PlayerLimits.Remove(uid);
    MaxTotalArea = CalculateMaxTotal();
  }

  private static int CalculateMaxTotal() => PlayerLimits.Values.Max(limits => limits.TotalArea);
  public static int GetMaxTotal() => MaxTotalArea;
  public static PlayerLimits GetCurrent() => GetPlayerLimits(CurrentId);

  // GetCurrent would probably work but more explicit this way.
  public static PlayerLimits GetLocalLimits() => GetPlayerLimits(0);

  public static void UpdateLocalLimits()
  {
    SetPlayerLimits(0, Configuration.LoadedArea, Configuration.ActiveArea, Configuration.GeneratedArea, Configuration.ForceActive);

    if (Player.m_localPlayer)
    {
      var uid = Player.m_localPlayer.GetPlayerID();
      SetPlayerLimits(uid, Configuration.LoadedArea, Configuration.ActiveArea, Configuration.GeneratedArea, Configuration.ForceActive);
      SendPlayerLimitsToServer();
    }
  }

  // Send player limits to server via RPC
  private static void SendPlayerLimitsToServer()
  {
    if (!ZNet.instance || ZNet.instance.IsServer())
      return;
    var server = ZNet.instance.GetServerRPC();
    ZPackage package = new();
    package.Write(Configuration.LoadedArea);
    package.Write(Configuration.ActiveArea);
    package.Write(Configuration.GeneratedArea);
    package.Write(Configuration.ForceActive.Count);
    foreach (var zone in Configuration.ForceActive)
      package.Write(zone);

    server?.Invoke(RPC_PLAYERSETTINGS, package);
  }

  // Receive player limits from client (server-side)
  public static void RPC_PlayerSettings(ZRpc rpc, ZPackage pkg)
  {
    if (!ZNet.instance.IsServer())
      return;
    ZNetPeer peer = ZNet.instance.GetPeer(rpc);
    var loadedArea = pkg.ReadInt();
    var activeArea = pkg.ReadInt();
    var generatedArea = Math.Min(Configuration.MaximumGeneratedArea, pkg.ReadInt());
    HashSet<Vector2i> forceActive = [];
    if (Configuration.AllowForceActive)
    {
      var forceActiveCount = pkg.ReadInt();
      for (int i = 0; i < forceActiveCount; i++)
        forceActive.Add(pkg.ReadVector2i());
    }
    SetPlayerLimits(peer.m_uid, loadedArea, activeArea, generatedArea, forceActive);
  }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
public class ZNet_RPC_PeerInfo_Patch
{
  static void Postfix(ZNet __instance, ZRpc rpc)
  {
    ZNetPeer peer = __instance.GetPeer(rpc);
    if (peer.m_uid == 0)
      return;
    if (__instance.IsServer())
      rpc.Register<ZPackage>(LimitManager.RPC_PLAYERSETTINGS, LimitManager.RPC_PlayerSettings);
    else
      LimitManager.UpdateLocalLimits();
  }
}
[HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
public class ZNet_Disconnect
{
  static void Postfix(ZNet __instance, ZNetPeer peer)
  {
    if (__instance.IsServer())
      LimitManager.RemovePlayer(peer.m_uid);
  }
}
