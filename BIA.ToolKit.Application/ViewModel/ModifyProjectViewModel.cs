namespace BIA.ToolKit.Application.ViewModel
{
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

        public string RootProjectsPath
        {
            get { return ModifyProject.RootProjectsPath; }
            set
            {
                if (ModifyProject.RootProjectsPath != value)
                {
                    ModifyProject.RootProjectsPath = value;
                    CurrentProject = null;

                    /* TODO recabler cela
                    if (ModifyProjectRootFolderText != null && CreateProjectRootFolderText != null && ModifyProjectRootFolderText.Text != CreateProjectRootFolderText.Text)
                    {
                        CreateProjectRootFolderText.Text = ModifyProjectRootFolderText.Text;
                    }
                    */
                    if (Directory.Exists(value))
                    {
                        DirectoryInfo di = new DirectoryInfo(value);
                        // Create an array representing the files in the current directory.
                        DirectoryInfo[] versionDirectories = di.GetDirectories("*", SearchOption.TopDirectoryOnly);

                        var projectList = new List<string>();
                        foreach (DirectoryInfo dir in versionDirectories)
                        {
                            //Add and select the last added
                            projectList.Add(dir.Name);
                        }
                        Projects = projectList;
                    }
                    else
                    {
                        Projects = null;
                    }


                    RaisePropertyChanged("RootProjectsPath");
                }
            }
        }

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

                    Regex reg = new Regex(currentProject.Folder.Replace("\\", "\\\\") + @"\\DotNet\\(.*)\.(.*)\.Crosscutting\.Common\\Constants\.cs$", RegexOptions.IgnoreCase);
                    string file = Directory.GetFiles(currentProject.Folder, "Constants.cs", SearchOption.AllDirectories)?.Where(path => reg.IsMatch(path))?.FirstOrDefault();
                    if (file != null)
                    {
                        var match = reg.Match(file);
                        currentProject.CompanyName = match.Groups[1].Value;
                        currentProject.Name = match.Groups[2].Value;
                        Regex regVersion = new Regex(@" FrameworkVersion[\s]*=[\s]* ""([0-9]+\.[0-9]+\.[0-9]+)""[\s]*;[\s]*$");

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

                    Regex reg2 = new Regex(currentProject.Folder.Replace("\\", "\\\\") + @"\\(.*)\\src\\app\\core\\bia-core\\bia-core.module\.ts$", RegexOptions.IgnoreCase);
                    List<string> filesFront = Directory.GetFiles(currentProject.Folder, "bia-core.module.ts", SearchOption.AllDirectories)?.Where(path => reg2.IsMatch(path)).ToList();
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

    }
}
