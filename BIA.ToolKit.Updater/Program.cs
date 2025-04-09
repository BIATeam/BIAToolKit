namespace BIA.ToolKit.Updater
{
    using System;
    using System.Diagnostics;
    using System.IO.Compression;
    using System.Reflection;

    internal class Program
    {
        private const string BiaToolkitApplicationName = "BIA.ToolKit.exe";

        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                //Console.WriteLine("Force parammeter : " + args.Length);
                //if (args.Length > 0)
                //{
                //    Console.WriteLine(args[0]);
                //}
                //args = [
                //    "C:\\Program Files\\BIAToolkit\\",
                //    "C:\\Users\\xxxx\\AppData\\Local\\Temp\\BIAToolkit\\BIAToolKit.zip"
                //    ];
                throw new ArgumentException($"Use : Bia.ToolKit.Updater.exe [biaToolKit_directory_path] [Update_archive_path]");
            }

            string appPath = args[0].Replace("\"", string.Empty);
            string zipPath = args[1].Replace("\"", string.Empty);
            bool adminMode = false;
            if (args.Length >= 3)
            {
                adminMode = args[2].Replace("\"", string.Empty) == "AdminMode";
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Path: " + appPath);
            Console.WriteLine("Zip: " + zipPath);
            if (adminMode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Admin mode");
            }
            
            await Task.Delay(3000);


            if (!Directory.Exists(appPath))
            {
                throw new ArgumentException("First argument must be a valid directory path.");
            }



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

            bool isInstalled = false;
            try
            {
                InstallUpdate(appPath, zipPath, args);
                isInstalled = true;
            }
            catch (System.UnauthorizedAccessException e)
            {
                if (!adminMode)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Failed : You need Admin right to update the application. The Updater will retry as ADMIN in 5s...");
                    await Task.Delay(1000);
                    Console.Write(".");
                    await Task.Delay(1000);
                    Console.Write(".");
                    await Task.Delay(1000);
                    Console.Write(".");
                    await Task.Delay(1000);
                    Console.Write(".");
                    await Task.Delay(1000);
                    Console.WriteLine(".");
                    ExecuteAsAdmin(AppDomain.CurrentDomain.BaseDirectory + "\\BIA.ToolKit.Updater.exe", appPath, zipPath);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The installation failled in Admin mode.");
                    Console.WriteLine("Try to download directly the BIAToolKit.zip\r\n from https://github.com/BIATeam/BIAToolKit/releases");
                    Console.WriteLine($"And install it manualy on : {appPath}");

                    Console.WriteLine("Press any key to stop...");
                    Console.ReadKey();

                    Environment.Exit(0);
                }
            }
            catch(Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The installation failled.");
                Console.WriteLine("Try to download directly the BIAToolKit.zip\r\n from https://github.com/BIATeam/BIAToolKit/releases");
                Console.WriteLine($"And install it manualy on : {appPath}");

                Console.WriteLine("Press any key to stop...");
                Console.ReadKey();

                Environment.Exit(0);
            }
            //finally
            //{
            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine("The installation failled.");
            //    Console.WriteLine("Try to download directly the BIAToolKit.zip\r\n from https://github.com/BIATeam/BIAToolKit/releases");
            //    Console.WriteLine($"And install it manualy on : {appPath}");

            //    Console.WriteLine("Press any key to stop...");
            //    Console.ReadKey();

            //    Environment.Exit(0);
            //}

            Console.ForegroundColor = ConsoleColor.Yellow;
            if (isInstalled)
            {
                Console.WriteLine("Launching BIA.ToolKit...");
                RestartApp(appPath);

                Console.ForegroundColor = ConsoleColor.Yellow;
            }

            Console.WriteLine("[END BIATOOLKIT UPGRADE]");

            await Task.Delay(1000);
        }

        static void CloseRunningApp()
        {
            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(BiaToolkitApplicationName)))
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        static void InstallUpdate(string appPath, string zipPath, string[] args)
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
                catch { }
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
                catch { }
            }

            ZipFile.ExtractToDirectory(zipPath, appPath, true);
            //File.Delete(zipPath);
        }

        static public void ExecuteAsAdmin(string fileName, string appPath, string zipPath)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.ArgumentList.Add(appPath);       // First argument          
            proc.StartInfo.ArgumentList.Add(zipPath);       // second argument
            proc.StartInfo.ArgumentList.Add("AdminMode");       // third argument
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.Verb = "runas";
            proc.Start();
        }

        static void RestartApp(string appPath)
        {
            string exePath = Path.Combine(appPath, BiaToolkitApplicationName);
            if (exePath != null)
                Process.Start(exePath);
        }
    }
}
