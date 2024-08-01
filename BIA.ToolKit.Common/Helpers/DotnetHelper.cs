namespace BIA.ToolKit.Common.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class DotnetHelper
    {
        /// <summary>
        /// Removes the projects from solution.
        /// </summary>
        /// <param name="solutionPath">The solution path.</param>
        /// <param name="projectPaths">The project paths.</param>
        public static void RemoveProjectsFromSolution(string solutionPath, List<string> projectPaths)
        {
            if (!string.IsNullOrWhiteSpace(solutionPath) && projectPaths?.Any() == true)
            {
                foreach (string projectPath in projectPaths)
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo.FileName = "dotnet";
                        process.StartInfo.Arguments = $"sln {solutionPath} remove {projectPath}";
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();

                        while (process.StandardOutput.Peek() > -1)
                        {
                            System.Console.WriteLine(process.StandardOutput.ReadLine());
                        }

                        process.WaitForExit();
                    }
                }
            }
        }
    }
}
