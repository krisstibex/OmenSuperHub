using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmenSuperHub {
  /// <summary>
  /// 本地配置文件管理器，替代 Windows 注册表
  /// 配置文件存储在 %AppData%\OmenSuperHub\config.json
  /// </summary>
  public class ConfigManager {
    private static readonly string ConfigDirectory = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "OmenSuperHub"
    );

    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

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
      /// 预设配置
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
    /// 初始化配置管理器，创建目录（如果不存在）
    /// </summary>
    public static void Initialize() {
      lock (_lockObject) {
        try {
          if (!Directory.Exists(ConfigDirectory)) {
            Directory.CreateDirectory(ConfigDirectory);
          }

          if (File.Exists(ConfigFilePath)) {
            LoadFromFile();
          } else {
            _config = new ConfigData();
            SaveToFile();
          }
        } catch (Exception ex) {
          Logger.Error($"ConfigManager Initialize failed: {ex.Message}");
          _config = new ConfigData();
        }
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
        }
      } catch (Exception ex) {
        Logger.Error($"Failed to load config from file: {ex.Message}");
        _config = new ConfigData();
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
    /// 获取配置值
    /// </summary>
    public static T Get<T>(string key, T defaultValue = default) {
      lock (_lockObject) {
        if (_config == null) return defaultValue;

        try {
          var property = typeof(ConfigData).GetProperty(key, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
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
    /// 设置配置值
    /// </summary>
    public static void Set<T>(string key, T value) {
      lock (_lockObject) {
        if (_config == null) return;

        try {
          var property = typeof(ConfigData).GetProperty(key, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
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
    /// 获取配置文件路径（用于备份或管理）
    /// </summary>
    public static string GetConfigPath() {
      return ConfigFilePath;
    }

    /// <summary>
    /// 获取配置目录路径
    /// </summary>
    public static string GetConfigDirectory() {
      return ConfigDirectory;
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
