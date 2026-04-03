namespace BIA.ToolKit.Common.Helpers
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class DotnetHelper
    {
        /// <summary>
        /// Removes the projects from solution.
        /// </summary>
        /// <param name="solutionPath">The solution path.</param>
        /// <param name="projectPaths">The project paths.</param>
        public static void RemoveProjectsFromSolution(string solutionPath, List<string> projectPaths)
        {
            if (!string.IsNullOrWhiteSpace(solutionPath) && projectPaths?.Count > 0)
            {
                var cmds = new List<string>();

                foreach (string projectPath in projectPaths)
                {
                    cmds.Add($"sln {solutionPath} remove {projectPath}");
                }

                ExecDotnetCmd(cmds);
            }
        }

        /// <summary>
        /// Executes the dotnet command.
        /// </summary>
        /// <param name="cmds">List of cmd.</param>
        private static void ExecDotnetCmd(List<string> cmds)
        {
            if (cmds?.Count > 0)
            {
                foreach (string cmd in cmds)
                {
                    using var process = new Process();
                    process.StartInfo.FileName = "dotnet";
                    process.StartInfo.Arguments = cmd;
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
