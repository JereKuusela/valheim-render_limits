using BepInEx;
using HarmonyLib;

namespace RenderLimits;
[BepInPlugin("valheim.jere.render_limits", "RenderLimits", "1.2.0.0")]
public class ExpandWorld : BaseUnityPlugin {
  ServerSync.ConfigSync ConfigSync = new("valheim.jere.render_limits")
  {
    DisplayName = "RenderLimits",
    CurrentVersion = "1.1.0",
    MinimumRequiredVersion = "1.1.0"
  };

  public void Awake() {
    Configuration.Init(ConfigSync, Config);
    Harmony harmony = new("valheim.jere.render_limits");
    harmony.PatchAll();
  }
}
