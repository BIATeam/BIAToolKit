namespace BIA.ToolKit.Updater
{
    using System.Diagnostics;
    using System.IO.Compression;
    using System.Reflection;

    internal class Program
    {
        private const string BiaToolkitApplicationName = "BIA.ToolKit.exe";

        static void Main(string[] args)
        {
            Console.WriteLine($"Paramètres : {string.Join(", ", args)}");

            if (args.Length < 3)
            {
                throw new Exception($"Paramètres incorrects.");
            }

            string appPath = args[0].Replace("\"", string.Empty);
            string zipPath = args[1].Replace("\"", string.Empty);

            Console.WriteLine("Fermeture de l'application...");
            CloseRunningApp();

            Console.WriteLine("Installation de la mise à jour...");
            InstallUpdate(appPath, zipPath);

            Console.WriteLine("Redémarrage de l'application...");
            RestartApp(appPath);
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
            Console.WriteLine($"AppPath={appPath}");
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
