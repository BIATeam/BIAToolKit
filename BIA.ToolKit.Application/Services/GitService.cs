namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Management.Automation;
    using System;
    using LibGit2Sharp;
    using System.Linq;
    using LibGit2Sharp.Handlers;
    using System.Diagnostics;
    using System.IO;

    public class GitService
    {
        private IConsoleWriter outPut;
        public GitService(IConsoleWriter outPut)
        {
            this.outPut = outPut;
        }

        public async Task Synchronize(string repoName, string localPath)
        {
            outPut.AddMessageLine("Synchronize " + repoName + " local folder...", "Pink");
            if( RunScript("git", "pull", localPath))
            {
                outPut.AddMessageLine("Synchronize " + repoName + " local folder finished", "Green");
            }
        }

        public async Task Clone(string repoName, string url, string localPath)
        {
            //var cloneOptions = new CloneOptions { BranchName = "master", Checkout = true };
            //var cloneResult = Repository.Clone(url, localPath);
            outPut.AddMessageLine("Clone " + repoName + " local folder...", "Pink");

            if (RunScript("git", $"clone \"" + url+"\" \"" + localPath + "\""))
            {
                outPut.AddMessageLine("Clone BIADemo local folder finished", "Green");
            }
        }

        public List<string> GetRelease(string localPath)
        {
            List<string> release = new List<string>();

            using (var repo = new Repository(localPath))
            {
                release = repo.Tags.Select(t => t.FriendlyName).ToList();
            }
            
            return release;
        }

        public async Task DiffFolder(bool actionFinishedAtEnd, string rootPath, string name1, string name2, string migrateFilePath)
        {
            outPut.AddMessageLine($"Diff {name1} <> {name2}", "Pink");


            // git diff --no-index V3.3.3 V3.4.0 > .\\Migration\\CF_3.3.3-3.4.0.patch
            //await RunScript($"cd {rootPath} \r\n git diff --no-index --binary {name1} {name2} > {migrateFilePath}");
            RunScript("git", $"diff --no-index --binary {name1} {name2} --output={migrateFilePath}", rootPath );

            // Replace a/{name1}/ by a/
            FileTransform.ReplaceInFile(migrateFilePath, $"a/{name1}/", "a/");
            FileTransform.ReplaceInFile(migrateFilePath, $"a/{name2}/", "a/");

            FileTransform.ReplaceInFile(migrateFilePath, $"rename from {name1}/", "rename from ");

            // Replace b/{name2}/ by b/
            FileTransform.ReplaceInFile(migrateFilePath, $"b/{name2}/", "b/");
            FileTransform.ReplaceInFile(migrateFilePath, $"b/{name1}/", "b/");

            FileTransform.ReplaceInFile(migrateFilePath, $"rename to {name2}/", "rename to ");

            FileTransform.ReplaceInFile(migrateFilePath, $"\r\n", "\n");

            outPut.AddMessageLine("Diff folder finished", actionFinishedAtEnd ? "Green" : "Blue");
        }

        public async Task ApplyDiff(bool actionFinishedAtEnd,string projectPath, string migrateFilePath)
        {
            outPut.AddMessageLine($"Apply diff", "Pink");

            // cd "...\\YourProject" git apply --reject --whitespace=fix "3.2.2-3.3.0.patch" \
            RunScript("git", $"apply --reject --whitespace=fix --binary {migrateFilePath} \\ ", projectPath);

            outPut.AddMessageLine("Apply diff finished", actionFinishedAtEnd ? "Green" : "Blue");
        }

        public class MergeParameter
        {
            public string ProjectOriginPath { get; set; }
            public string ProjectOriginVersion { get; set; }
            public string ProjectTargetPath { get; set; }
            public string ProjectTargetVersion { get; set; }
            public string ProjectPath { get; set; }
        }

        public async Task MergeRejeted(bool actionFinishedAtEnd, MergeParameter param)
        {
            outPut.AddMessageLine($"Apply merge on rejected", "Pink");

            await MergeRejetedDirectory(param.ProjectPath, param);

            outPut.AddMessageLine("Apply merge on rejected", actionFinishedAtEnd ? "Green" : "Blue");
        }

        // Process all files in the directory passed in, recurse on any directories
        // that are found, and process the files they contain.
        public async Task MergeRejetedDirectory(string targetDirectory, MergeParameter param)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory, "*.rej");
            foreach (string fileName in fileEntries)
                await MergeRejetedFileAsync(fileName, param);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                await MergeRejetedDirectory(subdirectory, param);
        }

        // Insert logic for processing found files here.
        public async Task MergeRejetedFileAsync(string path, MergeParameter param)
        {
            outPut.AddMessageLine("Merge file '" + path + "'.", "White");

            string finalFile = path.Substring(0, path.Length - 4);
            string originalFile = param.ProjectOriginPath + finalFile.Substring(param.ProjectPath.Length);
            string additionnalFile = param.ProjectTargetPath + finalFile.Substring(param.ProjectPath.Length);

            if (File.Exists(finalFile) && File.Exists(originalFile) && File.Exists(additionnalFile))
            {
                RunScript("git", $"merge-file -L Src -L {param.ProjectOriginVersion} -L {param.ProjectTargetVersion} \"{finalFile}\" \"{originalFile}\" \"{additionnalFile}\"");
            }
        }


        /// <summary>
        /// Runs a PowerShell script with parameters and prints the resulting pipeline objects to the console output. 
        /// </summary>
        /// <param name="program">The program name.</param>
        /// <param name="arguments">The argument.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private bool RunScript(string program, string arguments, string workingDirectory = null)
        {
            bool ret = true;
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = program/*"git"*/,
                    Arguments = arguments/*"pull"*/,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                if (workingDirectory != null)
                {
                    startInfo.WorkingDirectory = workingDirectory/*"C:\\Users\\L025308\\AppData\\Roaming\\BIA.ToolKit\\1.0.0.0\\BIATemplate\\Repo"*/;
                }

                var process = new Process
                {
                    StartInfo = startInfo
                };
                process.Start();
                while (!process.StandardOutput.EndOfStream && !process.StandardError.EndOfStream)
                {
                    if (!process.StandardOutput.EndOfStream)
                    {
                        outPut.AddMessageLine(process.StandardOutput.ReadLine(), "White");
                    }
                    if (!process.StandardError.EndOfStream)
                    {
                        outPut.AddMessageLine(process.StandardError.ReadLine(), "Red");
                    }
                }
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    outPut.AddMessageLine("Exit code :" + process.ExitCode, "Red");
                    while (!process.StandardError.EndOfStream)
                    {
                        outPut.AddMessageLine(process.StandardError.ReadLine(), "Red");
                    }
                    ret = false;
                }
            }
            catch (Exception e)
            {
                outPut.AddMessageLine("Error in RunScript", "Red");
                outPut.AddMessageLine(e.Message, "Red");
                if (e.InnerException != null) outPut.AddMessageLine(e.InnerException.Message, "Red");
                if (e.StackTrace != null) outPut.AddMessageLine(e.StackTrace, "Red");
                ret = false;
            }
            return ret;
        }
    }
}
