using System.IO;
using BepInEx;
using HarmonyLib;

namespace RenderLimits;
[BepInPlugin(GUID, NAME, VERSION)]
public class RenderLimits : BaseUnityPlugin {
  const string GUID = "render_limits";
  const string LEGACY_GUID = "valheim.jere.render_limits";
  const string NAME = "Render Limits";
  const string VERSION = "1.4";
  public static ServerSync.ConfigSync ConfigSync = new(GUID)
  {
    DisplayName = NAME,
    CurrentVersion = VERSION,
    MinimumRequiredVersion = "1.4"
  };

  public void Awake() {
    var config = Path.Combine(Path.GetDirectoryName(Config.ConfigFilePath), $"{GUID}.cfg");
    Configuration.Init(ConfigSync, Config);
    new Harmony(GUID).PatchAll();
  }
}

[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
public class SetCommands {
  static void Postfix() {
    new ForceActiveCommand();
  }
}