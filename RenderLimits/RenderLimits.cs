using System.IO;
using BepInEx;
using HarmonyLib;

namespace RenderLimits;
[BepInPlugin(GUID, NAME, VERSION)]
public class ExpandWorld : BaseUnityPlugin {
  const string GUID = "render_limits";
  const string LEGACY_GUID = "valheim.jere.render_limits";
  const string NAME = "Render Limits";
  const string VERSION = "1.3";
  ServerSync.ConfigSync ConfigSync = new(GUID)
  {
    DisplayName = NAME,
    CurrentVersion = VERSION,
    MinimumRequiredVersion = "1.1.0"
  };

  public void Awake() {
    var legacyConfig = Path.Combine(Path.GetDirectoryName(Config.ConfigFilePath), $"{LEGACY_GUID}.cfg");
    var config = Path.Combine(Path.GetDirectoryName(Config.ConfigFilePath), $"{GUID}.cfg");
    if (File.Exists(legacyConfig)) {
      if (File.Exists(config))
        File.Delete(legacyConfig);
      else
        File.Move(legacyConfig, config);
    }
    Configuration.Init(ConfigSync, Config);
    Harmony harmony = new("valheim.jere.render_limits");
    harmony.PatchAll();
  }
}
