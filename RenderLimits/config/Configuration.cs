using System;
using BepInEx.Configuration;
using ServerSync;
using Service;
namespace RenderLimits;
public class Configuration {
#nullable disable
  public static ConfigEntry<bool> configLocked;
  public static bool Locked => configLocked.Value;
  public static ConfigEntry<string> configActivateArea;
  public static int ActiveArea => Math.Max(1, ConfigWrapper.TryParseInt(configActivateArea));
  public static ConfigEntry<string> configLoadedArea;
  public static int LoadedArea => Math.Max(ActiveArea, ConfigWrapper.TryParseInt(configLoadedArea));
  public static ConfigEntry<string> configGeneratedArea;
  public static int GeneratedArea => Math.Max(LoadedArea, ConfigWrapper.TryParseInt(configGeneratedArea));
  public static ConfigEntry<string> configRealTerrainVisibility;
  public static int RealTerrainVisibility => ConfigWrapper.TryParseInt(configRealTerrainVisibility);
  public static ConfigEntry<string> configSpawnLimit;
  public static int SpawnLimit => ConfigWrapper.TryParseInt(configSpawnLimit);


  public static void Init(ConfigSync configSync, ConfigFile configFile) {
    ConfigWrapper wrapper = new("render_config", configFile, configSync);
    var section = "1. General";
    configLocked = wrapper.BindLocking(section, "Locked", false, "If locked on the server, the config can't be edited by clients.");
    configActivateArea = wrapper.Bind(section, "Active area", "2", "Amounts of zones that are active around the player.");
    configActivateArea.SettingChanged += (e, s) => {
      ZoneSystemActive.Update();
    };
    configLoadedArea = wrapper.Bind(section, "Loaded area", "3", "Amounts of zones loaded around the player.");
    configLoadedArea.SettingChanged += (e, s) => {
      ZoneSystemActive.Update();
    };
    configGeneratedArea = wrapper.Bind(section, "Generated area", "5", "Amounts of zones generated around the player.");
    configGeneratedArea.SettingChanged += (e, s) => {
      ZoneSystemActive.Update();
    };
    configRealTerrainVisibility = wrapper.Bind(section, "Real terrain visibility", "180", "Visibility in meters.");
    configRealTerrainVisibility.SettingChanged += (e, s) => {
      TerrainVisibility.Update();
    };
    configSpawnLimit = wrapper.Bind(section, "Spawn limit", "200", "How many meters away the spawn limits are checked. 0 for all loaded objects (base game behavior).");
  }
}
