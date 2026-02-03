using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Service;
using UnityEngine;
namespace RenderLimits;

public class Configuration
{
#nullable disable
  public static ConfigEntry<string> configActivateArea;
  public static int ActiveArea => ConfigWrapper.ParseInt(configActivateArea);
  public static ConfigEntry<string> configLoadedArea;
  public static int LoadedArea => ConfigWrapper.ParseInt(configLoadedArea);
  public static ConfigEntry<string> configGeneratedArea;
  public static int GeneratedArea => ConfigWrapper.ParseInt(configGeneratedArea);
  public static ConfigEntry<string> configRealTerrainVisibility;
  public static int RealTerrainVisibility => ConfigWrapper.ParseInt(configRealTerrainVisibility);
  public static ConfigEntry<string> configClutterVisibility;
  public static int ClutterVisibility => ConfigWrapper.ParseInt(configClutterVisibility);
  public static ConfigEntry<string> configForceActive;
  public static HashSet<Vector2i> ForceActive = [];

  public static ConfigEntry<string> configMaximumGeneratedArea;
  public static int MaximumGeneratedArea => Math.Max(1, ConfigWrapper.ParseInt(configMaximumGeneratedArea));
  public static ConfigEntry<bool> configAllowForceActive;
  public static bool AllowForceActive => configAllowForceActive.Value;

  public static ConfigEntry<string> configPixelLightCount;
  public static ConfigEntry<string> configShadowCascades;
  public static ConfigEntry<string> configShadowQuality;
  public static ConfigEntry<string> configShadowDistance;
  public static ConfigEntry<string> configShadowResolution;
  public static ConfigEntry<string> configLodBias;

  private static void ParseForceActive(string value)
  {
    ForceActive = [.. value.Split('|').Select(s => s.Trim()).Select(s => s.Split(',')).Where(s => s.Length == 2).Select(s =>
    {
      try
      {
        return new Vector2i(int.Parse(s[0]), int.Parse(s[1]));
      }
      catch
      {
        return new Vector2i();
      }
    })];
  }
  public static void SaveForceActive()
  {
    configForceActive.Value = string.Join("|", ForceActive.Select(s => $"{s.x},{s.y}"));
  }

  public static void Init(ConfigFile configFile)
  {
    ConfigWrapper wrapper = new("render_config", configFile);

    var section = "1. Zones";
    configActivateArea = wrapper.Bind(section, "Active zones", "1", "Amounts of zones that are active around the player. Creatures are active in this area.", () => { LimitManager.UpdateLocalLimits(); ZoneSystemPatches.Update(); });
    configLoadedArea = wrapper.Bind(section, "Loaded zones", "2", "Amounts of zones loaded around the player. Structures are visible in this area. ", () => { LimitManager.UpdateLocalLimits(); ZoneSystemPatches.Update(); TerrainVisibility.Update(); });
    configGeneratedArea = wrapper.Bind(section, "Generated zones", "4", "Amounts of zones generated around the player. Large static objects like trees are visible in this area.", () => { LimitManager.UpdateLocalLimits(); ZoneSystemPatches.Update(); });
    configRealTerrainVisibility = wrapper.Bind(section, "Real terrain visibility", "0", "Visibility in meters. If 0, automatically calculated from loaded area.", TerrainVisibility.Update);
    configForceActive = wrapper.Bind(section, "Force active", "", "Zones that are always active.", () => { ParseForceActive(configForceActive.Value); LimitManager.UpdateLocalLimits(); });

    section = "2. Quality";
    configClutterVisibility = wrapper.Bind(section, "Clutter visibility", "45", "Visibility in meters.", ClutterDistance.Update);
    configLodBias = wrapper.Bind(section, "Lod bias", "", "Level of detail limit (increase to show smaller objects, vanilla is from 1 to 5 but even 100 works).", (int value) => QualitySettings.lodBias = value);
    configPixelLightCount = wrapper.Bind(section, "Pixel light count", "", "Light detail (bigger the better, vanilla is from 2 to 8).", value => QualitySettings.pixelLightCount = value);
    configShadowCascades = wrapper.Bind(section, "Shadow cascades", "", "Improves shadows near the camera (bigger the better, vanilla is from 2 to 4).", value => QualitySettings.shadowCascades = value);
    configShadowQuality = wrapper.Bind(section, "Shadow quality", "", "0: off, 1: hard only, 2: all.", (int value) => QualitySettings.shadows = (ShadowQuality)Math.Max(0, Math.Min(2, value)));

    configShadowDistance = wrapper.Bind(section, "Shadow distance", "", "Max distance for shadows in meters (vanilla is from 80 to 150).", (float value) => QualitySettings.shadowDistance = value);
    configShadowResolution = wrapper.Bind(section, "Shadow resolution", "", "Shadow quality. From 0 to 3.", (int value) => QualitySettings.shadowResolution = (ShadowResolution)Math.Max(0, Math.Min(3, value)));

    section = "3. Server limits";
    configMaximumGeneratedArea = wrapper.Bind(section, "Maximum generated zones", "10", "Maximum generated zones that can be received from clients.", (int value) => { });
    configAllowForceActive = wrapper.Bind(section, "Allow force active", false, "Whether clients can have force active areas.");

    ParseForceActive(configForceActive.Value);
    LimitManager.UpdateLocalLimits();
  }

  public static void InitSettings()
  {
    // Game loads QualitySettings after Awake, so we need to set them here.
    if (ConfigWrapper.TryParseInt(configLodBias.Value, out var lodBias))
      QualitySettings.lodBias = lodBias;
    if (ConfigWrapper.TryParseInt(configPixelLightCount.Value, out var pixelLightCount))
      QualitySettings.pixelLightCount = pixelLightCount;
    if (ConfigWrapper.TryParseInt(configShadowCascades.Value, out var shadowCascades))
      QualitySettings.shadowCascades = shadowCascades;
    if (ConfigWrapper.TryParseInt(configShadowQuality.Value, out var shadowQuality))
      QualitySettings.shadows = (ShadowQuality)Math.Max(0, Math.Min(2, shadowQuality));
    if (ConfigWrapper.TryParseFloat(configShadowDistance.Value, out var shadowDistance))
      QualitySettings.shadowDistance = shadowDistance;
    if (ConfigWrapper.TryParseInt(configShadowResolution.Value, out var shadowResolution))
      QualitySettings.shadowResolution = (ShadowResolution)Math.Max(0, Math.Min(3, shadowResolution));
  }
}
