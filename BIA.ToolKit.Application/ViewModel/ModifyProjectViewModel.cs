namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.ModifyProject;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class ModifyProjectViewModel : ObservableObject
    {
        public ModifyProjectViewModel()
        {
            ModifyProject = new ModifyProject();
            OverwriteBIAFromOriginal = true;
            SynchronizeSettings.AddCallBack("RootProjectsPath", DelegateSetRootProjectsPath);
        }

        //public IProductRepository Repository { get; set; }
        public ModifyProject ModifyProject { get; set; }

        public List<string> Projects
        {
            get { return ModifyProject.Projects; }
            set
            {
                if (ModifyProject.Projects != value)
                {
                    ModifyProject.Projects = value;
                    RaisePropertyChanged("Projects");
                }
            }
        }

        public void RefreshProjetsList()
        {
            List<string> projectList = null;
            string projectPath = ModifyProject.RootProjectsPath;

            if (Directory.Exists(projectPath))
            {
                DirectoryInfo di = new(projectPath);
                // Create an array representing the files in the current directory.
                DirectoryInfo[] versionDirectories = di.GetDirectories("*", SearchOption.TopDirectoryOnly);

                projectList = new();
                foreach (DirectoryInfo dir in versionDirectories)
                {
                    //Add and select the last added
                    projectList.Add(dir.Name);
                }
            }

            Projects = projectList;
        }

        public void DelegateSetRootProjectsPath(string value)
        {
            ModifyProject.RootProjectsPath = value;
            CurrentProject = null;

            RefreshProjetsList();

            RaisePropertyChanged("RootProjectsPath");
        }

        public string RootProjectsPath
        {
            get { return ModifyProject.RootProjectsPath; }
            set
            {
                if (ModifyProject.RootProjectsPath != value)
                {
                    SynchronizeSettings.SettingChange("RootProjectsPath", value);
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

            // RegExp for path of an unique file in Front 
            public string FrontFileRegExpPath { get; set; }
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
                        var match = reg.Match(file);
                        currentProject.CompanyName = match.Groups[1].Value;
                        currentProject.Name = match.Groups[2].Value;
                        Regex regVersion = new Regex(ConstantFileRegExpVersion = @" FrameworkVersion[\s]*=[\s]* ""([0-9]+\.[0-9]+\.[0-9]+)""[\s]*;[\s]*$");

                        foreach (var line in File.ReadAllLines(file))
                        {
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
                    Regex reg2 = new Regex(currentProject.Folder.Replace("\\", "\\\\") + FrontFileRegExpPath, RegexOptions.IgnoreCase);
                    List<string> filesFront = Directory.GetFiles(currentProject.Folder, FrontFileNameSearchPattern, SearchOption.AllDirectories)?.Where(path => reg2.IsMatch(path)).ToList();
                    currentProject.BIAFronts = "";
                    if (filesFront != null)
                    {
                        foreach (var fileFront in filesFront)
                        {
                            var match = reg2.Match(fileFront);
                            if (currentProject.BIAFronts != "")
                            {
                                currentProject.BIAFronts += ", ";
                            }
                            currentProject.BIAFronts += match.Groups[1].Value;
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
                Project currentProject = null;

                if (value != null)
                {

                    currentProject = new Domain.ModifyProject.Project();

                    currentProject.Name = value;
                    currentProject.Folder = RootProjectsPath + "\\" + currentProject.Name;

                    NamesAndVersionResolver nvResolver2 = new NamesAndVersionResolver()
                    {
                        ConstantFileRegExpPath = @"\\.*\\(.*)\.(.*)\.Common\\Constants\.cs$",
                        ConstantFileNameSearchPattern = "Constants.cs",
                        ConstantFileRegExpVersion = @" FrameworkVersion[\s]*=[\s]* ""([0-9]+\.[0-9]+\.[0-9]+)""[\s]*;[\s]*$",
                        FrontFileRegExpPath = null,
                        FrontFileNameSearchPattern = null
                    };
                    nvResolver2.ResolveNamesAndVersion(currentProject);

                    NamesAndVersionResolver nvResolver = new NamesAndVersionResolver()
                    {
                        ConstantFileRegExpPath = @"\\DotNet\\(.*)\.(.*)\.Crosscutting\.Common\\Constants\.cs$",
                        ConstantFileNameSearchPattern = "Constants.cs",
                        ConstantFileRegExpVersion = @" FrameworkVersion[\s]*=[\s]* ""([0-9]+\.[0-9]+\.[0-9]+)""[\s]*;[\s]*$",
                        FrontFileRegExpPath = @"\\(.*)\\src\\app\\core\\bia-core\\bia-core.module\.ts$",
                        FrontFileNameSearchPattern = "bia-core.module.ts"
                    };
                    nvResolver.ResolveNamesAndVersion(currentProject);
                }
                CurrentProject = currentProject;
            }
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

        public string BIAFronts
        {
            get { return String.IsNullOrEmpty(ModifyProject.CurrentProject?.BIAFronts) ? "???" : ModifyProject.CurrentProject.BIAFronts; }
        }

        public Project CurrentProject
        {
            get { return ModifyProject.CurrentProject; }
            set
            {
                if (ModifyProject.CurrentProject != value)
                {
                    ModifyProject.CurrentProject = value;
                    RaisePropertyChanged("FrameworkVersion");
                    RaisePropertyChanged("CompanyName");
                    RaisePropertyChanged("Name");
                    RaisePropertyChanged("BIAFronts");
                }
            }
        }

        public bool OverwriteBIAFromOriginal
        {
            get; set;
        }

    }
}
