using HarmonyLib;
using UnityEngine;
namespace RenderLimits;

[HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.GetNrOfInstances), typeof(GameObject), typeof(Vector3), typeof(float), typeof(bool), typeof(bool))]
public class SpawnLimits
{
  static void Prefix(ref Vector3 center, ref float maxRange)
  {
    // With original setting, nothing to do.
    if (ZoneSystem.instance.m_activeArea == 2)
      return;
    if (maxRange == 0f)
    {
      // Default loaded is 5x5 zones, so (5 * 64) ^ 2 = 102400 area.
      // As radius, that's sqrt(102400 / Mathf.PI) = 181.
      maxRange = 181f;
      if (center == Vector3.zero && ZNet.instance)
      {
        center.x = ZNet.instance.m_referencePosition.x;
        center.y = ZNet.instance.m_referencePosition.y;
        center.z = ZNet.instance.m_referencePosition.z;
      }
    }
  }
}
