using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ServerSync;
using Service;
using UnityEngine;
namespace RenderLimits;
public class Configuration {
#nullable disable
  public static ConfigEntry<bool> configLocked;
  public static bool Locked => configLocked.Value;
  public static ConfigEntry<string> configActivateArea;
  public static int ActiveArea => Math.Max(1, ConfigWrapper.TryParseInt(configActivateArea));
  public static ConfigEntry<string> configLoadedArea;
  public static int LoadedArea => Math.Max(ActiveArea, ConfigWrapper.TryParseInt(configLoadedArea));
  public static ConfigEntry<string> configDistantArea;
  public static int DistantArea => Math.Max(LoadedArea, ConfigWrapper.TryParseInt(configDistantArea));
  public static ConfigEntry<string> configRealTerrainVisibility;
  public static int RealTerrainVisibility => ConfigWrapper.TryParseInt(configRealTerrainVisibility);
  public static ConfigEntry<string> configSpawnLimit;
  public static int SpawnLimit => ConfigWrapper.TryParseInt(configSpawnLimit);
  public static ConfigEntry<string> configClutterVisibility;
  public static int ClutterVisibility => ConfigWrapper.TryParseInt(configClutterVisibility);
  public static ConfigEntry<string> configLodBias;
  public static ConfigEntry<string> configForceActive;
  public static HashSet<Vector2i> ForceActive = new();

  private static void ParseForceActive() {
    ForceActive = configForceActive.Value.Split('|').Select(s => s.Trim()).Select(s => s.Split(',')).Where(s => s.Length == 2).Select(s => {
      try {
        return new Vector2i(int.Parse(s[0]), int.Parse(s[1]));
      } catch {
        return new Vector2i();
      }
    }).ToHashSet();
  }
  public static void SaveForceActive() {
    configForceActive.Value = string.Join("|", ForceActive.Select(s => $"{s.x},{s.y}"));
  }

  public static void Init(ConfigSync configSync, ConfigFile configFile) {
    ConfigWrapper wrapper = new("render_config", configFile, configSync);
    var section = "1. General";
    configLocked = wrapper.BindLocking(section, "Locked", false, "If locked on the server, the config can't be edited by clients.");
    configActivateArea = wrapper.Bind(section, "Active area", "2", "Amounts of zones that are active around the player. Creatures are visible in this area.");
    configActivateArea.SettingChanged += (s, e) => {
      ZoneSystemActive.Update();
    };
    configLoadedArea = wrapper.Bind(section, "Loaded area", "3", "Amounts of zones loaded around the player. Structures are visible in this area. ");
    configLoadedArea.SettingChanged += (s, e) => {
      ZoneSystemActive.Update();
    };
    configDistantArea = wrapper.Bind(section, "Distant area", "5", "Amounts of zones generated around the player. ig static objects like trees are visible in this area.");
    configDistantArea.SettingChanged += (s, e) => {
      ZoneSystemActive.Update();
    };
    configRealTerrainVisibility = wrapper.Bind(section, "Real terrain visibility", "180", "Visibility in meters.");
    configRealTerrainVisibility.SettingChanged += (s, e) => {
      TerrainVisibility.Update();
    };
    configClutterVisibility = wrapper.Bind(section, "Clutter visibility", "45", "Visibility in meters.");
    configClutterVisibility.SettingChanged += (s, e) => {
      ClutterDistance.Update();
    };
    configSpawnLimit = wrapper.Bind(section, "Spawn limit", "200", "How many meters away the spawn limits are checked. 0 for all loaded objects (base game behavior).");

    configLodBias = wrapper.Bind(section, "Lod bias", "5", "Level of detail limit (increase to show smaller objects).");
    configLodBias.SettingChanged += (s, e) => {
      QualitySettings.lodBias = ConfigWrapper.TryParseFloat(configLodBias);
    };
    QualitySettings.lodBias = ConfigWrapper.TryParseFloat(configLodBias);
    configForceActive = configFile.Bind(section, "Force active", "", "Zones that are always active.");
    configForceActive.SettingChanged += (s, e) => ParseForceActive();
    ParseForceActive();
  }
}
