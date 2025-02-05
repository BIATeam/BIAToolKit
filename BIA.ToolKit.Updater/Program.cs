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
            if (args.Length < 2)
            {
                Console.WriteLine("Paramètres incorrects.");
                return;
            }

            string appPath = args[0];
            string zipPath = args[1];

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
            var applicationFiles = Directory.GetFiles(appPath)
                .Where(file => !Path.GetFileNameWithoutExtension(file).Equals(Assembly.GetExecutingAssembly().FullName))
                .ToList();

            foreach(var applicationFile in applicationFiles)
            {
                File.Delete(applicationFile);
            }

            ZipFile.ExtractToDirectory(zipPath, appPath, true);
            Directory.Delete(Path.GetDirectoryName(zipPath)!, true);
        }

        static void RestartApp(string appPath)
        {
            string exePath = Path.Combine(appPath, BiaToolkitApplicationName);
            if (exePath != null)
                Process.Start(exePath);
        }
    }
}
