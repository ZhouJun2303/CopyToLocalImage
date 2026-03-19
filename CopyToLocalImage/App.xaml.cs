using System;
using System.Windows;
using CopyToLocalImage.Models;
using CopyToLocalImage.Services;

namespace CopyToLocalImage
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private AppSettings _settings = null!;
        private StorageService _storageService = null!;
        private ImageService _imageService = null!;
        private ClipboardMonitor _clipboardMonitor = null!;
        private HotkeyService _hotkeyService = null!;
        private TrayIconService _trayIconService = null!;
        private MainWindow _mainWindow = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            LogService.Info("========== 应用启动 ==========");
            LogService.Info($"命令行参数：{string.Join(" ", e.Args)}");
            LogService.Info($".NET 版本：{Environment.Version}");
            LogService.Info($"操作系统：{Environment.OSVersion}");

            base.OnStartup(e);

            try
            {
                // 加载配置
                LogService.Info("正在加载配置...");
                _settings = AppSettings.Load();
                LogService.Info($"配置加载成功，保存路径：{_settings.SavePath}");

                // 确保保存目录存在
                if (!System.IO.Directory.Exists(_settings.SavePath))
                {
                    LogService.Info($"创建保存目录：{_settings.SavePath}");
                    System.IO.Directory.CreateDirectory(_settings.SavePath);
                }

                // 初始化服务
                LogService.Info("正在初始化存储服务...");
                _storageService = new StorageService(_settings.SavePath);
                LogService.Info("存储服务初始化完成");

                LogService.Info("正在初始化图片服务...");
                _imageService = new ImageService(_settings.SavePath);
                LogService.Info("图片服务初始化完成");

                // 创建主窗口
                LogService.Info("正在创建主窗口...");
                _mainWindow = new MainWindow(_storageService, _imageService);
                LogService.Info("主窗口创建完成");

                // 注册主窗口关闭事件
                _mainWindow.Closing += MainWindow_Closing;

                // 初始化托盘图标
                LogService.Info("正在初始化托盘图标...");
                _trayIconService = new TrayIconService(
                    onOpen: ShowMainWindow,
                    onExit: ExitApplication,
                    getMinimizeToTray: () => _settings.MinimizeToTray);
                _trayIconService.Initialize();
                LogService.Info("托盘图标初始化完成");

                // 初始化剪切板监听
                LogService.Info("正在启动剪切板监听...");
                _clipboardMonitor = new ClipboardMonitor(OnClipboardChanged);
                _clipboardMonitor.Start();
                LogService.Info("剪切板监听已启动");

                // 初始化热键
                if (_settings.EnableHotkey)
                {
                    LogService.Info($"正在注册热键：{_settings.Hotkey}");
                    _hotkeyService = new HotkeyService(OnHotkeyPressed);
                    var combination = HotkeyService.Parse(_settings.Hotkey);
                    if (combination != null)
                    {
                        var registered = _hotkeyService.RegisterHotKey(combination);
                        LogService.Info(registered ? "热键注册成功" : "热键注册失败");
                    }
                    else
                    {
                        LogService.Warning($"热键解析失败：{_settings.Hotkey}");
                    }
                }
                else
                {
                    LogService.Info("热键已禁用");
                }

                // 显示主窗口（除非配置启动最小化）
                if (!_settings.StartMinimized)
                {
                    LogService.Info("显示主窗口");
                    _mainWindow.Show();
                }
                else
                {
                    LogService.Info("启动最小化，隐藏主窗口");
                }

                // 异步加载图片数据
                LogService.Info("开始加载图片数据...");
                _ = _storageService.LoadAsync().ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        LogService.Info("图片数据加载完成");
                    }
                    else if (t.IsFaulted)
                    {
                        LogService.Error("图片数据加载失败", t.Exception);
                    }
                });

                LogService.Info("========== 应用启动完成 ==========");
            }
            catch (Exception ex)
            {
                LogService.Error("应用启动失败", ex);
                System.Windows.MessageBox.Show(
                    $"启动失败：{ex.Message}\n\n{ex.StackTrace}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }

        /// <summary>
        /// 主窗口关闭事件处理
        /// </summary>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_mainWindow.HandleClosing())
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 剪切板变化时的回调
        /// </summary>
        private void OnClipboardChanged()
        {
            LogService.Debug("检测到剪切板变化");
            try
            {
                var filePath = _imageService.SaveImageFromClipboard();
                if (!string.IsNullOrEmpty(filePath))
                {
                    LogService.Info($"图片保存成功：{filePath}");
                    var item = _storageService.AddImageRecord(filePath);
                    _mainWindow.AddImage(item);

                    // 显示通知
                    _trayIconService.ShowBalloonTip(
                        "图片已保存",
                        $"图片已保存到：{System.IO.Path.GetDirectoryName(filePath)}",
                        System.Windows.Forms.ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                LogService.Error("保存图片失败", ex);
            }
        }

        /// <summary>
        /// 热键按下时的回调
        /// </summary>
        private void OnHotkeyPressed()
        {
            Dispatcher.Invoke(() =>
            {
                ShowMainWindow();
            });
        }

        /// <summary>
        /// 显示主窗口
        /// </summary>
        private void ShowMainWindow()
        {
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
            _mainWindow.Activate();
            _mainWindow.Focus();
            _mainWindow.RefreshImages();
        }

        /// <summary>
        /// 退出应用
        /// </summary>
        private void ExitApplication()
        {
            LogService.Info("========== 应用退出 ==========");
            _clipboardMonitor?.Dispose();
            _hotkeyService?.Dispose();
            _trayIconService?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogService.Info("OnExit 被调用");
            ExitApplication();
            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object? sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogService.Error("未处理的异常", e.Exception);
            e.Handled = true;
        }
    }
}
