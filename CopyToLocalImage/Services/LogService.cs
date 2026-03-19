using System;
using System.IO;
using System.Text;

namespace CopyToLocalImage.Services
{
    /// <summary>
    /// 日志服务
    /// </summary>
    public static class LogService
    {
        private static readonly string LogFilePath;
        private static readonly object LockObj = new();

        static LogService()
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CopyToLocalImage",
                "logs");

            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            LogFilePath = Path.Combine(logDir, $"app_{DateTime.Now:yyyyMMdd}.log");
        }

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Warning(string message)
        {
            WriteLog("WARN", message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            var msg = ex != null ? $"{message}\nException: {ex}" : message;
            WriteLog("ERROR", msg);
        }

        public static void Debug(string message)
        {
            WriteLog("DEBUG", message);
        }

        private static void WriteLog(string level, string message)
        {
            lock (LockObj)
            {
                try
                {
                    var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                    File.AppendAllText(LogFilePath, logLine + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // 日志写入失败不抛出异常
                }
            }
        }
    }
}
