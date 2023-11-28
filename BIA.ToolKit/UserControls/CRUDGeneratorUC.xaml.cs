namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class CRUDGeneratorUC : UserControl
    {
        IConsoleWriter consoleWriter;
        CSharpParserService service;
        ZipParserService zipService;
        GenerateCrudService crudService;
        private string entityName;
        private CRUDSettings crudSettings;
        private readonly CRUDGeneratorViewModel vm;

        /// <summary>
        /// Constructor
        /// </summary>
        public CRUDGeneratorUC()
        {
            InitializeComponent();
            vm = (CRUDGeneratorViewModel)base.DataContext;
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(CSharpParserService service, ZipParserService zipService, GenerateCrudService crudService, IConsoleWriter consoleWriter)
        {
            this.service = service;
            this.zipService = zipService;
            this.crudService = crudService;
            this.consoleWriter = consoleWriter;
        }

        /// <summary>
        /// Update CurrentProject value.
        /// </summary>
        public void SetCurrentProject(Project currentProject)
        {
            vm.CurrentProject = currentProject;
            crudSettings = new(consoleWriter);
            Init();
        }

        private void Init()
        {
            string dotnetFolderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderDotNet, "docs");
            string angularFolderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderAngular, "docs");

            vm.DtoFiles = ListDtoFiles();
            vm.CRUDDataFeature = new CRUDTypeData(CRUDType.Feature, crudSettings.Feature_DotNet, dotnetFolderPath, crudSettings.Feature_Angular, angularFolderPath);
            vm.CRUDDataOption = new CRUDTypeData(CRUDType.Option, crudSettings.Option_DotNet, dotnetFolderPath, crudSettings.Option_Angular, angularFolderPath);
            vm.CRUDDataTeam = new CRUDTypeData(CRUDType.Team, crudSettings.Team_DotNet, dotnetFolderPath, crudSettings.Team_Angular, angularFolderPath);
        }

        #region State change
        private void ModifyDto_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;
            vm.IsDtoParsed = false;
            this.entityName = crudService.GetEntityNameFromDto(vm.DtoSelected);
            vm.CRUDNameSingular = this.entityName;
        }

        private void ModifyEntitySingularText_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm.CRUDNamePlurial = string.Empty;
        }

        private void ModifyEntityPlurialText_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm.IsCheckedAction = true;
        }

        private void GenerateCrudFeature_Change(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;

            if ((bool)((CheckBox)sender).IsChecked)
            {
                vm.ZipDotNetSelected.Add(vm.CRUDDataFeature.DotNetZipName);
                vm.ZipAngularSelected.Add(vm.CRUDDataFeature.AngularZipName);
            }
            else
            {
                vm.ZipDotNetSelected.Remove(vm.CRUDDataFeature.DotNetZipName);
                vm.ZipAngularSelected.Remove(vm.CRUDDataFeature.AngularZipName);
            }

            vm.IsZipParsed = false;
            vm.IsCheckedAction = true;
        }

        private void GenerateCrudTeam_Change(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;

            if ((bool)((CheckBox)sender).IsChecked)
            {
                vm.ZipDotNetSelected.Add(vm.CRUDDataTeam.DotNetZipName);
                vm.ZipAngularSelected.Add(vm.CRUDDataTeam.AngularZipName);
            }
            else
            {
                vm.ZipDotNetSelected.Remove(vm.CRUDDataTeam.DotNetZipName);
                vm.ZipAngularSelected.Remove(vm.CRUDDataTeam.AngularZipName);
            }

            vm.IsZipParsed = false;
            vm.IsCheckedAction = true;
        }

        private void GenerateCrudOption_Change(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;

            if ((bool)((CheckBox)sender).IsChecked)
            {
                vm.ZipDotNetSelected.Add(vm.CRUDDataOption.DotNetZipName);
                vm.ZipAngularSelected.Add(vm.CRUDDataOption.AngularZipName);
            }
            else
            {
                vm.ZipDotNetSelected.Remove(vm.CRUDDataOption.DotNetZipName);
                vm.ZipAngularSelected.Remove(vm.CRUDDataOption.AngularZipName);
            }

            vm.IsZipParsed = false;
            vm.IsCheckedAction = true;
        }
        #endregion

        #region Button Action
        private void ParseDto_Click(object sender, RoutedEventArgs e)
        {
            ParseDtoFile();
            vm.IsDtoParsed = true;
        }

        private void ParseZip_Click(object sender, RoutedEventArgs e)
        {
            // Parse DotNet Zip files
            ParseDotNetZipFile();

            // Parse Angular Zip files
            ParseAngularZipFile();

            vm.IsZipParsed = true;
        }

        private void GenerateCrud_Click(object sender, RoutedEventArgs e)
        {
            // TODO NMA
            crudService.InitRenameValues(vm.CRUDNameSingular, vm.CRUDNamePlurial /* ,"Plane", "Planes" */);

            // Generation DotNet files
            string path = crudService.GenerateDotNetCrudFiles(this.entityName, vm.CurrentProject, vm.DtoEntity, vm.DotNetZipFilesContent);

            // Generation Angular files
            ClassDefinition cd = vm.DotNetZipFilesContent.Where(x => x.FileType == FileType.Dto).First();
            crudService.GenerateAngularCrudFiles(this.entityName, vm.CurrentProject, vm.DtoEntity, vm.AngularZipContentFiles/*, cd*/);

            System.Diagnostics.Process.Start("explorer.exe", path);
        }
        #endregion

        #region Private method
        /// <summary>
        /// List all Dto files from current project.
        /// </summary>
        private Dictionary<string, string> ListDtoFiles()
        {
            Dictionary<string, string> dtoFiles = new();

            string dtoFolder = $"{vm.CurrentProject.CompanyName}.{vm.CurrentProject.Name}.Domain.Dto";
            string path = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderDotNet, dtoFolder);

            // List files
            var files = Directory.GetFiles(path, "*Dto.cs", SearchOption.AllDirectories).ToList();
            // Build dictionnary: key = file Name, Value = full path
            files.ForEach(x => dtoFiles.Add(new FileInfo(x).Name, new FileInfo(x).FullName));

            return dtoFiles;
        }

        private void ParseDtoFile()
        {
            string fileName = vm.DtoFiles[vm.DtoSelected];
            if (string.IsNullOrWhiteSpace(fileName))
            {
                consoleWriter.AddMessageLine("Dto file not found to parse.", "Orange");
                return;
            }

            try
            {
                vm.DtoEntity = service.ParseEntity(fileName);
                if (vm.DtoEntity == null || vm.DtoEntity.Properties == null || vm.DtoEntity.Properties.Count <= 0)
                {
                    consoleWriter.AddMessageLine("No properties found on Dto file.", "Orange");
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }
        }

        private void ParseDotNetZipFile()
        {
            string fileName = Path.Combine(vm.CRUDDataFeature.DotNetZipPath, vm.CRUDDataFeature.DotNetZipName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                consoleWriter.AddMessageLine("No DotNet Zip files found to parse.", "Orange");
                return;
            }

            try
            {
                // Parse Feature Zip file
                (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZipAndExtract(fileName, this.entityName, vm.CurrentProject.CompanyName, vm.CurrentProject.Name, Constants.FolderDotNet);
                if (string.IsNullOrWhiteSpace(workingDirectoryPath))
                {
                    consoleWriter.AddMessageLine($"Zip archive not found: '{fileName}'.", "Orange");
                    return;
                }

                foreach (FileInfo fi in new DirectoryInfo(workingDirectoryPath).GetFiles())
                {
                    ClassDefinition classFile = service.ParseClassFile(fi.FullName);
                    classFile.PathOnZip = fileList[fi.Name];
                    FileType? type = zipService.GetFileType(fi.Name);
                    classFile.FileType = type;
                    classFile.EntityName = zipService.GetEntityName(fi.Name, type);
                    vm.DotNetZipFilesContent.Add(classFile);
                }

                // TODO NMA : 
                // Parse Option Zip file
                // Parse Team Zip file
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }
        }

        private void ParseAngularZipFile()
        {
            string fileName = Path.Combine(vm.CRUDDataFeature.AngularZipPath, vm.CRUDDataFeature.AngularZipName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                consoleWriter.AddMessageLine("No Angular Zip files found to parse.", "Orange");
                return;
            }

            try
            {
                // Parse Feature Zip file
                (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZipAndExtract(fileName, this.entityName,
                    vm.CurrentProject.CompanyName, vm.CurrentProject.Name, Constants.FolderAngular);

                ClassDefinition cd = vm.DotNetZipFilesContent.Where(x => x.FileType == FileType.Dto).First();
                if (cd == null)
                {
                    consoleWriter.AddMessageLine("Can't parse angular files, Dto 'plane' file not found.", "Orange");
                    return;
                }

                Dictionary<string, List<string>> planeDtoProperties = zipService.GetDtoProperties(cd.PropertyList);
                if (planeDtoProperties == null || planeDtoProperties.Count <= 0)
                {
                    consoleWriter.AddMessageLine("Can't read plane dto properties.", "Orange");
                    return;
                }

                // Analyze angular files
                foreach (KeyValuePair<string, string> file in fileList)
                {
                    CRUDAngularData data = new(file.Key, file.Value, workingDirectoryPath)
                    {
                        ExtractBlocks = zipService.AnalyzeAngularFile(Path.Combine(workingDirectoryPath, file.Key), planeDtoProperties)
                    };
                    vm.AngularZipContentFiles.Add(data);
                }

                // TODO NMA : 
                // Parse Option Zip file
                // Parse Team Zip file
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }
        }
        #endregion
    }

    class CRUDSettings
    {
        private readonly IConsoleWriter consoleWriter;
        public string Feature_DotNet { get; }
        public string Feature_Angular { get; }
        public string Option_DotNet { get; }
        public string Option_Angular { get; }
        public string Team_DotNet { get; }
        public string Team_Angular { get; }

        public CRUDSettings(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;

            Feature_DotNet = ReadSetting("CRUD_Feature_DotNet");
            Feature_Angular = ReadSetting("CRUD_Feature_Angular");
            Option_DotNet = ReadSetting("CRUD_Option_DotNet");
            Option_Angular = ReadSetting("CRUD_Option_Angular");
            Team_DotNet = ReadSetting("CRUD_Team_DotNet");
            Team_Angular = ReadSetting("CRUD_Team_Angular");
        }

        private string ReadSetting(string key)
        {
            string result = null;
            try
            {
                result = ConfigurationManager.AppSettings[key] ?? "Not Found";
            }
            catch (ConfigurationErrorsException)
            {
                consoleWriter.AddMessageLine("Error reading app settings", "Red");
            }
            return result;
        }
    }
}
