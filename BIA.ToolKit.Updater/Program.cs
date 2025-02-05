namespace BIA.ToolKit.Updater
{
    using System.Diagnostics;
    using System.IO.Compression;
    using System.Reflection;

    internal class Program
    {
        private const string BiaToolkitApplicationName = "BIA.ToolKit.exe";

        static async Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException($"Use : Bia.ToolKit.Updater.exe [biaToolKit_directory_path] [Update_archive_path]");
            }

            string appPath = args[0].Replace("\"", string.Empty);
            if(!Directory.Exists(appPath))
            {
                throw new ArgumentException("First argument must be a valid directory path.");
            }

            string zipPath = args[1].Replace("\"", string.Empty);
            if (!File.Exists(zipPath))
            {
                throw new ArgumentException("Second argument must be a valid file path.");
            }

            if(!Directory.GetFiles(appPath).Any(file => Path.GetFileName(file).Equals(BiaToolkitApplicationName)))
            {
                throw new Exception($"{appPath} is not a valid installation path of BiaToolKit");
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[START BIATOOLKIT UPGRADE]");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Closing BIA.ToolKit instances...");
            CloseRunningApp();

            Console.WriteLine("Installing update...");
            InstallUpdate(appPath, zipPath);

            Console.WriteLine("Launching BIA.ToolKit...");
            RestartApp(appPath);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[END BIATOOLKIT UPGRADE]");

            await Task.Delay(2000);
        }

        static void CloseRunningApp()
        {
            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(BiaToolkitApplicationName)))
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        static void InstallUpdate(string appPath, string zipPath)
        {
            foreach (var directory in Directory.GetDirectories(appPath, "*", SearchOption.AllDirectories).ToList())
            {
                try
                {
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, true);
                    }
                }
                finally { }
            }

            var applicationRootFiles = Directory.GetFiles(appPath)
                .Where(file => !Path.GetFileNameWithoutExtension(file).Equals(Assembly.GetExecutingAssembly().GetName().Name))
                .ToList();

            foreach (var file in applicationRootFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                finally { }
            }

            ZipFile.ExtractToDirectory(zipPath, appPath, true);
            File.Delete(zipPath);
        }

        static void RestartApp(string appPath)
        {
            string exePath = Path.Combine(appPath, BiaToolkitApplicationName);
            if (exePath != null)
                Process.Start(exePath);
        }
    }
}
