using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using static OmenSuperHub.GpuAppManager;
using static OmenSuperHub.OmenHardware;

namespace OmenSuperHub {
  static partial class Program {
    /// <summary>
    /// 从本地配置文件加载语言设置
    /// 替代原来的注册表读取
    /// </summary>
    static void LoadLanguageSetting() {
      try {
        // 首先从本地配置文件读取
        string savedLang = ConfigManager.Get<string>("AppLanguage", null);
        if (!string.IsNullOrEmpty(savedLang)) {
          appLanguage = savedLang;
        } else {
          // 首次运行，无配置值 → 使用系统语言
          appLanguage = GetSystemLanguage();
          // 立即将系统语言写入配置文件
          ConfigManager.Set("AppLanguage", appLanguage);
        }
      } catch { }
      ApplyLanguage(appLanguage);
    }

    /// <summary>
    /// 保存全部配置到本地文件
    /// 替代原来的注册表写入
    /// </summary>
    static void SaveConfig(string configName = null) {
      try {
        if (configName == null) {
          // 保存所有配置项
          ConfigManager.Set("CurrentPreset", currentPreset);
          ConfigManager.Set("PresetCustom1Name", presetCustom1Name);
          ConfigManager.Set("PresetCustom2Name", presetCustom2Name);
          ConfigManager.Set("PresetCustom3Name", presetCustom3Name);
          ConfigManager.Set("FanTable", fanTable);
          ConfigManager.Set("FanControl", fanControl);
          ConfigManager.Set("TempSensitivity", tempSensitivity);
          ConfigManager.Set("CpuPower", cpuPower);
          ConfigManager.Set("TgpPower", tgpPower);
          ConfigManager.Set("PpabPower", ppabPower);
          ConfigManager.Set("DState", dState);
          if (hasNVIDIAGpu) {
            ConfigManager.Set("GpuClock", gpuClock);
            ConfigManager.Set("DBVersion", DBVersion);
          }
          ConfigManager.Set("AutoStart", autoStart);
          ConfigManager.Set("AlreadyRead", alreadyRead);
          ConfigManager.Set("CustomIcon", customIcon);
          ConfigManager.Set("OmenKey", omenKey);
          if (hasNVIDIAGpu || hasAMDDiscreteGpu)
            ConfigManager.Set("MonitorGPU", monitorGPU);
          ConfigManager.Set("MonitorCPU", monitorCPU);
          ConfigManager.Set("MonitorFan", monitorFan);
          ConfigManager.Set("MonitorRefreshRate", monitorRefreshRate);
          ConfigManager.Set("TempDisplayMode", tempDisplayMode);
          ConfigManager.Set("FloatingBarLoc", floatingBarLoc);
          ConfigManager.Set("FloatingBar", floatingBar);
          ConfigManager.Set("DataLocalize", dataLocalize);
          ConfigManager.Set("AppLanguage", appLanguage);
          ConfigManager.Set("TppPower", tppPower);
          ConfigManager.Set("IccMax", iccMax);
          ConfigManager.Set("AcLoadLine", acLoadline);
          ConfigManager.Set("FloatingBarSize", textSize);
          SavePresetToConfig(currentPreset);
        } else {
          // 保存单个配置项
          switch (configName) {
            case "CurrentPreset":
              ConfigManager.Set("CurrentPreset", currentPreset);
              break;
            case "PresetCustom1Name":
              ConfigManager.Set("PresetCustom1Name", presetCustom1Name);
              break;
            case "PresetCustom2Name":
              ConfigManager.Set("PresetCustom2Name", presetCustom2Name);
              break;
            case "PresetCustom3Name":
              ConfigManager.Set("PresetCustom3Name", presetCustom3Name);
              break;
            case "FanTable":
              ConfigManager.Set("FanTable", fanTable);
              break;
            case "FanControl":
              ConfigManager.Set("FanControl", fanControl);
              break;
            case "TempSensitivity":
              ConfigManager.Set("TempSensitivity", tempSensitivity);
              break;
            case "CpuPower":
              ConfigManager.Set("CpuPower", cpuPower);
              break;
            case "TgpPower":
              ConfigManager.Set("TgpPower", tgpPower);
              break;
            case "PpabPower":
              ConfigManager.Set("PpabPower", ppabPower);
              break;
            case "DState":
              ConfigManager.Set("DState", dState);
              break;
            case "GpuClock":
              ConfigManager.Set("GpuClock", gpuClock);
              break;
            case "DBVersion":
              ConfigManager.Set("DBVersion", DBVersion);
              break;
            case "AutoStart":
              ConfigManager.Set("AutoStart", autoStart);
              break;
            case "AlreadyRead":
              ConfigManager.Set("AlreadyRead", alreadyRead);
              break;
            case "CustomIcon":
              ConfigManager.Set("CustomIcon", customIcon);
              break;
            case "OmenKey":
              ConfigManager.Set("OmenKey", omenKey);
              break;
            case "MonitorGPU":
              ConfigManager.Set("MonitorGPU", monitorGPU);
              break;
            case "MonitorCPU":
              ConfigManager.Set("MonitorCPU", monitorCPU);
              break;
            case "MonitorFan":
              ConfigManager.Set("MonitorFan", monitorFan);
              break;
            case "MonitorRefreshRate":
              ConfigManager.Set("MonitorRefreshRate", monitorRefreshRate);
              break;
            case "TempDisplayMode":
              ConfigManager.Set("TempDisplayMode", tempDisplayMode);
              break;
            case "FloatingBarSize":
              ConfigManager.Set("FloatingBarSize", textSize);
              break;
            case "FloatingBarLoc":
              ConfigManager.Set("FloatingBarLoc", floatingBarLoc);
              break;
            case "FloatingBar":
              ConfigManager.Set("FloatingBar", floatingBar);
              break;
            case "DataLocalize":
              ConfigManager.Set("DataLocalize", dataLocalize);
              break;
            case "TppPower":
              ConfigManager.Set("TppPower", tppPower);
              break;
            case "IccMax":
              ConfigManager.Set("IccMax", iccMax);
              break;
            case "AcLoadLine":
              ConfigManager.Set("AcLoadLine", acLoadline);
              break;
            case "AppLanguage":
              ConfigManager.Set("AppLanguage", appLanguage);
              break;
          }

          // 保存相关预设
          if (configName == "FanTable" || configName == "FanControl" || configName == "TempSensitivity" ||
              configName == "CpuPower" || configName == "TgpPower" || configName == "PpabPower" ||
              configName == "DState" || configName == "GpuClock" || configName == "TppPower" ||
              configName == "IccMax" || configName == "AcLoadLine") {
            SavePresetToConfig(currentPreset);
          }
        }
      } catch (Exception ex) {
        Logger.Error($"Error saving configuration: {ex.Message}");
      }
    }

