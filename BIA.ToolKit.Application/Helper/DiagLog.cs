namespace BIA.ToolKit.Application.Helper
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Diagnostic file logger used to root-cause UI freezes where in-app console output
    /// is not flushed before the Dispatcher locks up. Writes to %TEMP%\BIAToolkit_diag.log
    /// with synchronous flush after every line so the last line survives an app crash.
    /// </summary>
    public static class DiagLog
    {
        private static readonly object lockObj = new();
        public static readonly string FilePath = Path.Combine(Path.GetTempPath(), "BIAToolkit_diag.log");

        public static void Write(string message)
        {
            try
            {
                lock (lockObj)
                {
                    string line = $"[{DateTime.Now:HH:mm:ss.fff}] [T{Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}";
                    using var fs = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                    byte[] bytes = Encoding.UTF8.GetBytes(line);
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush(flushToDisk: true);
                }
            }
            catch
            {
                // Intentionally swallow — diagnostics must never crash the host.
            }
        }
    }
}
