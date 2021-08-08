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
            await RunScript($"cd " + localPath + $" \r\n" + $"git pull");

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
            await RunScript($"cd {rootPath} \r\n git diff --no-index {name1} {name2} > {migrateFilePath}");

            // Replace a/{name1}/ by a/
            FileTransform.ReplaceInFile(migrateFilePath, $"a/{name1}/", "a/");
            FileTransform.ReplaceInFile(migrateFilePath, $"a/{name2}/", "a/");

            // Replace b/{name2}/ by b/
            FileTransform.ReplaceInFile(migrateFilePath, $"b/{name2}/", "b/");
            FileTransform.ReplaceInFile(migrateFilePath, $"b/{name1}/", "b/");

            FileTransform.ReplaceInFile(migrateFilePath, $"\r\n", "\n");

            outPut.AddMessageLine("Diff folder finished", "Green");
        }

        public async Task ApplyDiff(string projectPath, string migrateFilePath)
        {
            outPut.AddMessageLine($"Apply diff", "Pink");

            // cd "...\\YourProject" git apply --reject --whitespace=fix "3.2.2-3.3.0.patch" \
            await RunScript($"cd {projectPath} \r\n git apply --reject --whitespace=fix {migrateFilePath} \\ ");

            outPut.AddMessageLine("Apply diff finished", "Green");
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
