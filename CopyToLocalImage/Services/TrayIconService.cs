using System;
using System.Windows;
using System.Windows.Forms;
using CopyToLocalImage.Models;

namespace CopyToLocalImage.Services
{
    /// <summary>
    /// 系统托盘图标服务
    /// </summary>
    public class TrayIconService : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private bool _disposed;
        private readonly Action _onOpen;
        private readonly Action _onExit;
        private readonly Func<bool> _getMinimizeToTray;

        public TrayIconService(Action onOpen, Action onExit, Func<bool> getMinimizeToTray)
        {
            _onOpen = onOpen;
            _onExit = onExit;
            _getMinimizeToTray = getMinimizeToTray;
        }

        /// <summary>
        /// 初始化托盘图标
        /// </summary>
        public void Initialize()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                    System.Windows.Forms.Application.ExecutablePath),
                Text = "CopyToLocalImage - 点击打开",
                Visible = true
            };

            // 创建上下文菜单
            var contextMenu = new ContextMenuStrip();
            contextMenu.BackColor = System.Drawing.Color.White;
            contextMenu.ForeColor = System.Drawing.Color.Black;

            var openItem = contextMenu.Items.Add("打开主窗口");
            openItem.Click += (s, e) => _onOpen?.Invoke();

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = contextMenu.Items.Add("退出");
            exitItem.Click += (s, e) => _onExit?.Invoke();

            _notifyIcon.ContextMenuStrip = contextMenu;

            // 双击打开
            _notifyIcon.DoubleClick += (s, e) => _onOpen?.Invoke();
            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    _onOpen?.Invoke();
            };
        }

        /// <summary>
        /// 显示气球提示
        /// </summary>
        public void ShowBalloonTip(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon?.ShowBalloonTip(2000, title, message, icon);
        }

        /// <summary>
        /// 关闭窗口时处理
        /// </summary>
        public bool HandleClosing()
        {
            if (_getMinimizeToTray?.Invoke() == true)
            {
                // 最小化到托盘
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _notifyIcon?.Dispose();
                _disposed = true;
            }
        }
    }
}
