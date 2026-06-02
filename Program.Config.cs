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
    /// 获取当前系统 UI 语言对应的语言代码（zh-CN / zh-TW / en）
    /// 若无法匹配，返回 "en"
    /// </summary>
    private static string GetSystemLanguage() {
      string cultureName = CultureInfo.CurrentUICulture.Name;

      // 简体中文：zh-CN, zh-Hans, zh-SG 等
      if (cultureName.StartsWith("zh-Hans", StringComparison.OrdinalIgnoreCase) ||
          cultureName.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ||
          cultureName.Equals("zh-SG", StringComparison.OrdinalIgnoreCase)) {
        return "zh-CN";
      }
      // 繁体中文：zh-TW, zh-HK, zh-MO, zh-Hant 等
      if (cultureName.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase) ||
          cultureName.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) ||
          cultureName.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
          cultureName.Equals("zh-MO", StringComparison.OrdinalIgnoreCase)) {
        return "zh-TW";
      }
      // 英语：en, en-US, en-GB 等
      if (cultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase)) {
        return "en";
      }
      // 其他语言默认英语
      return "en";
    }

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

    static void ApplyLanguage(string lang) {
      switch (lang) {
        case "zh-TW": Strings.Current = AppLanguage.TraditionalChinese; break;
        case "en": Strings.Current = AppLanguage.English; break;
        default: Strings.Current = AppLanguage.SimplifiedChinese; break;
      }
    }

    // 任务计划程序
    static void AutoStartEnable() {
      string currentPath = AppDomain.CurrentDomain.BaseDirectory;
      string exePath = Path.Combine(currentPath, "OmenSuperHub.exe");

      using (TaskService ts = new TaskService()) {

        // ── 任务一：系统启动时以 SYSTEM 账户启动 ──────────────────────────
        TaskDefinition tdBoot = ts.NewTask();
        tdBoot.RegistrationInfo.Description = "Start OmenSuperHub at system boot";
        tdBoot.Principal.RunLevel = TaskRunLevel.Highest;
        tdBoot.Principal.UserId = "SYSTEM";
        tdBoot.Principal.LogonType = TaskLogonType.ServiceAccount;

        tdBoot.Actions.Add(new ExecAction(exePath, null, null));

        BootTrigger bootTrigger = new BootTrigger();
        // bootTrigger.Delay = TimeSpan.FromSeconds(10); // 可选：延迟启动
        tdBoot.Triggers.Add(bootTrigger);

        tdBoot.Settings.DisallowStartIfOnBatteries = false;
        tdBoot.Settings.StopIfGoingOnBatteries = false;
        tdBoot.Settings.ExecutionTimeLimit = TimeSpan.Zero;
        tdBoot.Settings.AllowHardTerminate = false;

        ts.RootFolder.RegisterTaskDefinition(@"OmenSuperHub", tdBoot);
        //Console.WriteLine("任务一已创建：系统启动时运行。");

        // ── 任务二：用户登录时重启────────────────────────
        TaskDefinition tdLogon = ts.NewTask();
        tdLogon.RegistrationInfo.Description = "Restart OmenSuperHub at user logon";
        tdLogon.Principal.RunLevel = TaskRunLevel.Highest;

        tdLogon.Actions.Add(new ExecAction(
          exePath,
          "--relaunch",  // 传入参数，触发静默重启逻辑
          null
        ));

        LogonTrigger logonTrigger = new LogonTrigger();
        tdLogon.Triggers.Add(logonTrigger);

        tdLogon.Settings.Hidden = true; // 任务本身也隐藏
        tdLogon.Settings.DisallowStartIfOnBatteries = false;
        tdLogon.Settings.StopIfGoingOnBatteries = false;
        tdLogon.Settings.ExecutionTimeLimit = TimeSpan.Zero;
        tdLogon.Settings.AllowHardTerminate = false;

        ts.RootFolder.RegisterTaskDefinition(@"OmenSuperHub_Logon", tdLogon);
        //Console.WriteLine("任务二已创建：用户登录时重启。");
      }

      CleanUpAndRemoveTasks();
    }

    static void AutoStartDisable() {
      using (TaskService ts = new TaskService()) {
        string[] taskNames = { "OmenSuperHub", "OmenSuperHub_Logon" };

        foreach (string taskName in taskNames) {
          Task existingTask = ts.FindTask(taskName);
          if (existingTask != null) {
            ts.RootFolder.DeleteTask(taskName);
            //Console.WriteLine($"任务 {taskName} 已删除。");
          } else {
            //Console.WriteLine($"任务 {taskName} 不存在，无需删除。");
          }
        }
      }
    }

    // 清理旧版自启
    public static void CleanUpAndRemoveTasks() {
      // 目标文件夹和文件定义
      string targetFolder = @"C:\Program Files\OmenSuperHub";
      string taskName = "Omen Boot";
      string file1 = @"C:\Windows\SysWOW64\silent.txt";
      string file2 = @"C:\Windows\SysWOW64\cool.txt";

      // 删除目标文件夹及其内容
      if (Directory.Exists(targetFolder)) {
        string command = $"rd /s /q \"{targetFolder}\"";
        var result = ExecuteCommand(command);
        //Console.WriteLine(result.Output);
      } else {
        //Console.WriteLine("旧文件夹不存在");
      }

      // 删除 file1
      if (File.Exists(file1)) {
        string command = $"del /f /q \"{file1}\"";
        var result = ExecuteCommand(command);
        //Console.WriteLine($"文件已删除: {file1}");
        //Console.WriteLine(result.Output);
      } else {
        //Console.WriteLine($"文件不存在: {file1}");
      }

      // 删除 file2
      if (File.Exists(file2)) {
        string command = $"del /f /q \"{file2}\"";
        var result = ExecuteCommand(command);
        //Console.WriteLine($"文件已删除: {file2}");
        //Console.WriteLine(result.Output);
      } else {
        //Console.WriteLine($"文件不存在: {file2}");
      }

      // 检查并删除计划任务
      string taskQueryCommand = $"schtasks /query /tn \"{taskName}\"";
      var taskQueryResult = ExecuteCommand(taskQueryCommand);
      if (taskQueryResult.ExitCode == 0) {
        string deleteTaskCommand = $"schtasks /delete /tn \"{taskName}\" /f";
        var deleteTaskResult = ExecuteCommand(deleteTaskCommand);
        //Console.WriteLine("已成功删除计划任务 \"Omen Boot\"。");
        //Console.WriteLine(deleteTaskResult.Output);
      } else {
        //Console.WriteLine($"计划任务 \"{taskName}\" 不存在。");
      }

      // 从注册表中删除开机自启项（仅用于清理旧配置）
      string regDeleteCommand = @"reg delete ""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"" /v ""OmenSuperHub"" /f";
      var regDeleteResult = ExecuteCommand(regDeleteCommand);
      //Console.WriteLine("成功取消开机自启");
      //Console.WriteLine(regDeleteResult.Output);
    }

    static void RestoreCPUPower() {
      // 恢复CPU功耗设定
      if (cpuPower.Contains(" W")) {
        int value = int.Parse(cpuPower.Replace(" W", "").Trim());
        if (value >= 10 && value <= 254) {
          SetCpuPowerLimit((byte)value);
        }
      }
    }

    static void RestorePowerConfig() {
      SetUnleashMode();
      System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => {
        RestoreCPUPower();
        SetGpuPowerState(tgpPower == "on", ppabPower == "on", dState == "normal" ? 1 : 2);
        if (tppPower.Contains(" W")) {
          int value = int.Parse(tppPower.Replace(" W", "").Trim());
          if (value >= 20 && value <= 254) {
            SetConcurrentTdp((byte)value);
          }
        }
      });
    }

    static void RestoreFanControl() {
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
    }

    static void InitMaxTemp() {
      maxCPUTemp = null;
      if (platformSettings != null) {
        int throttle = platformSettings.temperatureThrottlingPerformance;
        if (throttle > 0) {
          maxCPUTemp = throttle;
        }
        if (hasNVIDIAGpu) {
          throttle = GetGpuTemperatureTarget();
          if (throttle > 50) {
            maxGPUTemp = throttle;
          }
        }
      }
    }

    // 从 platformSettings 提取平台最大转速，独立于风扇配置加载，启动时调用一次
    static void InitPlatformMaxFanSpeed() {
      if (platformSettings == null) return;
      int? maxFanSpeed = null;
      var candidates = new[] {
        platformSettings.SwFanControlCustomDefault,
        platformSettings.SwFanControlCustomPerformance,
        platformSettings.SwFanControlCustomUnleashed
      };
      foreach (var fanCustom in candidates) {
        if (fanCustom?.FanTable == null) continue;
        var cpuSpeeds = fanCustom.FanTable.Fan_Table_CPU_Fan_Speed_List;
        var gpuSpeeds = fanCustom.FanTable.Fan_Table_GPU_Fan_Speed_List;
        if (cpuSpeeds != null)
          foreach (var v in cpuSpeeds)
            if (!maxFanSpeed.HasValue || v > maxFanSpeed.Value) maxFanSpeed = v;
        if (gpuSpeeds != null)
          foreach (var v in gpuSpeeds)
            if (!maxFanSpeed.HasValue || v > maxFanSpeed.Value) maxFanSpeed = v;
      }
      if (maxFanSpeed.HasValue)
        platformMaxFanSpeed = maxFanSpeed.Value * 100;
    }

    static void LoadDefaultFanConfig(string filePath) {
      // ── 1. 获取 CPU 与 GPU 的允许最高温度差 ─────────────────────
      int? tempDelta = null;
      int maxGPUT = 87;
      if (maxGPUTemp.HasValue) {
        maxGPUT = maxGPUTemp.Value;
      }
      if (maxCPUTemp.HasValue) {
        tempDelta = maxCPUTemp.Value - maxGPUT;
      }

      // ── 2. 若两个值均获取成功，则生成 silent / cool 转速表 ──────────────
      if (platformMaxFanSpeed.HasValue && maxCPUTemp.HasValue && tempDelta.HasValue) {
        int maxRpm = platformMaxFanSpeed.Value;
        int maxCpu = maxCPUTemp.Value;
        int delta = tempDelta.Value;

        List<int> cpuTempList, cpuSpeedList, gpuTempList, gpuSpeedList;

        bool isSilent = filePath.IndexOf("silent", StringComparison.OrdinalIgnoreCase) >= 0;

        if (isSilent) {
          // silent: cpu30/gpu20 → 0RPM, 60℃ → maxRpm/3, 87℃ → maxRpm*2/3, maxTemp → maxRpm
          cpuTempList = new List<int> { 30, 60, 87, maxCpu };
          cpuSpeedList = new List<int> { 0, maxRpm / 3, maxRpm * 2 / 3, maxRpm - maxRpm / 10 };
          gpuTempList = new List<int> { 30 - delta, 60 - delta, 87 - delta, maxGPUT };
          gpuSpeedList = new List<int> { 0, maxRpm / 3, maxRpm * 2 / 3, maxRpm - maxRpm / 10 };
        } else {
          // cool: cpu45/gpu35 → maxRpm/4, (maxTemp-5)℃ → maxRpm
          cpuTempList = new List<int> { 45, maxCpu - 5, maxCpu };
          cpuSpeedList = new List<int> { maxRpm / 4, maxRpm, maxRpm + maxRpm / 10 };
          gpuTempList = new List<int> { 45 - delta, maxGPUT - 5, maxGPUT };
          gpuSpeedList = new List<int> { maxRpm / 4, maxRpm, maxRpm + maxRpm / 10 };
        }

        // 写入文件
        var lines = new List<string> {
          "Fan_Table_CPU_Temperature_List=" + string.Join(",", cpuTempList),
          "Fan_Table_CPU_Fan_Speed_List="   + string.Join(",", cpuSpeedList),
          "Fan_Table_GPU_Temperature_List=" + string.Join(",", gpuTempList),
          "Fan_Table_GPU_Fan_Speed_List="   + string.Join(",", gpuSpeedList)
        };
        File.WriteAllLines(filePath, lines);

        LoadFanConfigFromLists(cpuTempList, cpuSpeedList, gpuTempList, gpuSpeedList);
        return;
      }

      // ── 3. 兜底：无法提取参数时使用硬编码默认值 ─────────────────────────
      GenerateDefaultMapping(filePath);
    }

    static void LoadFanConfig(string filePath) {
      string absoluteFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
      if (!File.Exists(absoluteFilePath)) {
        //Logger.Info($"{absoluteFilePath} not found.");
        LoadDefaultFanConfig(absoluteFilePath);
        return;
      }

      string[] allLines = File.ReadAllLines(absoluteFilePath);
      if (allLines.Length == 0) {
        LoadDefaultFanConfig(absoluteFilePath);
        return;
      }

      // 判断文件格式：若第一行包含'='则视为新格式，否则为旧CSV格式
      bool isNewFormat = allLines[0].Contains('=');

      if (isNewFormat) {
        var cpuTempList = new List<int>();
        var cpuSpeedList = new List<int>();
        var gpuTempList = new List<int>();
        var gpuSpeedList = new List<int>();

        foreach (string line in allLines) {
          if (string.IsNullOrWhiteSpace(line)) continue;
          int eqIdx = line.IndexOf('=');
          if (eqIdx < 0) continue;
          string key = line.Substring(0, eqIdx).Trim();
          string valueStr = line.Substring(eqIdx + 1).Trim();
          var values = valueStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => int.Parse(s.Trim()))
                               .ToList();

          switch (key) {
            case "Fan_Table_CPU_Temperature_List":
              cpuTempList = values;
              break;
            case "Fan_Table_CPU_Fan_Speed_List":
              cpuSpeedList = values;
              break;
            case "Fan_Table_GPU_Temperature_List":
              gpuTempList = values;
              break;
            case "Fan_Table_GPU_Fan_Speed_List":
              gpuSpeedList = values;
              break;
          }
        }

        // 校验数据完整性
        if (cpuTempList.Count == 0 || cpuSpeedList.Count == 0 ||
            gpuTempList.Count == 0 || gpuSpeedList.Count == 0 ||
            cpuTempList.Count != cpuSpeedList.Count ||
            gpuTempList.Count != gpuSpeedList.Count) {
          Logger.Error($"{absoluteFilePath} invalid new format, regenerating.");
          LoadDefaultFanConfig(absoluteFilePath);
          return;
        }

        LoadFanConfigFromLists(cpuTempList, cpuSpeedList, gpuTempList, gpuSpeedList);
      } else {
        // 旧格式：CPU,Fan1,Fan2,GPU,Fan1,Fan2 多行
        var cpuTempList = new List<int>();
        var cpuSpeedList = new List<int>();
        var gpuTempList = new List<int>();
        var gpuSpeedList = new List<int>();

        try {
          for (int i = 1; i < allLines.Length; i++) // 跳过标题行
          {
            var parts = allLines[i].Split(',');
            if (parts.Length < 6) continue;
            int cpuTemp = int.Parse(parts[0].Trim());
            int cpuFan1 = int.Parse(parts[1].Trim()); // 我们取Fan1作为统一速度
            int gpuTemp = int.Parse(parts[3].Trim());
            int gpuFan1 = int.Parse(parts[4].Trim());

            cpuTempList.Add(cpuTemp);
            cpuSpeedList.Add(cpuFan1);
            gpuTempList.Add(gpuTemp);
            gpuSpeedList.Add(gpuFan1);
          }
        } catch {
          Logger.Error($"{absoluteFilePath} parse error, regenerating.");
          LoadDefaultFanConfig(absoluteFilePath);
          return;
        }

        if (cpuTempList.Count == 0 || gpuTempList.Count == 0) {
          LoadDefaultFanConfig(absoluteFilePath);
          return;
        }

        // 将旧格式转换为新格式并覆盖写入
        var newLines = new List<string>
        {
            "Fan_Table_CPU_Temperature_List=" + string.Join(",", cpuTempList),
            "Fan_Table_CPU_Fan_Speed_List=" + string.Join(",", cpuSpeedList),
            "Fan_Table_GPU_Temperature_List=" + string.Join(",", gpuTempList),
            "Fan_Table_GPU_Fan_Speed_List=" + string.Join(",", gpuSpeedList)
        };
        File.WriteAllLines(absoluteFilePath, newLines);

        LoadFanConfigFromLists(cpuTempList, cpuSpeedList, gpuTempList, gpuSpeedList);
      }
    }

    // Generate default temperature-fan speed mapping
    static void GenerateDefaultMapping(string filePath) {
      // 硬编码默认映射（与原逻辑一致，转换为新格式）
      var cpuTempList = new List<int> { 50, 60, 85, 100 };
      var cpuSpeedList = new List<int> { 1600, 2000, 4000, 5600 };   // RPM
      var gpuTempList = new List<int> { 40, 50, 75, 90 };
      var gpuSpeedList = new List<int> { 1600, 2000, 4000, 5600 };

      var lines = new List<string>
      {
        "Fan_Table_CPU_Temperature_List=" + string.Join(",", cpuTempList),
        "Fan_Table_CPU_Fan_Speed_List=" + string.Join(",", cpuSpeedList),
        "Fan_Table_GPU_Temperature_List=" + string.Join(",", gpuTempList),
        "Fan_Table_GPU_Fan_Speed_List=" + string.Join(",", gpuSpeedList)
      };
      File.WriteAllLines(filePath, lines);

      LoadFanConfigFromLists(cpuTempList, cpuSpeedList, gpuTempList, gpuSpeedList);
    }

    static void LoadFanConfigFromLists(List<int> cpuTempList, List<int> cpuSpeedList,
                                   List<int> gpuTempList, List<int> gpuSpeedList) {
      lock (CPUTempFanMap) {
        CPUTempFanMap.Clear();
        GPUTempFanMap.Clear();

        for (int i = 0; i < cpuTempList.Count; i++) {
          int speedRpm = cpuSpeedList[i];
          CPUTempFanMap[cpuTempList[i]] = new List<int> { speedRpm, speedRpm }; // 双风扇同速
        }

        for (int i = 0; i < gpuTempList.Count; i++) {
          int speedRpm = gpuSpeedList[i];
          GPUTempFanMap[gpuTempList[i]] = new List<int> { speedRpm, speedRpm };
        }
      }
    }

    // Get fan speed for CPU and GPU and return the maximum
    // 使用平滑后的温度查表，保证高中低档响应速度生效；实时档下平滑温度==原始温度
    // 只有对应监控开启且温度已完成初始化时，才参与风扇转速计算
    static int GetFanSpeedForTemperature(int fanIndex) {
      if (CPUTempFanMap.Count == 0 || GPUTempFanMap.Count == 0) return 0;

      int resultSpeed = 0;

      if (monitorCPU && cpuTempReady) {
        int cpuFanSpeed = GetFanSpeedForSpecificTemperature(smoothedCPUTemp, CPUTempFanMap, fanIndex);
        resultSpeed = Math.Max(resultSpeed, cpuFanSpeed);
      }

      if (monitorGPU && gpuTempReady) {
        int gpuFanSpeed = GetFanSpeedForSpecificTemperature(smoothedGPUTemp, GPUTempFanMap, fanIndex);
        resultSpeed = Math.Max(resultSpeed, gpuFanSpeed);
      }

      return resultSpeed;
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
