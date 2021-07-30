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

        public async void Synchronize(string repoName, string localPath)
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

        public async void Clone(string repoName, string url, string localPath)
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
