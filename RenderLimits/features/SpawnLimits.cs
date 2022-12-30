using HarmonyLib;
using UnityEngine;
namespace RenderLimits;

[HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.GetNrOfInstances), new[] { typeof(GameObject), typeof(Vector3), typeof(float), typeof(bool), typeof(bool) })]
public class SpawnLimits
{
  static void Prefix(GameObject prefab, ref Vector3 center, ref float maxRange)
  {
    if (maxRange == 0f)
    {
      maxRange = Configuration.SpawnLimit;
      if (center == Vector3.zero && ZNet.instance)
      {
        center.x = ZNet.instance.m_referencePosition.x;
        center.y = ZNet.instance.m_referencePosition.y;
        center.z = ZNet.instance.m_referencePosition.z;
      }
    }
  }
}
