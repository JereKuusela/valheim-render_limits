using BepInEx;
using HarmonyLib;

namespace RenderLimits;
[BepInPlugin(GUID, NAME, VERSION)]
public class RenderLimits : BaseUnityPlugin
{
  const string GUID = "render_limits";
  const string NAME = "Render Limits";
  const string VERSION = "1.7";
  public static ServerSync.ConfigSync ConfigSync = new(GUID)
  {
    DisplayName = NAME,
    CurrentVersion = VERSION,
    MinimumRequiredVersion = VERSION,
    IsLocked = true,
  };

  public void Awake()
  {
    Configuration.Init(ConfigSync, Config);
    new Harmony(GUID).PatchAll();
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