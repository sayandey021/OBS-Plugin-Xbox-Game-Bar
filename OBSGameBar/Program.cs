using System;
using System.IO;
using Windows.UI.Xaml;

namespace OBSGameBar
{
    public static class Program
    {
        private static string _logFile;

        public static void WriteLog(string msg)
        {
            try
            {
                if (_logFile == null)
                {
                    string localFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
                    _logFile = Path.Combine(localFolder, "app_crash.txt");
                }
                File.AppendAllText(_logFile, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
            }
            catch
            {
                try
                {
                    string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "app_crash.txt");
                    File.AppendAllText(fallback, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
                }
                catch { }
            }
        }

        static void Main(string[] args)
        {
            WriteLog("=== Program.Main entered ===");
            try
            {
                Application.Start((p) =>
                {
                    WriteLog("Application.Start callback invoked, creating App...");
                    try
                    {
                        var app = new App();
                        WriteLog("new App() instantiated successfully");
                    }
                    catch (Exception appEx)
                    {
                        WriteLog($"new App() exception: {appEx}");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                WriteLog($"Program.Main FATAL crash: {ex}");
                throw;
            }
            WriteLog("=== Program.Main exiting normally ===");
        }
    }
}