    /// <summary>
    /// 保存预设配置到本地文件
    /// </summary>
    static void SavePresetToConfig(string presetKey) {
      if (presetKey == "PresetExtreme" || presetKey == "PresetGpuPriority" || presetKey == "PresetLightUse") return;
      try {
        var preset = new ConfigManager.PresetConfig {
          FanTable = fanTable,
          FanControl = fanControl,
          TempSensitivity = tempSensitivity,
          CpuPower = cpuPower,
          TgpPower = tgpPower,
          PpabPower = ppabPower,
          DState = dState,
          GpuClock = gpuClock,
          TppPower = tppPower,
          IccMax = iccMax,
          AcLoadLine = acLoadline
        };
        ConfigManager.SavePreset(presetKey, preset);
      } catch { }
    }

    /// <summary>
    /// 从本地文件加载预设配置
    /// </summary>
    static void LoadPresetFromConfig(string presetKey) {
      try {
        var preset = ConfigManager.GetPreset(presetKey);
        if (preset != null) {
          fanTable = preset.FanTable;
          fanControl = preset.FanControl;
          tempSensitivity = preset.TempSensitivity;
          cpuPower = preset.CpuPower;
          tgpPower = preset.TgpPower;
          ppabPower = preset.PpabPower;
          dState = preset.DState;
          gpuClock = preset.GpuClock;
          tppPower = preset.TppPower;
          iccMax = preset.IccMax;
          acLoadline = preset.AcLoadLine;
        }
      } catch { }
    }

