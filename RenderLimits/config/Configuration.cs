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
  public static ConfigEntry<bool> configClassicShape;
  public static bool ClassicShape => configClassicShape.Value;
  public static ConfigEntry<string> configRealTerrainVisibility;
  public static int RealTerrainVisibility => ConfigWrapper.ParseInt(configRealTerrainVisibility);
  public static ConfigEntry<string> configClutterVisibility;
  public static int ClutterVisibility => ConfigWrapper.ParseInt(configClutterVisibility);
  public static ConfigEntry<string> configLightLodMultiplier;
  public static float LightLodMultiplier => ConfigWrapper.ParseFloat(configLightLodMultiplier);
  public static ConfigEntry<bool> configDistantLights;
  public static bool DistantLights => configDistantLights.Value;
  public static ConfigEntry<string> configForceActive;
  public static HashSet<Vector2s> ForceActive = [];
  private static readonly Dictionary<long, HashSet<Vector2s>> forceActiveByWorld = [];
  private static long world;

  public static ConfigEntry<string> configMaximumGeneratedArea;
  public static int MaximumGeneratedArea => Math.Max(1, ConfigWrapper.ParseInt(configMaximumGeneratedArea));
  public static ConfigEntry<bool> configAllowForceActive;
  public static bool AllowForceActive => configAllowForceActive.Value;
  public static ConfigEntry<bool> configAllowCustomDistantObjects;
  public static bool AllowCustomDistantObjects => configAllowCustomDistantObjects.Value;
  public static ConfigEntry<string> configMaximumForceActive;
  public static int MaximumForceActive => Math.Max(0, ConfigWrapper.ParseInt(configMaximumForceActive));

  public static ConfigEntry<string> configPixelLightCount;
  public static ConfigEntry<string> configShadowCascades;
  public static ConfigEntry<string> configShadowQuality;
  public static ConfigEntry<string> configShadowDistance;
  public static ConfigEntry<string> configShadowResolution;
  public static ConfigEntry<string> configLodBias;

  public static ConfigEntry<KeyboardShortcut> keyActiveIncrease, keyActiveDecrease;
  public static ConfigEntry<KeyboardShortcut> keyLoadedIncrease, keyLoadedDecrease;
  public static ConfigEntry<KeyboardShortcut> keyDistantIncrease, keyDistantDecrease;
  public static ConfigEntry<KeyboardShortcut> keyBothIncrease, keyBothDecrease;

  private static void Adjust(ConfigEntry<string> entry, int delta, int min)
  {
    var max = Math.Max(min, MaximumGeneratedArea);
    entry.Value = Math.Max(min, Math.Min(max, ConfigWrapper.ParseInt(entry) + delta)).ToString();
  }
  public static void HandleKeybinds()
  {
    if (!Player.m_localPlayer || Chat.instance && Chat.instance.HasFocus() || Console.IsVisible() || TextInput.IsVisible()) return;
    var step = 0;
    void check(ConfigEntry<KeyboardShortcut> up, ConfigEntry<KeyboardShortcut> down, Action<int> apply)
    {
      if (up.Value.IsDown()) step = 1;
      else if (down.Value.IsDown()) step = -1;
      else return;
      apply(step);
    }
    check(keyActiveIncrease, keyActiveDecrease, d => Adjust(configActivateArea, d, 0));
    check(keyLoadedIncrease, keyLoadedDecrease, d => Adjust(configLoadedArea, d, 1));
    check(keyDistantIncrease, keyDistantDecrease, d => Adjust(configGeneratedArea, d, 1));
    check(keyBothIncrease, keyBothDecrease, d =>
    {
      Adjust(configLoadedArea, d, 1);
      Adjust(configGeneratedArea, d, 1);
    });
  }

  private static void ParseForceActive(string value)
  {
    forceActiveByWorld.Clear();
    foreach (var entry in value.Split(';'))
    {
      var parts = entry.Split(':');
      if (parts.Length != 2 || !long.TryParse(parts[0].Trim(), out var uid)) continue;
      HashSet<Vector2s> zones = [];
      foreach (var pair in parts[1].Split('|').Select(s => s.Trim().Split(',')).Where(s => s.Length == 2))
      {
        if (int.TryParse(pair[0], out var x) && int.TryParse(pair[1], out var y))
          zones.Add(new Vector2s(x, y));
      }
      forceActiveByWorld[uid] = zones;
    }
    SelectWorld();
  }
  public static void SelectWorld()
  {
    world = ZNet.m_world?.m_uid ?? 0;
    if (!forceActiveByWorld.TryGetValue(world, out var zones))
      forceActiveByWorld[world] = zones = [];
    ForceActive = zones;
  }
  public static void SaveForceActive()
  {
    forceActiveByWorld[world] = ForceActive;
    configForceActive.Value = string.Join(";", forceActiveByWorld.Where(s => s.Value.Count > 0).Select(s => $"{s.Key}:{string.Join("|", s.Value.Select(z => $"{z.x},{z.y}"))}"));
  }

  public static void Init(ConfigFile configFile)
  {
    ConfigWrapper wrapper = new("render_config", configFile);

    var section = "1. Zones";
    configActivateArea = wrapper.Bind(section, "Active zones", "1", "Amounts of zones that are active around the player. Creatures are active in this area.", LimitManager.Update);
    configLoadedArea = wrapper.Bind(section, "Loaded zones", "2", "Amounts of zones loaded around the player. Structures are visible in this area. ", LimitManager.Update);
    configGeneratedArea = wrapper.Bind(section, "Generated zones", "4", "Amounts of zones generated around the player. Large static objects like trees are visible in this area.", LimitManager.Update);
    configClassicShape = wrapper.Bind(section, "Classic shape", true, "Zones are loaded in a square. If false, the corners are cut and the area is rounded (active zones are reduced to fit).", LimitManager.Update);
    configRealTerrainVisibility = wrapper.Bind(section, "Real terrain visibility", "0", "Visibility in meters. If 0, the game decides from loaded area.", Heightmap.ApplySettingsOnAll);
    configForceActive = wrapper.Bind(section, "Force active", "", "Zones that are always active. Saved for each world.", () =>
    {
      ParseForceActive(configForceActive.Value);
      ForceZones.Send();
    });

    section = "2. Quality";
    configClutterVisibility = wrapper.Bind(section, "Clutter visibility", "45", "Visibility in meters.", ClutterDistance.Update);
    configDistantLights = wrapper.Bind(section, "Distant lights", false, "Show lights in generated areas (causes them to be loaded from further away).", DistantObjects.Send);
    configLightLodMultiplier = wrapper.Bind(section, "Light distance multiplier", "1", "Multiplies the distance when lights are disabled.", LightDistance.Update);
    configLodBias = wrapper.Bind(section, "Lod bias", "", "Level of detail limit (increase to show smaller objects, vanilla is from 1 to 5 but even 100 works).", (int value) => QualitySettings.lodBias = value);
    configPixelLightCount = wrapper.Bind(section, "Pixel light count", "", "Light detail (bigger the better, vanilla is from 2 to 8).", value => QualitySettings.pixelLightCount = value);
    configShadowCascades = wrapper.Bind(section, "Shadow cascades", "", "Improves shadows near the camera (bigger the better, vanilla is from 2 to 4).", value => QualitySettings.shadowCascades = value);
    configShadowQuality = wrapper.Bind(section, "Shadow quality", "", "0: off, 1: hard only, 2: all.", (int value) => QualitySettings.shadows = (ShadowQuality)Math.Max(0, Math.Min(2, value)));

    configShadowDistance = wrapper.Bind(section, "Shadow distance", "", "Max distance for shadows in meters (vanilla is from 80 to 150).", (float value) => QualitySettings.shadowDistance = value);
    configShadowResolution = wrapper.Bind(section, "Shadow resolution", "", "Shadow quality. From 0 to 3.", (int value) => QualitySettings.shadowResolution = (ShadowResolution)Math.Max(0, Math.Min(3, value)));

    section = "3. Server limits";
    configMaximumGeneratedArea = wrapper.Bind(section, "Maximum generated zones", "10", "Maximum generated zones. Also limits loaded zones and active zones. Applies to every client.", LimitManager.UpdateMaximum);
    configAllowForceActive = wrapper.Bind(section, "Allow force active", false, "Whether clients can have force active areas.");
    configAllowCustomDistantObjects = wrapper.Bind(section, "Allow custom distant objects", true, "Whether clients can load objects (like lights) from generated zones.");
    configMaximumForceActive = wrapper.Bind(section, "Maximum force active zones", "64", "Maximum force active zones for each client.", (int value) => { });

    section = "4. Keybinds";
    keyActiveIncrease = configFile.Bind(section, "Increase active zones", KeyboardShortcut.Empty, "Key to increase active zones.");
    keyActiveDecrease = configFile.Bind(section, "Decrease active zones", KeyboardShortcut.Empty, "Key to decrease active zones.");
    keyLoadedIncrease = configFile.Bind(section, "Increase loaded zones", KeyboardShortcut.Empty, "Key to increase loaded zones.");
    keyLoadedDecrease = configFile.Bind(section, "Decrease loaded zones", KeyboardShortcut.Empty, "Key to decrease loaded zones.");
    keyDistantIncrease = configFile.Bind(section, "Increase generated zones", KeyboardShortcut.Empty, "Key to increase generated (distant) zones.");
    keyDistantDecrease = configFile.Bind(section, "Decrease generated zones", KeyboardShortcut.Empty, "Key to decrease generated (distant) zones.");
    keyBothIncrease = configFile.Bind(section, "Increase loaded and generated zones", KeyboardShortcut.Empty, "Key to increase both loaded and generated zones.");
    keyBothDecrease = configFile.Bind(section, "Decrease loaded and generated zones", KeyboardShortcut.Empty, "Key to decrease both loaded and generated zones.");

    ParseForceActive(configForceActive.Value);
    LimitManager.Update();
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
