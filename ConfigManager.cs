using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmenSuperHub {
  /// <summary>
  /// 本地配置文件管理器，替代 Windows 注册表
  /// 配置文件存储在应用程序所在目录：config.json
  /// </summary>
  public class ConfigManager {
    private static readonly string ConfigFilePath = Path.Combine(
      AppDomain.CurrentDomain.BaseDirectory,
      "config.json"
    );

    private static ConfigData _config;
    private static readonly object _lockObject = new object();

    /// <summary>
    /// 配置数据模型
    /// </summary>
    [Serializable]
    public class ConfigData {
      [JsonPropertyName("currentPreset")]
      public string CurrentPreset { get; set; } = "PresetCustom1";

      [JsonPropertyName("presetCustom1Name")]
      public string PresetCustom1Name { get; set; } = "自定义预设1";

      [JsonPropertyName("presetCustom2Name")]
      public string PresetCustom2Name { get; set; } = "自定义预设2";

      [JsonPropertyName("presetCustom3Name")]
      public string PresetCustom3Name { get; set; } = "自定义预设3";

      [JsonPropertyName("fanTable")]
      public string FanTable { get; set; } = "cool";

      [JsonPropertyName("fanControl")]
      public string FanControl { get; set; } = "auto";

      [JsonPropertyName("tempSensitivity")]
      public string TempSensitivity { get; set; } = "high";

      [JsonPropertyName("cpuPower")]
      public string CpuPower { get; set; } = "null";

      [JsonPropertyName("tgpPower")]
      public string TgpPower { get; set; } = "on";

      [JsonPropertyName("ppabPower")]
      public string PpabPower { get; set; } = "on";

      [JsonPropertyName("dState")]
      public string DState { get; set; } = "normal";

      [JsonPropertyName("gpuClock")]
      public int GpuClock { get; set; } = 0;

      [JsonPropertyName("dBVersion")]
      public int DBVersion { get; set; } = 2;

      [JsonPropertyName("autoStart")]
      public string AutoStart { get; set; } = "off";

      [JsonPropertyName("alreadyRead")]
      public int AlreadyRead { get; set; } = 0;

      [JsonPropertyName("customIcon")]
      public string CustomIcon { get; set; } = "original";

      [JsonPropertyName("omenKey")]
      public string OmenKey { get; set; } = "default";

      [JsonPropertyName("monitorGPU")]
      public bool MonitorGPU { get; set; } = true;

      [JsonPropertyName("monitorCPU")]
      public bool MonitorCPU { get; set; } = true;

      [JsonPropertyName("monitorFan")]
      public bool MonitorFan { get; set; } = false;

      [JsonPropertyName("monitorRefreshRate")]
      public string MonitorRefreshRate { get; set; } = "low";

      [JsonPropertyName("tempDisplayMode")]
      public string TempDisplayMode { get; set; } = "smoothed";

      [JsonPropertyName("floatingBarLoc")]
      public string FloatingBarLoc { get; set; } = "left";

      [JsonPropertyName("floatingBar")]
      public string FloatingBar { get; set; } = "off";

      [JsonPropertyName("dataLocalize")]
      public string DataLocalize { get; set; } = "off";

      [JsonPropertyName("appLanguage")]
      public string AppLanguage { get; set; } = "zh-CN";

      [JsonPropertyName("tppPower")]
      public string TppPower { get; set; } = "null";

      [JsonPropertyName("iccMax")]
      public string IccMax { get; set; } = "null";

      [JsonPropertyName("acLoadLine")]
      public string AcLoadLine { get; set; } = "null";

      [JsonPropertyName("floatingBarSize")]
      public int FloatingBarSize { get; set; } = 48;

      /// <summary>
      /// 预设配置字典
      /// </summary>
      [JsonPropertyName("presets")]
      public Dictionary<string, PresetConfig> Presets { get; set; } = new Dictionary<string, PresetConfig>();
    }

    /// <summary>
    /// 预设配置模型
    /// </summary>
    [Serializable]
    public class PresetConfig {
      [JsonPropertyName("fanTable")]
      public string FanTable { get; set; } = "cool";

      [JsonPropertyName("fanControl")]
      public string FanControl { get; set; } = "auto";

      [JsonPropertyName("tempSensitivity")]
      public string TempSensitivity { get; set; } = "high";

      [JsonPropertyName("cpuPower")]
      public string CpuPower { get; set; } = "null";

      [JsonPropertyName("tgpPower")]
      public string TgpPower { get; set; } = "on";

      [JsonPropertyName("ppabPower")]
      public string PpabPower { get; set; } = "on";

      [JsonPropertyName("dState")]
      public string DState { get; set; } = "normal";

      [JsonPropertyName("gpuClock")]
      public int GpuClock { get; set; } = 0;

      [JsonPropertyName("tppPower")]
      public string TppPower { get; set; } = "null";

      [JsonPropertyName("iccMax")]
      public string IccMax { get; set; } = "null";

      [JsonPropertyName("acLoadLine")]
      public string AcLoadLine { get; set; } = "null";
    }

    /// <summary>
    /// 初始化配置管理器
    /// 1. 如果存在本地 JSON 配置文件，直接加载
    /// 2. 如果不存在，尝试从注册表迁移
    /// 3. 如果注册表也不存在，使用默认配置
    /// </summary>
    public static void Initialize() {
      lock (_lockObject) {
        try {
          if (File.Exists(ConfigFilePath)) {
            LoadFromFile();
            Logger.Info($"Configuration loaded from: {ConfigFilePath}");
          } else {
            // 尝试从注册表迁移配置
            if (MigrateFromRegistry()) {
              Logger.Info("Configuration migrated from registry to: " + ConfigFilePath);
            } else {
              _config = new ConfigData();
              SaveToFile();
              Logger.Info($"New configuration created at: {ConfigFilePath}");
            }
          }
        } catch (Exception ex) {
          Logger.Error($"ConfigManager Initialize failed: {ex.Message}");
          _config = new ConfigData();
        }
      }
    }

    /// <summary>
    /// 从注册表迁移配置（仅在本地文件不存在时调用）
    /// </summary>
    private static bool MigrateFromRegistry() {
      try {
        using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\OmenSuperHub")) {
          if (key == null) {
            return false; // 注册表中无配置
          }

          _config = new ConfigData();

          // 迁移主配置项
          _config.CurrentPreset = (string)key.GetValue("CurrentPreset", _config.CurrentPreset) ?? _config.CurrentPreset;
          _config.PresetCustom1Name = (string)key.GetValue("PresetCustom1Name", _config.PresetCustom1Name) ?? _config.PresetCustom1Name;
          _config.PresetCustom2Name = (string)key.GetValue("PresetCustom2Name", _config.PresetCustom2Name) ?? _config.PresetCustom2Name;
          _config.PresetCustom3Name = (string)key.GetValue("PresetCustom3Name", _config.PresetCustom3Name) ?? _config.PresetCustom3Name;
          _config.FanTable = (string)key.GetValue("FanTable", _config.FanTable) ?? _config.FanTable;
          _config.FanControl = (string)key.GetValue("FanControl", _config.FanControl) ?? _config.FanControl;
          _config.TempSensitivity = (string)key.GetValue("TempSensitivity", _config.TempSensitivity) ?? _config.TempSensitivity;
          _config.CpuPower = (string)key.GetValue("CpuPower", _config.CpuPower) ?? _config.CpuPower;
          _config.TgpPower = (string)key.GetValue("TgpPower", _config.TgpPower) ?? _config.TgpPower;
          _config.PpabPower = (string)key.GetValue("PpabPower", _config.PpabPower) ?? _config.PpabPower;
          _config.DState = (string)key.GetValue("DState", _config.DState) ?? _config.DState;
          _config.GpuClock = (int)key.GetValue("GpuClock", _config.GpuClock);
          _config.DBVersion = (int)key.GetValue("DBVersion", _config.DBVersion);
          _config.AutoStart = (string)key.GetValue("AutoStart", _config.AutoStart) ?? _config.AutoStart;
          _config.AlreadyRead = (int)key.GetValue("AlreadyRead", _config.AlreadyRead);
          _config.CustomIcon = (string)key.GetValue("CustomIcon", _config.CustomIcon) ?? _config.CustomIcon;
          _config.OmenKey = (string)key.GetValue("OmenKey", _config.OmenKey) ?? _config.OmenKey;
          _config.MonitorGPU = Convert.ToBoolean(key.GetValue("MonitorGPU", _config.MonitorGPU));
          _config.MonitorCPU = Convert.ToBoolean(key.GetValue("MonitorCPU", _config.MonitorCPU));
          _config.MonitorFan = Convert.ToBoolean(key.GetValue("MonitorFan", _config.MonitorFan));
          _config.MonitorRefreshRate = (string)key.GetValue("MonitorRefreshRate", _config.MonitorRefreshRate) ?? _config.MonitorRefreshRate;
          _config.TempDisplayMode = (string)key.GetValue("TempDisplayMode", _config.TempDisplayMode) ?? _config.TempDisplayMode;
          _config.FloatingBarLoc = (string)key.GetValue("FloatingBarLoc", _config.FloatingBarLoc) ?? _config.FloatingBarLoc;
          _config.FloatingBar = (string)key.GetValue("FloatingBar", _config.FloatingBar) ?? _config.FloatingBar;
          _config.DataLocalize = (string)key.GetValue("DataLocalize", _config.DataLocalize) ?? _config.DataLocalize;
          _config.AppLanguage = (string)key.GetValue("AppLanguage", _config.AppLanguage) ?? _config.AppLanguage;
          _config.TppPower = (string)key.GetValue("TppPower", _config.TppPower) ?? _config.TppPower;
          _config.IccMax = (string)key.GetValue("IccMax", _config.IccMax) ?? _config.IccMax;
          _config.AcLoadLine = (string)key.GetValue("AcLoadLine", _config.AcLoadLine) ?? _config.AcLoadLine;
          _config.FloatingBarSize = (int)key.GetValue("FloatingBarSize", _config.FloatingBarSize);

          // 迁移预设配置
          _config.Presets = new Dictionary<string, PresetConfig>();
          foreach (string presetKey in new[] { "PresetCustom1", "PresetCustom2", "PresetCustom3" }) {
            using (Microsoft.Win32.RegistryKey presetRegKey = key.OpenSubKey(presetKey)) {
              if (presetRegKey != null) {
                var preset = new PresetConfig {
                  FanTable = (string)presetRegKey.GetValue("FanTable", "cool") ?? "cool",
                  FanControl = (string)presetRegKey.GetValue("FanControl", "auto") ?? "auto",
                  TempSensitivity = (string)presetRegKey.GetValue("TempSensitivity", "high") ?? "high",
                  CpuPower = (string)presetRegKey.GetValue("CpuPower", "null") ?? "null",
                  TgpPower = (string)presetRegKey.GetValue("TgpPower", "on") ?? "on",
                  PpabPower = (string)presetRegKey.GetValue("PpabPower", "on") ?? "on",
                  DState = (string)presetRegKey.GetValue("DState", "normal") ?? "normal",
                  GpuClock = (int)presetRegKey.GetValue("GpuClock", 0),
                  TppPower = (string)presetRegKey.GetValue("TppPower", "null") ?? "null",
                  IccMax = (string)presetRegKey.GetValue("IccMax", "null") ?? "null",
                  AcLoadLine = (string)presetRegKey.GetValue("AcLoadLine", "null") ?? "null"
                };
                _config.Presets[presetKey] = preset;
              }
            }
          }

          SaveToFile();
          return true;
        }
      } catch (Exception ex) {
        Logger.Error($"Failed to migrate configuration from registry: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// 从文件读取配置
    /// </summary>
    private static void LoadFromFile() {
      try {
        string json = File.ReadAllText(ConfigFilePath);
        _config = JsonSerializer.Deserialize<ConfigData>(json);
        if (_config == null) {
          _config = new ConfigData();
          SaveToFile();
        }
      } catch (Exception ex) {
        Logger.Error($"Failed to load config from file: {ex.Message}");
        _config = new ConfigData();
        SaveToFile();
      }
    }

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    private static void SaveToFile() {
      try {
        var options = new JsonSerializerOptions {
          WriteIndented = true,
          DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        string json = JsonSerializer.Serialize(_config, options);
        File.WriteAllText(ConfigFilePath, json);
      } catch (Exception ex) {
        Logger.Error($"Failed to save config to file: {ex.Message}");
      }
    }

    /// <summary>
    /// 获取配置值（泛型）
    /// </summary>
    public static T Get<T>(string key, T defaultValue = default) {
      lock (_lockObject) {
        if (_config == null) return defaultValue;

        try {
          var property = typeof(ConfigData).GetProperty(
            key,
            System.Reflection.BindingFlags.IgnoreCase |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance
          );
          if (property != null) {
            var value = property.GetValue(_config);
            if (value != null && value is T typedValue) {
              return typedValue;
            }
          }
        } catch (Exception ex) {
          Logger.Error($"Failed to get config value for key '{key}': {ex.Message}");
        }

        return defaultValue;
      }
    }

    /// <summary>
    /// 设置配置值（泛型）
    /// </summary>
    public static void Set<T>(string key, T value) {
      lock (_lockObject) {
        if (_config == null) return;

        try {
          var property = typeof(ConfigData).GetProperty(
            key,
            System.Reflection.BindingFlags.IgnoreCase |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance
          );
          if (property != null && property.CanWrite) {
            property.SetValue(_config, value);
            SaveToFile();
          }
        } catch (Exception ex) {
          Logger.Error($"Failed to set config value for key '{key}': {ex.Message}");
        }
      }
    }

    /// <summary>
    /// 获取预设配置
    /// </summary>
    public static PresetConfig GetPreset(string presetKey) {
      lock (_lockObject) {
        if (_config?.Presets != null && _config.Presets.TryGetValue(presetKey, out var preset)) {
          return preset;
        }
        return new PresetConfig();
      }
    }

    /// <summary>
    /// 保存预设配置
    /// </summary>
    public static void SavePreset(string presetKey, PresetConfig preset) {
      lock (_lockObject) {
        if (_config == null) return;

        if (_config.Presets == null) {
          _config.Presets = new Dictionary<string, PresetConfig>();
        }

        _config.Presets[presetKey] = preset;
        SaveToFile();
      }
    }

    /// <summary>
    /// 获取配置文件路径
    /// </summary>
    public static string GetConfigPath() {
      return ConfigFilePath;
    }

    /// <summary>
    /// 重新加载配置（从文件）
    /// </summary>
    public static void Reload() {
      lock (_lockObject) {
        LoadFromFile();
      }
    }

    /// <summary>
    /// 重置为默认配置
    /// </summary>
    public static void ResetToDefault() {
      lock (_lockObject) {
        _config = new ConfigData();
        SaveToFile();
      }
    }
  }
}