    /// <summary>
    /// 从本地文件恢复全部配置
    /// 替代原来的注册表读取
    /// </summary>
    static void RestoreConfig(bool isPreset = false) {
      try {
        currentPreset = ConfigManager.Get("CurrentPreset", platformSettings != null ? "PresetExtreme" : "PresetCustom1");
        presetCustom1Name = ConfigManager.Get("PresetCustom1Name", Strings.PresetCustom1);
        presetCustom2Name = ConfigManager.Get("PresetCustom2Name", Strings.PresetCustom2);
        presetCustom3Name = ConfigManager.Get("PresetCustom3Name", Strings.PresetCustom3);

        switch (currentPreset) {
          case "PresetExtreme":
            UpdateCheckedState("presetsGroup", Strings.PresetExtreme);
            break;
          case "PresetGpuPriority":
            UpdateCheckedState("presetsGroup", Strings.PresetGpuPriority);
            break;
          case "PresetLightUse":
            UpdateCheckedState("presetsGroup", Strings.PresetLightUse);
            break;
          case "PresetCustom1":
            UpdateCheckedState("presetsGroup", Strings.PresetCustom1);
            break;
          case "PresetCustom2":
            UpdateCheckedState("presetsGroup", Strings.PresetCustom2);
            break;
          case "PresetCustom3":
            UpdateCheckedState("presetsGroup", Strings.PresetCustom3);
            break;
        }

        if (currentPreset != "PresetExtreme" && currentPreset != "PresetGpuPriority" && currentPreset != "PresetLightUse") {
          LoadPresetFromConfig(currentPreset);
        }

        if (currentPreset == "PresetExtreme" || currentPreset == "PresetGpuPriority" || currentPreset == "PresetLightUse") {
          fanTable = ConfigManager.Get("FanTable", fanTable);
          fanControl = ConfigManager.Get("FanControl", "auto");
          tempSensitivity = ConfigManager.Get("TempSensitivity", "high");
          cpuPower = ConfigManager.Get("CpuPower", "null");
          tgpPower = ConfigManager.Get("TgpPower", "on");
          ppabPower = ConfigManager.Get("PpabPower", "on");
          dState = ConfigManager.Get("DState", "normal");
          gpuClock = ConfigManager.Get("GpuClock", 0);
          tppPower = ConfigManager.Get("TppPower", "null");
          iccMax = ConfigManager.Get("IccMax", "null");
          acLoadline = ConfigManager.Get("AcLoadLine", "null");
        }

        if (fanTable.Contains("cool")) {
          LoadFanConfig("cool.txt");
          UpdateCheckedState("fanTableGroup", Strings.FanCoolMode);
        } else if (fanTable.Contains("silent")) {
          LoadFanConfig("silent.txt");
          UpdateCheckedState("fanTableGroup", Strings.FanSilentMode);
        }

        if (fanControl == "auto") {
          SetMaxFanSpeedOff();
          fanControlTimer.Change(0, 1000);
          UpdateCheckedState("fanControlGroup", Strings.FanAuto);
        } else if (fanControl.Contains("max")) {
          SetMaxFanSpeedOn();
          fanControlTimer.Change(Timeout.Infinite, Timeout.Infinite);
          UpdateCheckedState("fanControlGroup", Strings.FanMax);
        } else if (fanControl.Contains(" RPM")) {
          SetMaxFanSpeedOff();
          fanControlTimer.Change(Timeout.Infinite, Timeout.Infinite);
          int rpmValue = int.Parse(fanControl.Replace(" RPM", "").Trim());
          SetFanLevel(rpmValue / 100, rpmValue / 100, Is3FanNb);
          if (fanTrackBar != null && rpmValue / 100 >= fanTrackBar.Minimum && rpmValue / 100 <= fanTrackBar.Maximum) {
            fanTrackBar.Value = rpmValue / 100;
          }
          UpdateCheckedState("fanControlGroup", Strings.SetFanSpeedSlider);
        }

        switch (tempSensitivity) {
          case "realtime":
            respondSpeed = 1;
            UpdateCheckedState("tempSensitivityGroup", Strings.FanRespRealtime);
            break;
          case "high":
            respondSpeed = 0.4f;
            UpdateCheckedState("tempSensitivityGroup", Strings.FanRespHigh);
            break;
          case "medium":
            respondSpeed = 0.1f;
            UpdateCheckedState("tempSensitivityGroup", Strings.FanRespMedium);
            break;
          case "low":
            respondSpeed = 0.04f;
            UpdateCheckedState("tempSensitivityGroup", Strings.FanRespLow);
            break;
        }

        // TPP 设置单独延迟 1s 应用，避免启动时与其他设置冲突
        string tppPowerSnapshot = tppPower;
        System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => {
          if (tppPowerSnapshot == "null") {
            UpdateCheckedState("tppPowerGroup", Strings.NotSet);
          } else if (tppPowerSnapshot == "max") {
            SetConcurrentTdp(254);
            if (tppTrackBar != null && tppTrackBar.Minimum <= 254 && 254 <= tppTrackBar.Maximum) {
              tppTrackBar.Value = 254;
            }
          } else if (tppPowerSnapshot.Contains(" W")) {
            int value = int.Parse(tppPowerSnapshot.Replace(" W", "").Trim());
            if (value >= 20 && value <= 254) {
              SetConcurrentTdp((byte)value);
              if (tppTrackBar != null && tppTrackBar.Minimum <= value && value <= tppTrackBar.Maximum) {
                tppTrackBar.Value = value;
              }
              UpdateCheckedState("tppPowerGroup", Strings.SetTppSlider);
            }
          }
        });

        if (iccMax == "null") {
          UpdateCheckedState("iccMaxGroup", Strings.NotSet);
        } else if (iccMax.Contains(" A")) {
          if (int.TryParse(iccMax.Replace(" A", "").Trim(), out int ampVal) && ampVal >= 150 && ampVal <= 350) {
            SetIccMaxByWmi((decimal)ampVal);
            UpdateCheckedState("iccMaxGroup", iccMax);
          }
        }

        if (acLoadline == "null") {
          UpdateCheckedState("acLoadLineGroup", Strings.NotSet);
        } else if (int.TryParse(acLoadline, out int llVal) && llVal >= 1) {
          SetLoadLine(llVal);
          string llDisplay = (180 - 10 * llVal).ToString();
          UpdateCheckedState("acLoadLineGroup", llDisplay);
        }

        if (cpuPower == "null") {
          UpdateCheckedState("cpuPowerGroup", Strings.NotSet);
        } else if (cpuPower == "max") {
          SetCpuPowerLimit(254);
          if (cpuPowerTrackBar != null && 254 >= cpuPowerTrackBar.Minimum && 254 <= cpuPowerTrackBar.Maximum) {
            cpuPowerTrackBar.Value = 254;
          }
          UpdateCheckedState("cpuPowerGroup", Strings.SetCpuPowerSlider);
        } else if (cpuPower.Contains(" W")) {
          int value = int.Parse(cpuPower.Replace(" W", "").Trim());
          if (value >= 5 && value <= 254) {
            SetCpuPowerLimit((byte)value);
            if (cpuPowerTrackBar != null && value >= cpuPowerTrackBar.Minimum && value <= cpuPowerTrackBar.Maximum) {
              cpuPowerTrackBar.Value = value;
            }
            UpdateCheckedState("cpuPowerGroup", Strings.SetCpuPowerSlider);
          }
        }

        SetGpuPowerState(tgpPower == "on", ppabPower == "on", dState == "normal" ? 1 : 2);
        UpdateCheckedState("tgpPowerGroup", tgpPower == "on" ? Strings.Enable : Strings.Disable);
        UpdateCheckedState("ppabPowerGroup", ppabPower == "on" ? Strings.Enable : Strings.Disable);
        UpdateCheckedState("dStateGroup", dState == "normal" ? Strings.Normal : Strings.LowPower);

        if (hasNVIDIAGpu) {
          if (SetGPUClockLimit(gpuClock)) {
            if (gpuClock > 0 && gpuClockTrackBar != null && gpuClockTrackBar.Minimum <= gpuClock / 10 && gpuClock / 10 <= gpuClockTrackBar.Maximum) {
              gpuClockTrackBar.Value = gpuClock / 10;
              UpdateCheckedState("gpuClockGroup", Strings.SetGpuClockSlider);
            } else if (gpuClock == 0) {
              UpdateCheckedState("gpuClockGroup", Strings.Restore);
            }
          } else {
            UpdateCheckedState("gpuClockGroup", Strings.Restore);
          }

          if (DBMenu.Enabled && !isPreset) {
            DBVersion = ConfigManager.Get("DBVersion", 2);
            switch (DBVersion) {
              case 1:
                if (IsAbove50Series()) {
                  DBVersion = 2;
                  string deviceId50 = "\"ACPI\\NVDA0820\\NPCF\"";
                  string command50 = $"pnputil /enable-device {deviceId50}";
                  ExecuteCommand(command50);
                  UpdateCheckedState("DBGroup", Strings.DbNormal);
                  break;
                }
                DBVersion = 1;
                SetGpuPowerState(true, true);
                SetCpuPowerLimit((byte)CPULimitDB);
                countDB = countDBInit;
                DBMenu.Enabled = false;
                UpdateCheckedState("DBGroup", Strings.DbUnlocked);
                break;
              case 2:
                string deviceId = "\"ACPI\\NVDA0820\\NPCF\"";
                string command = $"pnputil /enable-device {deviceId}";
                ExecuteCommand(command);
                DBVersion = 2;
                UpdateCheckedState("DBGroup", Strings.DbNormal);
                break;
            }
          }
        }

        autoStart = ConfigManager.Get("AutoStart", "off");
        switch (autoStart) {
          case "on":
            AutoStartEnable();
            UpdateCheckedState("autoStartGroup", Strings.Enable);
            break;
          case "off":
            UpdateCheckedState("autoStartGroup", Strings.Disable);
            break;
        }

        alreadyRead = ConfigManager.Get("AlreadyRead", 0);

        customIcon = ConfigManager.Get("CustomIcon", "original");
        switch (customIcon) {
          case "original":
            trayIcon.Icon = Properties.Resources.smallfan;
            UpdateCheckedState("customIconGroup", Strings.IconOriginal);
            break;
          case "custom":
            SetCustomIcon();
            UpdateCheckedState("customIconGroup", Strings.IconCustom);
            break;
          case "dynamic":
            UpdateDynamicIcon();
            UpdateCheckedState("customIconGroup", Strings.IconDynamic);
            break;
        }

        omenKey = ConfigManager.Get("OmenKey", "default");
        switch (omenKey) {
          case "default":
            checkFloatingTimer.Enabled = false;
            OmenKeyOff();
            OmenKeyOn(omenKey);
            UpdateCheckedState("omenKeyGroup", Strings.OmenKeyDefault);
            break;
          case "custom":
            checkFloatingTimer.Enabled = true;
            OmenKeyOff();
            OmenKeyOn(omenKey);
            UpdateCheckedState("omenKeyGroup", Strings.OmenKeyToggle);
            break;
          case "none":
            checkFloatingTimer.Enabled = false;
            OmenKeyOff();
            UpdateCheckedState("omenKeyGroup", Strings.OmenKeyNone);
            break;
        }

        bool monitorCPUCache = ConfigManager.Get("MonitorCPU", true);
        monitorCPU = monitorCPUCache;
        UpdateCheckedState("monitorCPUGroup", monitorCPU ? Strings.MonitorCpuOn : Strings.MonitorCpuOff);

        if (hasNVIDIAGpu || hasAMDDiscreteGpu) {
          bool monitorGPUCache = ConfigManager.Get("MonitorGPU", true);
          monitorGPU = monitorGPUCache;
          UpdateCheckedState("monitorGPUGroup", monitorGPU ? Strings.MonitorGpuOn : Strings.MonitorGpuOff);
        } else {
          monitorGPU = false;
          UpdateCheckedState("monitorGPUGroup", monitorGPU ? Strings.MonitorGpuOn : Strings.MonitorGpuOff);
        }

        if (monitorCPU || monitorGPU) {
          StartHardwareMonitor();
        }

        bool monitorFanCache = ConfigManager.Get("MonitorFan", false);
        if (monitorFanCache == true) {
          monitorFan = true;
          UpdateCheckedState("monitorFanGroup", Strings.MonitorFanOn);
        } else {
          monitorFan = false;
          UpdateCheckedState("monitorFanGroup", Strings.MonitorFanOff);
        }

        monitorRefreshRate = ConfigManager.Get("MonitorRefreshRate", "low");
        switch (monitorRefreshRate) {
          case "high":
            tooltipUpdateTimer.Interval = 250;
            SetMonitorInterval(250);
            UpdateCheckedState("monitorRefreshGroup", Strings.MonitorRefreshHigh);
            break;
          case "low":
          default:
            monitorRefreshRate = "low";
            tooltipUpdateTimer.Interval = 1000;
            SetMonitorInterval(1000);
            UpdateCheckedState("monitorRefreshGroup", Strings.MonitorRefreshLow);
            break;
        }

        tempDisplayMode = ConfigManager.Get("TempDisplayMode", "smoothed");
        if (tempDisplayMode == "raw") {
          UpdateCheckedState("tempDisplayGroup", Strings.TempRaw);
        } else {
          tempDisplayMode = "smoothed";
          UpdateCheckedState("tempDisplayGroup", Strings.TempSmoothed);
        }

        textSize = ConfigManager.Get("FloatingBarSize", 48);
        UpdateFloatingText();
        switch (textSize) {
          case 24:
            UpdateCheckedState("floatingBarSizeGroup", Strings.FontSize24);
            break;
          case 36:
            UpdateCheckedState("floatingBarSizeGroup", Strings.FontSize36);
            break;
          case 48:
            UpdateCheckedState("floatingBarSizeGroup", Strings.FontSize48);
            break;
        }

        floatingBarLoc = ConfigManager.Get("FloatingBarLoc", "left");
        UpdateFloatingText();
        if (floatingBarLoc == "left") {
          UpdateCheckedState("floatingBarLocGroup", Strings.FloatingLocLeft);
        } else {
          UpdateCheckedState("floatingBarLocGroup", Strings.FloatingLocRight);
        }

        floatingBar = ConfigManager.Get("FloatingBar", "off");
        if (floatingBar == "on") {
          ShowFloatingForm();
          UpdateCheckedState("floatingBarGroup", Strings.FloatingShow);
        } else {
          CloseFloatingForm();
          UpdateCheckedState("floatingBarGroup", Strings.FloatingHide);
        }

        dataLocalize = ConfigManager.Get("DataLocalize", "off");
        if (dataLocalize == "on") {
          UpdateCheckedState("dataLocalizeGroup", Strings.Enable);
        } else {
          UpdateCheckedState("dataLocalizeGroup", Strings.Disable);
        }

        appLanguage = ConfigManager.Get("AppLanguage", "zh-CN");
        RestoreLanguageChecked();

        // 兼容性检查：如果没有 CurrentPreset 键，则迁移为自定义预设1
        var currentPresetValue = ConfigManager.Get<string>("CurrentPreset", null);
        if (currentPresetValue == null) {
          currentPreset = "PresetCustom1";
          SavePresetToConfig(currentPreset);
          SaveConfig("CurrentPreset");
        }

        // 确保 DataLocalize 菜单项被初始化
        if (dataLocalize == "on") {
          UpdateCheckedState("dataLocalizeGroup", Strings.Enable);
        } else {
          UpdateCheckedState("dataLocalizeGroup", Strings.Disable);
        }
      } catch (Exception ex) {
        Logger.Error($"Error restoring configuration: {ex.Message}");
      }
    }

