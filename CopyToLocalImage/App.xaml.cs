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
            base.OnStartup(e);

            // 加载配置
            _settings = AppSettings.Load();

            // 确保保存目录存在
            if (!System.IO.Directory.Exists(_settings.SavePath))
            {
                System.IO.Directory.CreateDirectory(_settings.SavePath);
            }

            // 初始化服务
            _storageService = new StorageService(_settings.SavePath);
            _imageService = new ImageService(_settings.SavePath);

            // 创建主窗口
            _mainWindow = new MainWindow(_storageService, _imageService);

            // 注册主窗口关闭事件
            _mainWindow.Closing += MainWindow_Closing;

            // 初始化托盘图标
            _trayIconService = new TrayIconService(
                onOpen: ShowMainWindow,
                onExit: ExitApplication,
                getMinimizeToTray: () => _settings.MinimizeToTray);
            _trayIconService.Initialize();

            // 初始化剪切板监听
            _clipboardMonitor = new ClipboardMonitor(OnClipboardChanged);
            _clipboardMonitor.Start();

            // 初始化热键
            if (_settings.EnableHotkey)
            {
                _hotkeyService = new HotkeyService(OnHotkeyPressed);
                var combination = HotkeyService.Parse(_settings.Hotkey);
                if (combination != null)
                {
                    _hotkeyService.RegisterHotKey(combination);
                }
            }

            // 显示主窗口（除非配置启动最小化）
            if (!_settings.StartMinimized)
            {
                _mainWindow.Show();
            }

            // 异步加载图片数据
            _ = _storageService.LoadAsync();
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
            try
            {
                var filePath = _imageService.SaveImageFromClipboard();
                if (!string.IsNullOrEmpty(filePath))
                {
                    var item = _storageService.AddImageRecord(filePath);
                    _mainWindow.AddImage(item);

                    // 显示通知
                    _trayIconService.ShowBalloonTip(
                        "图片已保存",
                        $"图片已保存到：{System.IO.Path.GetDirectoryName(filePath)}",
                        System.Windows.Forms.ToolTipIcon.Info);
                }
            }
            catch
            {
                // 忽略保存错误
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
            _clipboardMonitor?.Dispose();
            _hotkeyService?.Dispose();
            _trayIconService?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ExitApplication();
            base.OnExit(e);
        }
    }
}
