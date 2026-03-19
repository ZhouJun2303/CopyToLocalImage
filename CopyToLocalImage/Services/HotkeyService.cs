using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace CopyToLocalImage.Services
{
    /// <summary>
    /// 全局热键服务
    /// </summary>
    public class HotkeyService : IDisposable
    {
        private nint _windowHandle;
        private int _hotkeyId;
        private bool _disposed;
        private readonly Action _onHotkeyPressed;

        // Windows API
        private const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(nint hWnd, int id);

        // 修饰键常量
        private const uint MOD_NONE = 0;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        // 虚拟键码
        private static readonly System.Collections.Generic.Dictionary<Key, uint> KeyToVk = new()
        {
            { Key.A, 0x41 }, { Key.B, 0x42 }, { Key.C, 0x43 }, { Key.D, 0x44 },
            { Key.E, 0x45 }, { Key.F, 0x46 }, { Key.G, 0x47 }, { Key.H, 0x48 },
            { Key.I, 0x49 }, { Key.J, 0x4A }, { Key.K, 0x4B }, { Key.L, 0x4C },
            { Key.M, 0x4D }, { Key.N, 0x4E }, { Key.O, 0x4F }, { Key.P, 0x50 },
            { Key.Q, 0x51 }, { Key.R, 0x52 }, { Key.S, 0x53 }, { Key.T, 0x54 },
            { Key.U, 0x55 }, { Key.V, 0x56 }, { Key.W, 0x57 }, { Key.X, 0x58 },
            { Key.Y, 0x59 }, { Key.Z, 0x5A },
            { Key.D0, 0x30 }, { Key.D1, 0x31 }, { Key.D2, 0x32 }, { Key.D3, 0x33 },
            { Key.D4, 0x34 }, { Key.D5, 0x35 }, { Key.D6, 0x36 }, { Key.D7, 0x37 },
            { Key.D8, 0x38 }, { Key.D9, 0x39 },
            { Key.F1, 0x70 }, { Key.F2, 0x71 }, { Key.F3, 0x72 }, { Key.F4, 0x73 },
            { Key.F5, 0x74 }, { Key.F6, 0x75 }, { Key.F7, 0x76 }, { Key.F8, 0x77 },
            { Key.F9, 0x78 }, { Key.F10, 0x79 }, { Key.F11, 0x7A }, { Key.F12, 0x7B }
        };

        public HotkeyService(Action onHotkeyPressed)
        {
            _onHotkeyPressed = onHotkeyPressed;
        }

        /// <summary>
        /// 注册热键
        /// </summary>
        public bool RegisterHotKey(KeyCombination combination)
        {
            if (_windowHandle != nint.Zero)
            {
                UnregisterHotKey(_windowHandle, _hotkeyId);
            }

            var parameters = new HwndSourceParameters
            {
                Width = 1,
                Height = 1,
                WindowStyle = unchecked((int)0x80000000),
                HwndSourceHook = HwndSourceHook
            };

            var hwndSource = new HwndSource(parameters);
            _windowHandle = hwndSource.Handle;
            _hotkeyId = 1;

            uint modifiers = 0;
            if (combination.Control) modifiers |= MOD_CONTROL;
            if (combination.Alt) modifiers |= MOD_ALT;
            if (combination.Shift) modifiers |= MOD_SHIFT;
            if (combination.Win) modifiers |= MOD_WIN;

            if (!KeyToVk.TryGetValue(combination.Key, out var vk))
                return false;

            return RegisterHotKey(_windowHandle, _hotkeyId, modifiers, vk);
        }

        private nint HwndSourceHook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
            {
                _onHotkeyPressed?.Invoke();
                handled = true;
            }
            return nint.Zero;
        }

        /// <summary>
        /// 注销热键
        /// </summary>
        public void Unregister()
        {
            if (_windowHandle != nint.Zero)
            {
                UnregisterHotKey(_windowHandle, _hotkeyId);
                _windowHandle = nint.Zero;
            }
        }

        /// <summary>
        /// 解析热键字符串
        /// </summary>
        public static KeyCombination? Parse(string hotkeyString)
        {
            try
            {
                var parts = hotkeyString.Split('+', StringSplitOptions.RemoveEmptyEntries);
                bool control = false, alt = false, shift = false, win = false;
                Key? key = null;

                foreach (var part in parts)
                {
                    var trimmed = part.Trim().ToUpper();
                    if (trimmed == "CTRL" || trimmed == "CONTROL") control = true;
                    else if (trimmed == "ALT") alt = true;
                    else if (trimmed == "SHIFT") shift = true;
                    else if (trimmed == "WIN") win = true;
                    else if (Enum.TryParse<Key>(trimmed, out var k)) key = k;
                }

                if (key.HasValue)
                    return new KeyCombination { Control = control, Alt = alt, Shift = shift, Win = win, Key = key.Value };
            }
            catch
            {
                // 解析失败
            }
            return null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Unregister();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 热键组合
    /// </summary>
    public class KeyCombination
    {
        public bool Control { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }
        public Key Key { get; set; }

        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();
            if (Control) parts.Add("Ctrl");
            if (Alt) parts.Add("Alt");
            if (Shift) parts.Add("Shift");
            if (Win) parts.Add("Win");
            parts.Add(Key.ToString());
            return string.Join("+", parts);
        }
    }
}
