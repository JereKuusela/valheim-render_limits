using BepInEx;
using HarmonyLib;

namespace RenderLimits;

[BepInPlugin(GUID, NAME, VERSION)]
public class RenderLimits : BaseUnityPlugin
{
  const string GUID = "render_limits";
  const string NAME = "Render Limits";
  const string VERSION = "1.14";

  public void Awake()
  {
    Configuration.Init(Config);
    new Harmony(GUID).PatchAll();
  }
}

[HarmonyPatch(typeof(GraphicsSettingsManager), nameof(GraphicsSettingsManager.ApplyQualitySettings))]
public class ApplyQualitySettings
{
  static void Postfix()
  {
    Configuration.InitSettings();
  }
}
[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
public class SetCommands
{
  static void Postfix()
  {
    new ForceActiveCommand();
  }
}