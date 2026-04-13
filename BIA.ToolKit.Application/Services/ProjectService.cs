namespace BIA.ToolKit.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Domain.ModifyProject;

    public class ProjectService(CSharpParserService parserService, IConsoleWriter consoleWriter)
    {
        private readonly CSharpParserService parserService = parserService;
        private readonly IConsoleWriter consoleWriter = consoleWriter;

        public async Task LoadProject(Project project, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                consoleWriter.AddMessageLine($"Loading project {project.Name}", "pink");

                consoleWriter.AddMessageLine("List project's files...", "darkgray");
                await project.ListProjectFiles();
                project.SolutionPath = project.ProjectFiles.FirstOrDefault(path =>
                    path.EndsWith($"{project.Name}.sln", StringComparison.InvariantCultureIgnoreCase));
                consoleWriter.AddMessageLine("Project's files listed", "lightgreen");

                consoleWriter.AddMessageLine("Resolving names and version...", "darkgray");

                NamesAndVersionResolver nvResolverOldVersions = new()
                {
                    ConstantFileRegExpPath = @"\\.*\\(.*)\.(.*)\.Common\\Constants\.cs$",
                    ConstantFileNameSearchPattern = "Constants.cs",
                    ConstantFileNamespace = @"^namespace\s+([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\.",
                    ConstantFileRegExpVersion = @" FrameworkVersion[\s]*=[\s]* ""([0-9]+\.[0-9]+\.[0-9]+)""[\s]*;[\s]*$",
                    FrontFileRegExpPath = null,
                    FrontFileUsingBiaNg = null,
                    FrontFileBiaNgImportRegExp = null,
                    FrontFileNameSearchPattern = null
                };

                NamesAndVersionResolver nvResolver = new()
                {
                    ConstantFileRegExpPath = @"\\DotNet\\(.*)\.(.*)\.Crosscutting\.Common\\[Bia\\]*Constants\.cs$",
                    ConstantFileNameSearchPattern = "Constants.cs",
                    ConstantFileNamespace = @"^namespace\s+([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\.",
                    ConstantFileRegExpVersion = @" FrameworkVersion[\s]*=[\s]* ""([0-9]+\.[0-9]+\.[0-9]+)""[\s]*;[\s]*$",
                    FrontFileRegExpPath =
                    [
                        @"\\(.*)\\src\\app\\core\\bia-core\\bia-core.module\.ts$",
                        @"\\(.*)\\packages\\bia-ng\\core\\bia-core.module\.ts$"
                    ],
                    FrontFileUsingBiaNg = @"\\(?!.*(?:\\node_modules\\|\\dist\\|\\\.angular\\))(.*)\\package\.json$",
                    FrontFileBiaNgImportRegExp = "\"@bia-team/bia-ng\":",
                    FrontFileNameSearchPattern = "bia-core.module.ts"
                };

                var resolverTask = Task.Run(() => nvResolver.ResolveNamesAndVersion(project), ct);
                var resolverOldVersionsTask = Task.Run(() => nvResolverOldVersions.ResolveNamesAndVersion(project), ct);
                await Task.WhenAll(resolverTask, resolverOldVersionsTask);

                consoleWriter.AddMessageLine("Names and version resolved", "lightgreen");

                if (project.BIAFronts.Count == 0)
                {
                    consoleWriter.AddMessageLine("Unable to find any BIA front folder for this project", "orange");
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error while loading project : {ex.Message}", "red");
            }
        }

        public async Task ParseProject(Project project, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await parserService.LoadSolution(project.SolutionPath, ct);
                await parserService.ParseSolutionClasses(ct);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error while loading project solution : {ex.Message}", "red");
            }
        }
    }
}
