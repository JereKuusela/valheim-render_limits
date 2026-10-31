using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
namespace RenderLimits;

[HarmonyPatch]
public static class DistantObjects
{
  public const string RPC_DISTANT_OBJECTS = "RenderLimits_DistantObjects";
  private const int MaximumPrefabs = 1000;

  private static readonly HashSet<int> None = [];
  private static HashSet<int>? lights;

  private static HashSet<int> Lights()
  {
    if (lights != null) return lights;
    if (!ZNetScene.instance) return None;
    lights = [];
    foreach (var prefab in ZNetScene.instance.m_prefabs)
    {
      if (prefab.GetComponentInChildren<LightLod>(true))
        lights.Add(prefab.name.GetStableHashCode());
    }
    return lights;
  }

  private static HashSet<int> Local() => Configuration.DistantLights ? Lights() : None;

  public static void Send()
  {
    var net = ZNet.instance;
    if (!net || net.IsServer()) return;
    var prefabs = Local();
    ZPackage package = new();
    package.Write(prefabs.Count);
    foreach (var prefab in prefabs)
      package.Write(prefab);
    net.GetServerRPC()?.Invoke(RPC_DISTANT_OBJECTS, package);
  }

  public static void RPC_DistantObjects(ZRpc rpc, ZPackage package)
  {
    var count = package.ReadInt();
    HashSet<int> prefabs = [];
    if (count >= 0 && count <= MaximumPrefabs)
    {
      for (var i = 0; i < count; i++)
        prefabs.Add(package.ReadInt());
    }
    PeerActive.Get(ZNet.instance.GetPeer(rpc)).DistantObjects = prefabs;
  }

  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix]
  static void ZNetScene_Awake() => Send();

  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.FindDistantObjects)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> FindDistantObjects_Transpiler(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ZDO), nameof(ZDO.Distant))))
      .Set(OpCodes.Call, AccessTools.Method(typeof(DistantObjects), nameof(IsDistant)))
      .InstructionEnumeration();

  static bool IsDistant(ZDO zdo) => zdo.Distant || For(PeerActive.Context).Contains(zdo.GetPrefab());

  static HashSet<int> For(ZNetPeer? peer)
  {
    if (peer == null) return Local();
    return Configuration.AllowCustomDistantObjects ? PeerActive.Get(peer).DistantObjects : None;
  }
}
