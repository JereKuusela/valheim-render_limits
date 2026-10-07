using System.IO;
using BepInEx;
using HarmonyLib;

namespace RenderLimits;

[BepInPlugin(GUID, NAME, VERSION)]
public class RenderLimits : BaseUnityPlugin
{
  const string GUID = "render_limits";
  const string NAME = "Render Limits";
  const string VERSION = "1.15";

  public void Awake()
  {
    Configuration.Init(Config);
    new Harmony(GUID).PatchAll();
    SetupWatcher();
  }

  private void SetupWatcher()
  {
    FileSystemWatcher watcher = new(Path.GetDirectoryName(Config.ConfigFilePath), Path.GetFileName(Config.ConfigFilePath));
    watcher.Changed += ReadConfigValues;
    watcher.Created += ReadConfigValues;
    watcher.Renamed += ReadConfigValues;
    watcher.IncludeSubdirectories = true;
    watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
    watcher.EnableRaisingEvents = true;
  }

  private void ReadConfigValues(object sender, FileSystemEventArgs e)
  {
    if (!File.Exists(Config.ConfigFilePath)) return;
    LimitManager.Reload(Config);
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