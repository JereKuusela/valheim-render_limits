using HarmonyLib;
using UnityEngine;
namespace RenderLimits;

[HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.GetNrOfInstances), new[] { typeof(GameObject), typeof(Vector3), typeof(float), typeof(bool), typeof(bool) })]
public class SpawnLimits {
  static void Prefix(GameObject prefab, ref Vector3 center, ref float maxRange) {
    if (maxRange == 0f) {
      maxRange = Configuration.SpawnLimit;
      if (center == Vector3.zero) {
        center.x = prefab.transform.position.x;
        center.y = prefab.transform.position.y;
        center.z = prefab.transform.position.z;
      }
    }
  }
}
