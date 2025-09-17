namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.Settings;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public class ModifyProjectViewModel : ObservableObject
    {
        private UIEventBroker eventBroker;
        private FileGeneratorService fileGeneratorService;
        private IConsoleWriter consoleWriter;
        private SettingsService settingsService;
        private CSharpParserService parserService;

        public ModifyProjectViewModel()
        {
            ModifyProject = new ModifyProject();
            OverwriteBIAFromOriginal = true;
        }

        public void Inject(UIEventBroker eventBroker, FileGeneratorService fileGeneratorService, IConsoleWriter consoleWriter, SettingsService settingsService, CSharpParserService parserService)
        {
            this.eventBroker = eventBroker;
            this.fileGeneratorService = fileGeneratorService;
            this.consoleWriter = consoleWriter;
            this.settingsService = settingsService;
            this.parserService = parserService;

            eventBroker.OnSettingsUpdated += EventBroker_OnSettingsUpdated;
        }

        private void EventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            RaisePropertyChanged(nameof(RootProjectsPath));
        }

        public ICommand RefreshProjectInformationsCommand => new RelayCommand((_) => RefreshProjectInformations());
        public ModifyProject ModifyProject { get; set; }

        private bool _isFileGeneratorServiceInit;
        public bool IsFileGeneratorServiceInit
        {
            get => _isFileGeneratorServiceInit;
            set
            {
                _isFileGeneratorServiceInit = value;
                RaisePropertyChanged(nameof(IsFileGeneratorServiceInit));
            }
        }

        private bool isProjectCompatibleCrudGenerator;

        public bool IsProjectCompatibleCrudGenerator
        {
            get { return isProjectCompatibleCrudGenerator; }
            set
            {
                isProjectCompatibleCrudGenerator = value;
                RaisePropertyChanged(nameof(IsProjectCompatibleCrudGenerator));
            }
        }


        private ObservableCollection<string> projects = [];
        public ObservableCollection<string> Projects
        {
            get => projects;
            set
            {
                if (projects != value)
                {
                    projects = value;
                    ModifyProject.Projects = [.. value];
                    RaisePropertyChanged(nameof(Projects));
                }
            }
        }

        public void RefreshProjetsList()
        {
            List<string> newProjects = null;

            if (Directory.Exists(RootProjectsPath))
            {
                DirectoryInfo di = new(RootProjectsPath);
                // Create an array representing the files in the current directory.
                DirectoryInfo[] versionDirectories = di.GetDirectories("*", SearchOption.TopDirectoryOnly);

                newProjects = new();
                foreach (DirectoryInfo dir in versionDirectories)
                {
                    //Add and select the last added
                    newProjects.Add(dir.Name);
                }
            }

            if (!newProjects.Select(x => Path.Combine(RootProjectsPath, x)).Contains(CurrentProject?.Folder))
            {
                Folder = null;
            }

            for (int i = 0; i < newProjects.Count; i++)
            {
                var existingProjectInNewProjects = Projects.FirstOrDefault(x => x == newProjects[i]);
                if (existingProjectInNewProjects is not null)
                    continue;

                Projects.Insert(i, newProjects[i]);
            }

            for (int i = 0; i < Projects.Count; i++)
            {
                var newProjectInExistingProjects = newProjects.FirstOrDefault(x => x == Projects[i]);
                if (newProjectInExistingProjects is not null)
                    continue;

                Projects.RemoveAt(i);
                i--;
            }
        }

        public string CurrentRootProjectsPath { get; set; }
        public string RootProjectsPath
        {
            get => settingsService?.Settings?.ModifyProjectRootProjectsPath;
            set
            {
                if (settingsService.Settings.ModifyProjectRootProjectsPath != value)
                {
                    CurrentRootProjectsPath = value;
                    settingsService.SetModifyProjectRootProjectPath(value);
                    RefreshProjetsList();
                }
            }
        }

        public class NamesAndVersionResolver
        {
            // RegExp for path to find a file that contain FrameworkVersion. Param1 in Path is the Company, Param2 in Path is the ProjectName
            public string ConstantFileRegExpPath { get; set; }
            // Pattern corresponding to the name of the file that contain FrameworkVersion.
            public string ConstantFileNameSearchPattern { get; set; }
            // RegExp to determine de Framewprkversion in the file: It is the Param 1
            public string ConstantFileRegExpVersion { get; set; }
            // RegExp to determine the namespace of the constant file. Param1 in Path is the Company, Param2 in Path is the ProjectName
            public string ConstantFileNamespace { get; set; }

            // RegExp for path of an unique file in Front 
            public string[] FrontFileRegExpPath { get; set; }
            // RegExp for path of the package.json file
            public string FrontFileUsingBiaNg { get; set; }
            // RegExp for findind import of bia-ng in package.json file
            public string FrontFileBiaNgImportRegExp { get; set; }
            // Pattern corresponding to the name of the file unique file in Front.
            public string FrontFileNameSearchPattern { get; set; }

            public void ResolveNamesAndVersion(Project currentProject)
            {
                if (!string.IsNullOrEmpty(ConstantFileNameSearchPattern))
                {
                    Regex reg = new Regex(currentProject.Folder.Replace("\\", "\\\\") + ConstantFileRegExpPath, RegexOptions.IgnoreCase);
                    string file = Directory.GetFiles(currentProject.Folder, ConstantFileNameSearchPattern, SearchOption.AllDirectories)?.Where(path => reg.IsMatch(path))?.FirstOrDefault();
                    if (file != null)
                    {
                        Regex regProjectName = new Regex(ConstantFileNamespace);
                        Regex regVersion = new Regex(ConstantFileRegExpVersion);

                        foreach (var line in File.ReadAllLines(file))
                        {
                            var matchNamespace = regProjectName.Match(line);
                            if (matchNamespace.Success)
                            {
                                currentProject.CompanyName = matchNamespace.Groups[1].Value;
                                currentProject.Name = matchNamespace.Groups[2].Value;
                                continue;
                            }

                            var matchVersion = regVersion.Match(line);
                            if (matchVersion.Success)
                            {
                                currentProject.FrameworkVersion = matchVersion.Groups[1].Value;
                                break;
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(FrontFileNameSearchPattern))
                {
                    currentProject.BIAFronts.Clear();
                    Regex regPackageJson = new Regex(currentProject.Folder.Replace("\\", "\\\\") + FrontFileUsingBiaNg, RegexOptions.IgnoreCase);
                    List<string> packagesJsonFiles = Directory.GetFiles(currentProject.Folder, "package.json", SearchOption.AllDirectories)?.Where(path => regPackageJson.IsMatch(path)).ToList();
                    if (packagesJsonFiles != null)
                    {
                        foreach (var fileFront in packagesJsonFiles)
                        {
                            string fileContent = File.ReadAllText(fileFront);
                            if (fileContent.Contains(FrontFileBiaNgImportRegExp))
                            {
                                var match = regPackageJson.Match(fileFront);
                                if (!currentProject.BIAFronts.Contains(match.Groups[1].Value))
                                {
                                    currentProject.BIAFronts.Add(match.Groups[1].Value);
                                }
                            }
                        }
                    }


                    foreach (string regex in FrontFileRegExpPath)
                    {
                        Regex reg2 = new Regex(currentProject.Folder.Replace("\\", "\\\\") + regex, RegexOptions.IgnoreCase);
                        List<string> filesFront = Directory.GetFiles(currentProject.Folder, FrontFileNameSearchPattern, SearchOption.AllDirectories)?.Where(path => reg2.IsMatch(path)).ToList();

                        if (filesFront != null)
                        {
                            foreach (var fileFront in filesFront)
                            {
                                var match = reg2.Match(fileFront);
                                if (!currentProject.BIAFronts.Contains(match.Groups[1].Value))
                                {
                                    currentProject.BIAFronts.Add(match.Groups[1].Value);
                                }
                            }
                        }
                    }

                }
            }
        }

        public Dictionary<string, NamesAndVersionResolver> CurrentProjectDetections { get; set; }

        public string Folder
        {
            get { return ModifyProject.CurrentProject?.Folder; }
            set
            {
                if (value == Path.GetFileName(Folder))
                    return;

                eventBroker.RequestExecuteActionWithWaiter(async () =>
                {
                    IsFileGeneratorServiceInit = false;
                    IsProjectCompatibleCrudGenerator = false;

                    Project currentProject = null;
                    if (value is not null)
                    {
                        currentProject = new Project
                        {
                            Name = value
                        };
                        currentProject.Folder = RootProjectsPath + "\\" + currentProject.Name;
                        await LoadProject(currentProject);
                    }

                    await InitFileGeneratorServiceFromProject(currentProject);
                    CurrentProject = currentProject;

                    if (CurrentProject is not null)
                    {
                        await LoadProjectSolution(currentProject);
                    }

                    RaisePropertyChanged(nameof(Folder));
                });
            }
        }

        private async Task InitFileGeneratorServiceFromProject(Project currentProject)
        {
            await fileGeneratorService.Init(currentProject);
            IsFileGeneratorServiceInit = fileGeneratorService.IsInit;
            IsProjectCompatibleCrudGenerator = GenerateCrudService.IsProjectCompatible(currentProject);
        }

        private async Task LoadProject(Project project)
        {
            try
            {
                consoleWriter.AddMessageLine($"Loading project {project.Name}", "yellow");
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
                    ConstantFileRegExpPath = @"\\DotNet\\(.*)\.(.*)\.Crosscutting\.Common\\Constants\.cs$",
                    ConstantFileNameSearchPattern = "Constants.cs",
                    ConstantFileNamespace = @"^namespace\s+([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\.",
                    ConstantFileRegExpVersion = @" FrameworkVersion[\s]*=[\s]* ""([0-9]+\.[0-9]+\.[0-9]+)""[\s]*;[\s]*$",
                    FrontFileRegExpPath =
                    [
                        @"\\(.*)\\src\\app\\core\\bia-core\\bia-core.module\.ts$",
                    @"\\(.*)\\packages\\bia-ng\\core\\bia-core.module\.ts$"
                    ],
                    FrontFileUsingBiaNg = @"\\(?!.*(?:\\node_modules\\|\\dist\\|\\\.angular\\))(.*)\\package\.json$",
                    FrontFileBiaNgImportRegExp = "\"bia-ng\":",
                    FrontFileNameSearchPattern = "bia-core.module.ts"
                };

                var resolverTask = Task.Run(() => nvResolver.ResolveNamesAndVersion(project));
                var resolverOldVersionsTask = Task.Run(() => nvResolverOldVersions.ResolveNamesAndVersion(project));
                await Task.WhenAll(resolverTask, resolverOldVersionsTask).ContinueWith((_) =>
                {
                    project.SolutionPath = Directory.GetFiles(project.Folder, $"{project.Name}.sln", SearchOption.AllDirectories).FirstOrDefault();
                });

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

        private async Task LoadProjectSolution(Project currentProject)
        {
            try
            {
                await parserService.LoadSolution(currentProject.SolutionPath);
                await parserService.ParseSolutionClasses();
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error while loading project solution : {ex.Message}", "red");
            }
        }

        private void RefreshProjectInformations()
        {
            eventBroker.RequestExecuteActionWithWaiter(async () =>
            {
                await LoadProject(CurrentProject);
                await InitFileGeneratorServiceFromProject(CurrentProject);
                await LoadProjectSolution(CurrentProject);
            });
        }

        public string FrameworkVersion
        {
            get { return String.IsNullOrEmpty(ModifyProject.CurrentProject?.FrameworkVersion) ? "???" : ModifyProject.CurrentProject.FrameworkVersion; }
        }

        public string CompanyName
        {
            get { return String.IsNullOrEmpty(ModifyProject.CurrentProject?.CompanyName) ? "???" : ModifyProject.CurrentProject.CompanyName; }
        }

        public string Name
        {
            get { return String.IsNullOrEmpty(ModifyProject.CurrentProject?.Name) ? "???" : ModifyProject.CurrentProject.Name; }
        }

        public string BIAFronts => ModifyProject.CurrentProject?.BIAFronts != null && ModifyProject.CurrentProject?.BIAFronts.Count > 0 ?
            string.Join(", ", ModifyProject.CurrentProject.BIAFronts) :
            "???";

        public Project CurrentProject
        {
            get { return ModifyProject.CurrentProject; }
            set
            {
                if (ModifyProject.CurrentProject != value)
                {
                    ModifyProject.CurrentProject = value;
                    RaisePropertyChanged(nameof(FrameworkVersion));
                    RaisePropertyChanged(nameof(CompanyName));
                    RaisePropertyChanged(nameof(Name));
                    RaisePropertyChanged(nameof(IsProjectSelected));
                    RaisePropertyChanged(nameof(BIAFronts));
                    eventBroker.NotifyProjectChanged(value);
                }
            }
        }

        public bool OverwriteBIAFromOriginal
        {
            get; set;
        }

        public bool IsProjectSelected => CurrentProject != null;
    }
}