    /// <summary>
    /// 应用预设逻辑
    /// </summary>
    static void applyPresetLogic(string targetPreset) {
      currentPreset = targetPreset;
      SaveConfig("CurrentPreset");
      // 重新加载预设并保存值
      if (targetPreset == "PresetExtreme" || targetPreset == "PresetGpuPriority" || targetPreset == "PresetLightUse") {
        fanTable = "cool"; fanControl = "auto"; tempSensitivity = "high";
        tgpPower = "on"; ppabPower = "on"; dState = "normal";
        gpuClock = 0; iccMax = "null"; acLoadline = "null";
        int targetPL1Perf = (platformSettings?.NbPL1UpperBoundPerformance > 0) ? platformSettings.NbPL1UpperBoundPerformance : 160;
        int targetPL1Default = (platformSettings?.NbPL1UpperBoundDefault > 0) ? platformSettings.NbPL1UpperBoundDefault : 55;

        if (targetPreset == "PresetExtreme") {
          cpuPower = $"{targetPL1Perf} W";
          tppPower = $"{targetPL1Perf} W";
        } else if (targetPreset == "PresetGpuPriority") {
          cpuPower = $"{targetPL1Default} W";
          tppPower = $"{targetPL1Perf} W";
        } else if (targetPreset == "PresetLightUse") {
          fanTable = "silent";
          cpuPower = $"{(int)(targetPL1Default * 0.6)} W";
          if (currentPreset == "PresetLightUse" && platformSettings?.NbPL1UpperBoundDefault == null) cpuPower = "30 W";
          tppPower = "null";
          tgpPower = "off";
          ppabPower = "off";
        }
      } else {
        LoadPresetFromConfig(targetPreset);
      }
      SaveConfig();
      RestoreConfig(isPreset: true);
    }
  }
}
