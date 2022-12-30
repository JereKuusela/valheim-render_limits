using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ServerSync;
using Service;
using UnityEngine;
namespace RenderLimits;
public class Configuration
{
#nullable disable
  public static ConfigEntry<string> configActivateArea;
  public static int ActiveArea => Math.Max(1, ConfigWrapper.ParseInt(configActivateArea));
  public static ConfigEntry<string> configLoadedArea;
  public static int LoadedArea => Math.Max(ActiveArea, ConfigWrapper.ParseInt(configLoadedArea));
  public static ConfigEntry<string> configDistantArea;
  public static int DistantArea => Math.Max(LoadedArea, ConfigWrapper.ParseInt(configDistantArea));
  public static ConfigEntry<string> configRealTerrainVisibility;
  public static int RealTerrainVisibility => ConfigWrapper.ParseInt(configRealTerrainVisibility);
  public static ConfigEntry<string> configSpawnLimit;
  public static int SpawnLimit => ConfigWrapper.ParseInt(configSpawnLimit);
  public static ConfigEntry<string> configClutterVisibility;
  public static int ClutterVisibility => ConfigWrapper.ParseInt(configClutterVisibility);
  public static ConfigEntry<string> configForceActive;
  public static HashSet<Vector2i> ForceActive = new();

  public static ConfigEntry<string> configPixelLightCount;
  public static ConfigEntry<string> configShadowCascades;
  public static ConfigEntry<string> configShadowQuality;
  public static ConfigEntry<string> configShadowDistance;
  public static ConfigEntry<string> configShadowResolution;
  public static ConfigEntry<string> configLodBias;

  private static void ParseForceActive(string value)
  {
    ForceActive = value.Split('|').Select(s => s.Trim()).Select(s => s.Split(',')).Where(s => s.Length == 2).Select(s =>
    {
      try
      {
        return new Vector2i(int.Parse(s[0]), int.Parse(s[1]));
      }
      catch
      {
        return new Vector2i();
      }
    }).ToHashSet();
  }
  public static void SaveForceActive()
  {
    configForceActive.Value = string.Join("|", ForceActive.Select(s => $"{s.x},{s.y}"));
  }

  public static void Init(ConfigSync configSync, ConfigFile configFile)
  {
    ConfigWrapper wrapper = new("render_config", configFile, configSync);

    var section = "1. Visibility";
    configActivateArea = wrapper.Bind(section, "Active area", "2", "Amounts of zones that are active around the player. Creatures are visible in this area.", ZoneSystemActive.Update);
    configLoadedArea = wrapper.Bind(section, "Loaded area", "3", "Amounts of zones loaded around the player. Structures are visible in this area. ", ZoneSystemActive.Update);
    configDistantArea = wrapper.Bind(section, "Distant area", "5", "Amounts of zones generated around the player. ig static objects like trees are visible in this area.", ZoneSystemActive.Update);
    configRealTerrainVisibility = wrapper.Bind(section, "Real terrain visibility", "180", "Visibility in meters.", TerrainVisibility.Update);
    configClutterVisibility = wrapper.Bind(section, "Clutter visibility", "45", "Visibility in meters.", ClutterDistance.Update);
    configForceActive = wrapper.Bind(section, "Force active", "", "Zones that are always active.", ParseForceActive);

    section = "2. Quality";

    configLodBias = wrapper.Bind(section, "Lod bias", "", "Level of detail limit (increase to show smaller objects, vanilla is from 1 to 5 but even 100 works).", (int value) => QualitySettings.lodBias = value);

    configPixelLightCount = wrapper.Bind(section, "Pixel light count", "", "Light detail (bigger the better, vanilla is from 2 to 8).", value => QualitySettings.pixelLightCount = value);

    configShadowCascades = wrapper.Bind(section, "Shadow cascades", "", "Improves shadows near the camera (bigger the better, vanilla is from 2 to 4).", value => QualitySettings.shadowCascades = value);

    configShadowQuality = wrapper.Bind(section, "Shadow quality", "", "0: off, 1: hard only, 2: all.", (int value) =>
    {
      QualitySettings.shadows = (ShadowQuality)(Math.Max(0, Math.Min(2, value)));
    });

    configShadowDistance = wrapper.Bind(section, "Shadow distance", "", "Max distance for shadows in meters (vanilla is from 80 to 150).", (float value) => QualitySettings.shadowDistance = value);

    configShadowResolution = wrapper.Bind(section, "Shadow resolution", "", "Shadow quality. From 0 to 3.", (int value) =>
    {
      QualitySettings.shadowResolution = (ShadowResolution)(Math.Max(0, Math.Min(3, value)));
    });
    section = "3. Synced settings";
    configSpawnLimit = wrapper.BindSynced(section, "Spawn limit", "200", "How many meters away the spawn limits are checked. 0 for all loaded objects (base game behavior).");
  }
}
