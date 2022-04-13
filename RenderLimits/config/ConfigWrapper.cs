using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using ServerSync;

namespace Service;

public class ConfigWrapper {

  private ConfigFile ConfigFile;
  private ConfigSync ConfigSync;
  public ConfigWrapper(string command, ConfigFile configFile, ConfigSync configSync) {
    ConfigFile = configFile;
    ConfigSync = configSync;

    new Terminal.ConsoleCommand(command, "[key] [value] - Toggles or sets a config value.", (Terminal.ConsoleEventArgs args) => {
      if (args.Length < 2) return;
      if (!SettingHandlers.TryGetValue(args[1].ToLower(), out var handler)) return;
      if (args.Length == 2)
        handler(args.Context, "");
      else
        handler(args.Context, string.Join(" ", args.Args.Skip(2)));
    }, optionsFetcher: () => SettingHandlers.Keys.ToList());
  }
  public ConfigEntry<bool> BindLocking(string group, string name, bool value, ConfigDescription description) {
    var configEntry = ConfigFile.Bind(group, name, value, description);
    Register(configEntry);
    var syncedConfigEntry = ConfigSync.AddLockingConfigEntry(configEntry);
    syncedConfigEntry.SynchronizedConfig = true;
    return configEntry;
  }
  public ConfigEntry<bool> BindLocking(string group, string name, bool value, string description) => BindLocking(group, name, value, new ConfigDescription(description));
  public ConfigEntry<T> Bind<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true) {
    var configEntry = ConfigFile.Bind(group, name, value, description);
    Register(configEntry);
    var syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
    syncedConfigEntry.SynchronizedConfig = synchronizedSetting;
    return configEntry;
  }
  public ConfigEntry<T> Bind<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => Bind(group, name, value, new ConfigDescription(description), synchronizedSetting);
  private static void AddMessage(Terminal context, string message) {
    context.AddString(message);
    Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, message);
  }
  private Dictionary<string, ConfigEntryBase> Settings = new();
  private Dictionary<string, Action<Terminal, string>> SettingHandlers = new();
  private void Register(ConfigEntry<bool> setting) {
    var name = setting.Definition.Key;
    var key = name.ToLower().Replace(' ', '_');
    SettingHandlers.Add(key, (Terminal terminal, string value) => Toggle(terminal, setting, name, value));
  }
  private void Register(ConfigEntry<string> setting) {
    var name = setting.Definition.Key;
    var key = name.ToLower().Replace(' ', '_');
    SettingHandlers.Add(key, (Terminal terminal, string value) => SetValue(terminal, setting, name, value));
  }
  private void Register<T>(ConfigEntry<T> setting) {
    var name = setting.Definition.Key;
    var key = name.ToLower().Replace(' ', '_');
    SettingHandlers.Add(key, (Terminal terminal, string value) => SetValue(terminal, setting, name, value));
  }
  private static string State(bool value) => value ? "enabled" : "disabled";
  private static string Flag(bool value) => value ? "Removed" : "Added";
  private static void Toggle(Terminal context, ConfigEntry<bool> setting, string name, string value) {
    if (value == "") setting.Value = !setting.Value;
    else if (value == "1") setting.Value = true;
    else if (value == "0") setting.Value = false;
    AddMessage(context, $"{name} {State(setting.Value)}.");
  }
  public static int TryParseInt(string value, int defaultValue) {
    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) return result;
    return defaultValue;
  }
  public static int TryParseInt(ConfigEntry<string> setting) {
    if (int.TryParse(setting.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) return result;
    return TryParseInt((string)setting.DefaultValue, 0);
  }
  private static float TryParseFloat(string value, float defaultValue) {
    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) return result;
    return defaultValue;
  }
  public static float TryParseFloat(ConfigEntry<string> setting) {
    if (float.TryParse(setting.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) return result;
    return TryParseFloat((string)setting.DefaultValue, 0f);
  }
  private static void SetValue(Terminal context, ConfigEntry<int> setting, string name, string value) {
    if (value == "") {
      AddMessage(context, $"{name}: {setting.Value}.");
      return;
    }
    setting.Value = TryParseInt(value, (int)setting.DefaultValue);
    AddMessage(context, $"{name} set to {setting.Value}.");
  }
  private static void SetValue(Terminal context, ConfigEntry<float> setting, string name, string value) {
    if (value == "") {
      AddMessage(context, $"{name}: {setting.Value}.");
      return;
    }
    setting.Value = TryParseFloat(value, (float)setting.DefaultValue);
    AddMessage(context, $"{name} set to {setting.Value}.");
  }
  private static void SetValue<T>(Terminal context, ConfigEntry<T> setting, string name, string value) {
    if (value == "") {
      AddMessage(context, $"{name}: {setting.Value}.");
      return;
    }
    setting.Value = (T)(object)value;
    AddMessage(context, $"{name} set to {value}.");
  }
}
