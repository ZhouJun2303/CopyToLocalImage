# CopyToLocalImage - 跨平台图片剪切板监听程序

一个轻量级的 Windows 桌面程序，用于监听系统剪切板并自动保存复制的图片。

## 功能特性

- **自动监听剪切板**：检测到图片时自动保存为 PNG 格式
- **按日期分类**：图片按日期自动分类存储（格式：`yyyy-MM-dd`）
- **网格/列表视图**：支持两种视图模式浏览图片
- **批量删除**：支持多选删除和按日期删除
- **全局热键**：默认 `Ctrl+Alt+V` 快速打开主窗口
- **系统托盘**：支持最小化到托盘，双击托盘图标打开
- **拖拽支持**：支持将图片拖拽到命令行窗口

## 系统要求

- Windows 10/11
- .NET 8.0 Runtime

## 编译和运行

### 前置条件

1. 安装 [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 编译

```bash
cd CopyToLocalImage
dotnet restore
dotnet build --configuration Release
```

编译后的文件位于 `bin/Release/net8.0-windows/` 目录

### 运行

```bash
dotnet run
```

或直接运行生成的 EXE 文件：

```bash
bin/Release/net8.0-windows/CopyToLocalImage.exe
```

## 使用说明

### 基本使用

1. 启动程序后，系统托盘会显示图标
2. 复制任意图片到剪切板（如截图、网页图片等）
3. 程序会自动保存图片并显示通知
4. 在主窗口可以预览和管理已保存的图片

### 视图切换

- **网格视图**：以缩略图形式展示，适合快速浏览
- **列表视图**：以列表形式展示，显示更多详细信息

### 筛选功能

- **全部**：显示所有图片
- **今天**：仅显示今天的图片
- **本周**：显示本周的图片
- **本月**：显示本月的图片

### 批量删除

1. **手动多选**：
   - 在网格/列表视图中选择图片
   - 点击"全选"按钮快速选择所有
   - 点击"删除选中"按钮删除

2. **按日期删除**：
   - 点击"按日期删除"按钮
   - 选择预设选项（7 天前/30 天前/90 天前）
   - 或选择"选择日期删除..."自定义日期

### 设置

点击"设置"按钮可以配置：

- **图片保存路径**：更改图片存储位置
- **关闭行为**：选择关闭窗口时最小化到托盘或直接退出
- **启动行为**：启动时自动最小化到托盘
- **全局热键**：自定义热键组合
- **自动清理**：设置自动清理 N 天前的图片

### 全局热键

默认热键：`Ctrl+Alt+V`

按下热键时：
- 如果窗口已最小化，恢复窗口
- 如果窗口在后台，激活窗口
- 刷新图片列表

## 文件结构

```
CopyToLocalImage/
├── CopyToLocalImage.sln          # 解决方案文件
├── CopyToLocalImage/
│   ├── App.xaml / .cs            # 应用入口
│   ├── MainWindow.xaml / .cs     # 主窗口
│   ├── SettingsWindow.xaml / .cs # 设置窗口
│   ├── Services/
│   │   ├── ClipboardMonitor.cs   # 剪切板监听
│   │   ├── ImageService.cs       # 图片处理
│   │   ├── StorageService.cs     # 存储管理
│   │   ├── HotkeyService.cs      # 热键服务
│   │   └── TrayIconService.cs    # 托盘图标
│   ├── Models/
│   │   ├── ImageItem.cs          # 图片模型
│   │   └── AppSettings.cs        # 配置模型
│   ├── Converters/
│   │   └── DateToColorConverter.cs
│   ├── Resources/
│   │   └── Styles.xaml           # 样式资源
│   └── CopyToLocalImage.csproj   # 项目文件
```

## 数据存储

- **图片文件**：默认保存在 `图片/ClipboardImages` 文件夹
- **缩略图**：保存在 `_thumbnails` 子文件夹
- **元数据**：保存在 `metadata.json` 文件
- **配置文件**：保存在 `%LOCALAPPDATA%/CopyToLocalImage/settings.json`

## 常见问题

### Q: 为什么有时保存图片失败？
A: 某些特殊格式的图片可能无法直接从剪切板获取，建议先粘贴到画图软件再复制。

### Q: 如何彻底退出程序？
A: 右键点击托盘图标，选择"退出"菜单项。

### Q: 如何更改热键？
A: 在设置窗口中修改热键文本框，格式如：`Ctrl+Shift+V`

## 技术栈

- **框架**：.NET 8.0 + WPF
- **UI**：Fluent Design 风格
- **存储**：JSON 文件存储元数据
- **图片处理**：System.Windows.Media.Imaging

## 许可证

MIT License
