迁移指南：从注册表配置到本地配置文件
========================================

## 概述

本分支完全将应用配置从 Windows 注册表迁移到本地 JSON 配置文件。配置文件与主程序存放于同一目录：`config.json`

## 核心组件

### 1. ConfigManager.cs
- **功能**：配置的读写、持久化、注册表自动迁移
- **位置**：应用程序目录
- **配置文件**：`config.json`（与 OmenSuperHub.exe 同目录）
- **自动迁移**：首次运行时，如果检测到旧注册表配置，会自动迁移到 JSON 文件

#### 主要方法：
```csharp
ConfigManager.Initialize()           // 启动时调用
ConfigManager.Get<T>(key, default)  // 读取配置
ConfigManager.Set<T>(key, value)    // 保存配置
ConfigManager.GetPreset(presetKey)  // 读取预设
ConfigManager.SavePreset(presetKey, preset)  // 保存预设
ConfigManager.GetConfigPath()       // 获取配置文件路径
```

### 2. Program.Config.Migration.cs
- **功能**：在 Program.Config.cs 中替换的方法集
- 包含所有配置相关操作，使用 ConfigManager 代替注册表
- 直接可用于替换原 Program.Config.cs 中的对应方法

## 集成步骤

### 第一步：在 Main() 中初始化 ConfigManager
在 `Program.cs` 的 `Main` 方法中，LoadLanguageSetting() 调用之前添加：

```csharp
static void Main(string[] args) {
    // ... 其他初始化代码 ...
    
    // 初始化配置管理器（必须在使用配置之前）
    ConfigManager.Initialize();
    
    // 加载语言设置（使用 ConfigManager）
    LoadLanguageSetting();
    
    // ... 继续其他初始化 ...
}
```

### 第二步：替换 Program.Config.cs 中的方法

使用 `Program.Config.Migration.cs` 中的实现替换以下方法：

1. **LoadLanguageSetting()** - 从配置文件读取语言
2. **SaveConfig(configName)** - 保存配置到文件
3. **SavePresetToRegistry(presetKey)** → **SavePresetToConfig(presetKey)**
4. **LoadPresetFromRegistry(presetKey)** → **LoadPresetFromConfig(presetKey)**
5. **RestoreConfig(isPreset)** - 恢复全部配置

### 第三步：删除注册表相关代码

- 移除所有 `Microsoft.Win32.Registry` 相关的直接操作
- 注册表迁移由 ConfigManager.Initialize() 自动处理
- 不需要手动编写迁移代码

## 配置文件格式

### config.json 示例

```json
{
  "currentPreset": "PresetExtreme",
  "presetCustom1Name": "自定义预设1",
  "fanTable": "cool",
  "fanControl": "auto",
  "tempSensitivity": "high",
  "cpuPower": "100 W",
  "tgpPower": "on",
  "appLanguage": "zh-CN",
  "autoStart": "off",
  "monitorCPU": true,
  "monitorGPU": true,
  "floatingBar": "off",
  "presets": {
    "PresetCustom1": {
      "fanTable": "cool",
      "fanControl": "auto",
      "cpuPower": "null",
      "tgpPower": "on"
    }
  }
}
```

## 优势

✅ **跨平台性更好** - JSON 文件可在任何系统中管理  
✅ **易于备份和迁移** - 直接复制 config.json 即可  
✅ **透明且易读** - JSON 格式人类可读  
✅ **无需管理员权限** - 不依赖注册表写入权限  
✅ **便于调试** - 可直接编辑配置文件测试  
✅ **自动迁移** - 旧用户无缝升级，自动转换注册表配置  

## 自动迁移流程

1. **首次运行更新版本**：
   - 检查是否存在 `config.json`
   - 若不存在，检查注册表中是否有旧配置
   - 若存在旧配置，自动导入到 `config.json`
   - 若都不存在，创建默认配置

2. **日志输出**：
   ```
   Configuration loaded from: C:\...\config.json
   或
   Configuration migrated from registry to: C:\...\config.json
   或
   New configuration created at: C:\...\config.json
   ```

## 测试建议

### 测试场景 1：全新安装
- 删除 `config.json` 和注册表中的 `OmenSuperHub` 项
- 运行程序
- 验证生成了新的 `config.json` 和默认配置

### 测试场景 2：升级迁移
- 保留旧的注册表配置
- 更新程序
- 运行程序
- 验证 `config.json` 已自动从注册表生成
- 验证所有配置值已正确迁移

### 测试场景 3：手动编辑配置
- 编辑 `config.json`（例如改变 `appLanguage`）
- 重启程序
- 验证新配置已应用

### 测试场景 4：配置持久化
- 修改一个配置（如风扇表）
- 关闭程序
- 重启程序
- 验证配置已保存

## 常见问题

### Q: 如何备份配置？
A: 直接复制 `config.json` 文件即可。可保存到云存储或其他位置备用。

### Q: 如何重置为默认配置？
A: 调用 `ConfigManager.ResetToDefault()`，或删除 `config.json` 文件重启程序。

### Q: 配置文件损坏怎么办？
A: ConfigManager 会自动检测并修复，若无法修复则使用默认配置。

### Q: 是否可以禁用注册表迁移？
A: 可以，若配置文件已存在，自动跳过迁移过程。

## 分支信息

- **分支名称**：`feature/local-config-file`
- **基础分支**：`master`
- **提交**：
  1. ConfigManager.cs - 配置管理核心
  2. ConfigManager Update - 支持本地存储和自动迁移
  3. Program.Config.Migration.cs - 配置方法实现

## 下一步

1. 审查并测试所有功能
2. 集成到主分支前进行完整 QA
3. 考虑添加 UI 选项让用户管理配置
4. 后续可添加配置版本控制和冲突解决机制
