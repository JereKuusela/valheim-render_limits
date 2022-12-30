using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using ServerSync;

namespace Service;

public class ConfigWrapper
{

  private ConfigFile ConfigFile;
  private ConfigSync ConfigSync;
  public ConfigWrapper(string command, ConfigFile configFile, ConfigSync configSync)
  {
    ConfigFile = configFile;
    ConfigSync = configSync;

    new Terminal.ConsoleCommand(command, "[key] [value] - Toggles or sets a config value.", (Terminal.ConsoleEventArgs args) =>
    {
      if (args.Length < 2) return;
      if (!SettingHandlers.TryGetValue(args[1].ToLower(), out var handler)) return;
      if (args.Length == 2)
        handler(args.Context, "");
      else
        handler(args.Context, string.Join(" ", args.Args.Skip(2)));
    }, optionsFetcher: () => SettingHandlers.Keys.ToList());
  }
  private void HandleChange(ConfigEntry<string> configEntry, Action onChange)
  {
    configEntry.SettingChanged += (s, e) => onChange();
    onChange();
  }
  private void HandleChange(ConfigEntry<string> configEntry, Action<string> onChange)
  {
    configEntry.SettingChanged += (s, e) => onChange(configEntry.Value);
    onChange(configEntry.Value);
  }
  private void HandleChange(ConfigEntry<string> configEntry, Action<int> onChange)
  {
    var onSettingChanged = (string value) =>
    {
      if (ConfigWrapper.TryParseInt(value, out var result))
        onChange(result);
    };
    HandleChange(configEntry, onSettingChanged);
  }
  private void HandleChange(ConfigEntry<string> configEntry, Action<float> onChange)
  {
    var onSettingChanged = (string value) =>
    {
      if (ConfigWrapper.TryParseFloat(value, out var result))
        onChange(result);
    };
    HandleChange(configEntry, onSettingChanged);
  }
  public ConfigEntry<string> Bind(string group, string name, string value, string description, Action onChange)
  {
    var configEntry = ConfigFile.Bind(group, name, value, description);
    Register(configEntry);
    HandleChange(configEntry, onChange);
    return configEntry;
  }
  public ConfigEntry<string> Bind(string group, string name, string value, string description, Action<string> onChange)
  {
    var configEntry = ConfigFile.Bind(group, name, value, description);
    Register(configEntry);
    HandleChange(configEntry, onChange);
    return configEntry;
  }
  public ConfigEntry<string> Bind(string group, string name, string value, string description, Action<int> onChange)
  {
    var configEntry = ConfigFile.Bind(group, name, value, description);
    Register(configEntry);
    HandleChange(configEntry, onChange);
    return configEntry;
  }
  public ConfigEntry<string> Bind(string group, string name, string value, string description, Action<float> onChange)
  {
    var configEntry = ConfigFile.Bind(group, name, value, description);
    Register(configEntry);
    HandleChange(configEntry, onChange);
    return configEntry;
  }
  public ConfigEntry<T> BindSynced<T>(string group, string name, T value, string description)
  {
    var configEntry = ConfigFile.Bind(group, name, value, description);
    Register(configEntry);
    var syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
    syncedConfigEntry.SynchronizedConfig = true;
    return configEntry;
  }
  private static void AddMessage(Terminal context, string message)
  {
    context.AddString(message);
    Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, message);
  }
  private Dictionary<string, ConfigEntryBase> Settings = new();
  private Dictionary<string, Action<Terminal, string>> SettingHandlers = new();
  private void Register(ConfigEntry<string> setting)
  {
    var name = setting.Definition.Key;
    var key = name.ToLower().Replace(' ', '_');
    SettingHandlers.Add(key, (Terminal terminal, string value) => SetValue(terminal, setting, name, value));
  }
  private void Register<T>(ConfigEntry<T> setting)
  {
    var name = setting.Definition.Key;
    var key = name.ToLower().Replace(' ', '_');
    SettingHandlers.Add(key, (Terminal terminal, string value) => SetValue(terminal, setting, name, value));
  }
  public static int ParseInt(string value, int defaultValue)
  {
    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) return result;
    return defaultValue;
  }
  public static bool TryParseInt(ConfigEntry<string> setting, out int result)
  {
    return TryParseInt(setting.Value, out result);
  }
  public static bool TryParseInt(string value, out int result)
  {
    return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
  }
  public static int ParseInt(ConfigEntry<string> setting)
  {
    if (int.TryParse(setting.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) return result;
    return ParseInt((string)setting.DefaultValue, 0);
  }
  private static float ParseFloat(string value, float defaultValue)
  {
    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) return result;
    return defaultValue;
  }
  public static float ParseFloat(ConfigEntry<string> setting)
  {
    if (float.TryParse(setting.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) return result;
    return ParseFloat((string)setting.DefaultValue, 0f);
  }
  public static bool TryParseFloat(string value, out float result)
  {
    return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
  }
  private static void SetValue(Terminal context, ConfigEntry<int> setting, string name, string value)
  {
    if (value == "")
    {
      AddMessage(context, $"{name}: {setting.Value}.");
      return;
    }
    setting.Value = ParseInt(value, (int)setting.DefaultValue);
    AddMessage(context, $"{name} set to {setting.Value}.");
  }
  private static void SetValue(Terminal context, ConfigEntry<float> setting, string name, string value)
  {
    if (value == "")
    {
      AddMessage(context, $"{name}: {setting.Value}.");
      return;
    }
    setting.Value = ParseFloat(value, (float)setting.DefaultValue);
    AddMessage(context, $"{name} set to {setting.Value}.");
  }
  private static void SetValue<T>(Terminal context, ConfigEntry<T> setting, string name, string value)
  {
    if (value == "")
    {
      AddMessage(context, $"{name}: {setting.Value}.");
      return;
    }
    setting.Value = (T)(object)value;
    AddMessage(context, $"{name} set to {value}.");
  }
}
