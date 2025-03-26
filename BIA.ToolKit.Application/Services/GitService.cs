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
    using BIA.ToolKit.Domain.Settings;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.RegularExpressions;

    public class GitService
    {
        private IConsoleWriter outPut;
        public GitService(IConsoleWriter outPut)
        {
            this.outPut = outPut;
        }

        public async Task Synchronize(Domain.Settings.RepositorySettings repository)
        {
            if (!Directory.Exists(repository.RootFolderPath))
            {
                await this.Clone(repository.Name, repository.UrlRepo, repository.RootFolderPath);
            }
            else
            {
                await this.Synchronize(repository.Name, repository.RootFolderPath);
            }
        }


        public async Task Synchronize(string repoName, string localPath)
        {
            outPut.AddMessageLine("Synchronize " + repoName + " local folder...", "Pink");
            if (RunScript("git", "pull", localPath).Result == 0)
            {
                outPut.AddMessageLine("Synchronize " + repoName + " local folder finished", "Green");
            }
            else
            {
                outPut.AddMessageLine("Error durring synchronize " + repoName + " local folder", "Red");
            }
        }

        public async Task Clone(string repoName, string url, string localPath)
        {
            //var cloneOptions = new CloneOptions { BranchName = "master", Checkout = true };
            //var cloneResult = Repository.Clone(url, localPath);
            outPut.AddMessageLine("Clone " + repoName + " local folder...", "Pink");

            if (await RunScript("git", $"clone \"" + url + "\" \"" + localPath + "\"") == 0)
            {
                outPut.AddMessageLine("Clone BIADemo local folder finished", "Green");
            }
            else
            {
                outPut.AddMessageLine("Error durring clone BIADemo local folder.", "Red");
            }
        }

        public List<string> GetTags(string localPath)
        {
            List<string> release = new List<string>();

            using (var repo = new Repository(localPath))
            {
                release = repo.Tags.Select(t => t.FriendlyName).ToList();
            }

            return release;
        }


        public void CheckoutTag(RepositorySettings repoSettings, string tag)
        {
            // git checkout tags/1.1.4
            outPut.AddMessageLine("Checkout Tag " + tag + "  for repo : " + repoSettings.Name + ".", "Pink");

            if (RunScript("git", $"checkout tags/" + tag, repoSettings.RootFolderPath).Result == 0)
            {
                outPut.AddMessageLine("Checkout Tag " + tag + "  for repo : " + repoSettings.Name + "finished", "Green");
            }
            else
            {
                outPut.AddMessageLine("Error durring Checkout Tag " + tag + "  for repo : " + repoSettings.Name, "Red");
            }
        }


        public bool DiffFolder(bool actionFinishedAtEnd, string rootPath, string name1, string name2, string migrateFilePath)
        {
            outPut.AddMessageLine($"Diff {name1} <> {name2}", "Pink");


            // git diff --no-index V3.3.3 V3.4.0 > .\\Migration\\CF_3.3.3-3.4.0.patch
            //await RunScript($"cd {rootPath} \r\n git diff --no-index --binary {name1} {name2} > {migrateFilePath}");
            int result = RunScript("git", $"diff --ignore-blank-lines --no-index --binary {name1} {name2} --output={migrateFilePath}", rootPath).Result;
            if (result == 0)
            {
                outPut.AddMessageLine("Error durring diff folder: No difference found ", "Red");
                return false;
            }
            else if (result == 1)
            {
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
                return true;
            }
            else
            {
                outPut.AddMessageLine("Error " + result + " durring diff folder", "Red");
                return false;
            }
        }

        public bool ApplyDiff(bool actionFinishedAtEnd, string projectPath, string migrateFilePath)
        {
            outPut.AddMessageLine($"Apply diff", "Pink");
            outPut.AddMessageLine($"On project : {projectPath}", "Pink");
            // cd "...\\YourProject" git apply --reject --whitespace=fix "3.2.2-3.3.0.patch" \
            int result = RunScript("git", $"apply --reject --unsafe-paths --whitespace=fix {migrateFilePath}", projectPath).Result;
            if (result == 0)
            {
                outPut.AddMessageLine("Apply diff finished", actionFinishedAtEnd ? "Green" : "Blue");
                return true;
            }
            else
            {
                if (result == 3)
                {
                    outPut.AddMessageLine("Error code " + result + " during apply diff.", "Red");
                    outPut.AddMessageLine("Migration will stop : Try using migration with \"Overwrite BIA first\" checked", "Orange");
                    return false;
                }
                else
                {
                    outPut.AddMessageLine("Error code " + result + " during apply diff.", "Orange");
                    return true;
                }


            }
        }

        public class MergeParameter
        {
            public string ProjectOriginPath { get; set; }
            public string ProjectOriginVersion { get; set; }
            public string ProjectTargetPath { get; set; }
            public string ProjectTargetVersion { get; set; }
            public string ProjectPath { get; set; }
            public string MigrationPatchFilePath { get; set; }
        }

        public void MergeRejeted(bool actionFinishedAtEnd, MergeParameter param)
        {
            outPut.AddMessageLine($"Apply merge on rejected", "Pink");

            MergeRejetedDirectory(param.ProjectPath, param);

            outPut.AddMessageLine("Apply merge on rejected", actionFinishedAtEnd ? "Green" : "Blue");
        }

        // Process all files in the directory passed in, recurse on any directories
        // that are found, and process the files they contain.
        public void MergeRejetedDirectory(string targetDirectory, MergeParameter param)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory, "*.rej");
            foreach (string fileName in fileEntries)
                MergeRejetedFileAsync(fileName, param);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                MergeRejetedDirectory(subdirectory, param);
        }

        // Insert logic for processing found files here.
        public void MergeRejetedFileAsync(string rejectedFilePath, MergeParameter param)
        {
            outPut.AddMessageLine("Merge file '" + rejectedFilePath + "'.", "White");

            var rejectedFileDiffInstruction = File.ReadAllLines(rejectedFilePath).First();
            (string rejectedFileOriginalFileRelativePath, string rejectedFileTargetFileRelativePath) = ExtractOriginalAndFinalRelativePathOfDiffInstruction(rejectedFileDiffInstruction);

            var migrationPatchFileDiffInstruction = File.ReadLines(param.MigrationPatchFilePath).FirstOrDefault(l => l.EndsWith(rejectedFileTargetFileRelativePath));
            (string migrationPatchFileOriginalFileRelativePath, string migrationPatchFileTargetFileRelativePath) = ExtractOriginalAndFinalRelativePathOfDiffInstruction(migrationPatchFileDiffInstruction);

            string finalProjectFile = rejectedFilePath.Substring(0, rejectedFilePath.Length - 4);
            string originalProjectFile = Path.Combine(param.ProjectOriginPath, migrationPatchFileOriginalFileRelativePath.Replace("/", "\\"));
            string targetProjectFile = Path.Combine(param.ProjectTargetPath, migrationPatchFileTargetFileRelativePath.Replace("/", "\\"));

            if (!File.Exists(originalProjectFile))
            {
                outPut.AddMessageLine($"Unable to perform merge : original project's file {originalProjectFile} doesn't exist", "red");
                return;
            }

            if (!File.Exists(targetProjectFile))
            {
                outPut.AddMessageLine($"Unable to perform merge : target project's file {targetProjectFile} doesn't exist", "red");
                return;
            }

            if (!File.Exists(finalProjectFile))
            {
                outPut.AddMessageLine($"Unable to perform merge : final project's file {finalProjectFile} doesn't exist", "red");
                return;
            }

            int result = RunScript("git", $"merge-file -L Src -L {param.ProjectOriginVersion} -L {param.ProjectTargetVersion} \"{finalProjectFile}\" \"{originalProjectFile}\" \"{targetProjectFile}\"").Result;
            if (result == 0)
            {
                File.Delete(rejectedFilePath);
            }
            if (result > 0)
            {
                outPut.AddMessageLine(result + " conflict to solve in file '" + rejectedFilePath + "'.", "Yellow");
                File.Delete(rejectedFilePath);
            }
            else if (result < 0)
            {
                outPut.AddMessageLine("Error " + result + " durring Merge file '" + rejectedFilePath + "'.", "Red");
            }
        }

        private (string OriginalRelativePath, string TargetRelativePath) ExtractOriginalAndFinalRelativePathOfDiffInstruction(string diffInstruction)
        {
            if (string.IsNullOrEmpty(diffInstruction))
            {
                return (string.Empty, string.Empty);
            }

            string pattern = @"a/(?<Part1>[^\s]+)\s+b/(?<Part2>[^\s]+)";
            var match = Regex.Match(diffInstruction, pattern);

            if (match.Success)
            {
                string part1 = match.Groups["Part1"].Value;
                string part2 = match.Groups["Part2"].Value;
                return (part1, part2);
            }

            return (string.Empty, string.Empty);
        }

        /// <summary>
        /// Runs a PowerShell script with parameters and prints the resulting pipeline objects to the console output. 
        /// </summary>
        /// <param name="program">The program name.</param>
        /// <param name="arguments">The argument.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private async Task<int> RunScript(string program, string arguments, string workingDirectory = null)
        {
            //bool ret = true;
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
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                return await RunProcessAsync(process).ConfigureAwait(false);
                /*
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
                process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    outPut.AddMessageLine("Exit code :" + process.ExitCode, "Red");
                    while (!process.StandardError.EndOfStream)
                    {
                        string message = process.StandardError.ReadLine();
                        if (message.Length> 200)
                        {
                            message = message.Substring(0, 200) + "...";
                        }
                        outPut.AddMessageLine(message, "Red");
                    }
                    ret = false;
                }*/
            }
            catch (Exception e)
            {
                outPut.AddMessageLine("Error in RunScript", "Red");
                outPut.AddMessageLine(e.Message, "Red");
                if (e.InnerException != null) outPut.AddMessageLine(e.InnerException.Message, "Red");
                if (e.StackTrace != null) outPut.AddMessageLine(e.StackTrace, "Red");
            }
            return -1;
        }

        private Task<int> RunProcessAsync(Process process)
        {
            var tcs = new TaskCompletionSource<int>();

            process.Exited += (s, ea) => tcs.SetResult(process.ExitCode);
            process.OutputDataReceived += (s, ea) =>
            {
                if (!string.IsNullOrEmpty(ea.Data))
                {
                    outPut.AddMessageLine(ea.Data, "White", false);
                    // Console.WriteLine(ea.Data);
                }
            };
            process.ErrorDataReceived += (s, ea) =>
            {
                if (!string.IsNullOrEmpty(ea.Data))
                {
                    outPut.AddMessageLine(ea.Data, "Red", false);
                    // Console.WriteLine("ERR: " + ea.Data);
                }
            };

            bool started = process.Start();
            if (!started)
            {
                //you may allow for the process to be re-used (started = false) 
                //but I'm not sure about the guarantees of the Exited event in such a case
                throw new InvalidOperationException("Could not start process: " + process);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }
    }
}
