using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CopyToLocalImage.Services
{
    /// <summary>
    /// 剪切板监听服务
    /// </summary>
    public class ClipboardMonitor : IDisposable
    {
        private nint _windowHandle;
        private bool _disposed;
        private readonly Action _onClipboardChanged;

        // Windows API 常量
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private const int HWND_MESSAGE = -3;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(nint hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(nint hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);

        private const int CF_BITMAP = 2;
        private const int CF_DIB = 8;
        private const int CF_DIBV5 = 17;
        private const int CF_ENHMETAFILE = 14;
        private const int CF_METAFILEPICT = 3;

        public ClipboardMonitor(Action onClipboardChanged)
        {
            _onClipboardChanged = onClipboardChanged;
        }

        /// <summary>
        /// 启动监听
        /// </summary>
        public void Start()
        {
            // 创建一个隐藏窗口用于接收剪切板消息
            var parameters = new HwndSourceParameters
            {
                Width = 1,
                Height = 1,
                WindowStyle = unchecked((int)0x80000000), // WS_OVERLAPPED
                HwndSourceHook = HwndSourceHook
            };

            var hwndSource = new HwndSource(parameters);
            _windowHandle = hwndSource.Handle;

            if (!AddClipboardFormatListener(_windowHandle))
            {
                throw new System.ComponentModel.Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "Failed to add clipboard format listener");
            }
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        public void Stop()
        {
            if (_windowHandle != nint.Zero)
            {
                RemoveClipboardFormatListener(_windowHandle);
                _windowHandle = nint.Zero;
            }
        }

        /// <summary>
        /// 窗口消息处理
        /// </summary>
        private nint HwndSourceHook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                try
                {
                    if (IsImageInClipboard())
                    {
                        _onClipboardChanged?.Invoke();
                    }
                }
                catch
                {
                    // 忽略剪切板访问异常
                }
                handled = true;
            }
            return nint.Zero;
        }

        /// <summary>
        /// 检查剪切板中是否有图片
        /// </summary>
        public static bool IsImageInClipboard()
        {
            try
            {
                return Clipboard.ContainsImage();
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }
    }
}
