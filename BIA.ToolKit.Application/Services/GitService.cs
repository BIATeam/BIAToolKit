﻿namespace BIA.ToolKit.Application.Services
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
            await RunScript($"cd \"" + localPath + $"\" \r\n" + $"git pull");

            /*using (var repo = new Repository(localPath))
            {
                var result = Commands.Pull(repo, new LibGit2Sharp.Signature("BIAToolKit", "BIAToolKit", DateTimeOffset.Now), new PullOptions());
                outPut.AddMessageLine(result.Status.ToString(), "White");
            }*/

            outPut.AddMessageLine("Synchronize BIADemo local folder finished", "Green");
        }

        public async Task Clone(string repoName, string url, string localPath)
        {
            //var cloneOptions = new CloneOptions { BranchName = "master", Checkout = true };
            //var cloneResult = Repository.Clone(url, localPath);
            outPut.AddMessageLine("Clone " + repoName + " local folder...", "Pink");

            await RunScript($"git clone \"" + url+"\" \"" + localPath + "\"");

            outPut.AddMessageLine("Clone BIADemo local folder finished", "Green");
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

        public async Task DiffFolder(string rootPath, string name1, string name2, string migrateFilePath)
        {
            outPut.AddMessageLine($"Diff {name1} <> {name2}", "Pink");


            // git diff --no-index V3.3.3 V3.4.0 > .\\Migration\\CF_3.3.3-3.4.0.patch
            //await RunScript($"cd {rootPath} \r\n git diff --no-index --binary {name1} {name2} > {migrateFilePath}");
            await RunScript($"cd \"{rootPath}\" \r\n git diff --no-index --binary {name1} {name2} | Out-File -encoding OEM {migrateFilePath}");

            // Replace a/{name1}/ by a/
            FileTransform.ReplaceInFile(migrateFilePath, $"a/{name1}/", "a/");
            FileTransform.ReplaceInFile(migrateFilePath, $"a/{name2}/", "a/");

            FileTransform.ReplaceInFile(migrateFilePath, $"rename from {name1}/", "rename from ");

            // Replace b/{name2}/ by b/
            FileTransform.ReplaceInFile(migrateFilePath, $"b/{name2}/", "b/");
            FileTransform.ReplaceInFile(migrateFilePath, $"b/{name1}/", "b/");

            FileTransform.ReplaceInFile(migrateFilePath, $"rename to {name2}/", "rename to ");

            FileTransform.ReplaceInFile(migrateFilePath, $"\r\n", "\n");

            outPut.AddMessageLine("Diff folder finished", "Green");
        }

        public async Task ApplyDiff(string projectPath, string migrateFilePath)
        {
            outPut.AddMessageLine($"Apply diff", "Pink");

            // cd "...\\YourProject" git apply --reject --whitespace=fix "3.2.2-3.3.0.patch" \
            await RunScript($"cd \"{projectPath}\" \r\n git apply --reject --whitespace=fix --binary {migrateFilePath} \\ ");

            outPut.AddMessageLine("Apply diff finished", "Green");
        }

        public class MergeParameter
        {
            public string ProjectOriginPath { get; set; }
            public string ProjectTargetPath { get; set; }
            public string ProjectPath { get; set; }
        }

        public async Task MergeRejeted(MergeParameter param)
        {
            outPut.AddMessageLine($"Apply merge on rejected", "Pink");

            await MergeRejetedDirectory(param.ProjectPath, param);

            outPut.AddMessageLine("Apply merge on rejected", "Green");
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
                await RunScript($"git merge-file '{finalFile}' '{originalFile}' '{additionnalFile}'");
            }
        }


        /// <summary>
        /// Runs a PowerShell script with parameters and prints the resulting pipeline objects to the console output. 
        /// </summary>
        /// <param name="scriptContents">The script file contents.</param>
        /// <param name="scriptParameters">A dictionary of parameter names and parameter values.</param>
        private async Task<string> RunScript(string scriptContents, Dictionary<string, object> scriptParameters = null)
        {
            string output = "";
            // create a new hosted PowerShell instance using the default runspace.
            // wrap in a using statement to ensure resources are cleaned up.

            using (PowerShell ps = PowerShell.Create())
            {
                // specify the script code to run.
                ps.AddScript(scriptContents);

                // specify the parameters to pass into the script.
                if (scriptParameters != null) ps.AddParameters(scriptParameters);

                // execute the script and await the result.
                var pipelineObjects = await ps.InvokeAsync().ConfigureAwait(true);

                // print the resulting pipeline objects to the console.
                foreach (var item in pipelineObjects)
                {
                    outPut.AddMessageLine(item.BaseObject.ToString(), "White");
                }
            }
            return output;
        }
    }
}
