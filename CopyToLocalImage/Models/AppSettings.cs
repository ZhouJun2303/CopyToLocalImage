using System;
using System.IO;

namespace CopyToLocalImage.Models
{
    /// <summary>
    /// 应用配置模型
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 图片保存路径
        /// </summary>
        public string SavePath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "ClipboardImages");

        /// <summary>
        /// 关闭窗口时最小化到托盘（而非退出）
        /// </summary>
        public bool MinimizeToTray { get; set; } = true;

        /// <summary>
        /// 启动时自动最小化到托盘
        /// </summary>
        public bool StartMinimized { get; set; } = false;

        /// <summary>
        /// 全局热键（默认 Ctrl+Alt+V）
        /// </summary>
        public string Hotkey { get; set; } = "Ctrl+Alt+V";

        /// <summary>
        /// 是否启用全局热键
        /// </summary>
        public bool EnableHotkey { get; set; } = true;

        /// <summary>
        /// 自动清理天数（0=不清理）
        /// </summary>
        public int AutoCleanDays { get; set; } = 0;

        /// <summary>
        /// 使用深色主题
        /// </summary>
        public bool UseDarkTheme { get; set; } = false;

        /// <summary>
        /// 网格视图列数
        /// </summary>
        public int GridColumns { get; set; } = 4;

        /// <summary>
        /// 配置文件路径
        /// </summary>
        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CopyToLocalImage",
            "settings.json");

        /// <summary>
        /// 加载配置
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                // 加载失败时返回默认配置
            }
            return new AppSettings();
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // 保存失败时静默处理
            }
        }
    }
}
