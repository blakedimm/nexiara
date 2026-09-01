using System;
using System.IO;

namespace Nexiara.Abstractions
{
    public static class NexiaraLogger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "Nexiara_Debug_Log.txt"
        );

        private static readonly object _lock = new object();

        public static void Log(string tag, string message)
        {
            string logLine = $"[{DateTime.Now:HH:mm:ss.fff}] [{tag}] {message}";

            Console.WriteLine(logLine);

            try
            {
                lock (_lock)
                {
                    File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
                }
            }
            catch { }
        }

        public static void ClearLog()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    File.Delete(LogFilePath);
                }
            }
            catch { }
        }
    }
}