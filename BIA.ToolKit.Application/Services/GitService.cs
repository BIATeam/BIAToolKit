namespace BIA.ToolKit.Application.Services
{
    using BIAToolKit.ToolKit.Application.Helper;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Management.Automation;
    using System;

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

            outPut.AddMessageLine("Synchronize BIADemo local folder finished", "Green");
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
