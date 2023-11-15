﻿namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using System;
    using System.Collections.Generic;
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

        private CRUDGeneratorViewModel vm;

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
            vm.DtoFiles = ListDtoFiles();
            vm.ZipFiles = ListZipFiles();
        }

        #region Action
        private void ModifyDto_SelectionChanged(object sender, RoutedEventArgs e)
        {
            vm.IsDtoParsed = false;
            vm.DtoRootFilePath = vm.DtoFiles[vm.DtoSelected];
        }

        private void ModifyZip_SelectionChanged(object sender, RoutedEventArgs e)
        {
            vm.IsZipParsed = false;
            vm.ZipRootFilePath = vm.ZipFiles[vm.ZipSelected];
        }

        private void ParseDto_Click(object sender, RoutedEventArgs e)
        {
            this.entityName = GetEntityNameFromDto(vm.DtoSelected);
            ParseDtoFile(vm.DtoRootFilePath);
            vm.IsDtoParsed = true;
        }

        private void ParseZip_Click(object sender, RoutedEventArgs e)
        {
            if (vm.ZipSelected.StartsWith(Constants.FolderAngular))
            {
                // Parse Angular Zip files
                ParseAngularZipFile();
            }
            else if (vm.ZipSelected.StartsWith(Constants.FolderDotNet))
            {
                // Parse DotNet Zip files
                ParseDotNetZipFile();
            }
            else
            {
                consoleWriter.AddMessageLine($"*** Parsing Zip files not implemented ***", "Orange");
                return;
            }

            vm.IsZipParsed = true;
        }

        private void GenerateCrud_Click(object sender, RoutedEventArgs e)
        {
            if (vm.ZipSelected.StartsWith(Constants.FolderAngular))
            {
                // Generation Angular files
                crudService.GenerateAngularCrudFiles(this.entityName, vm.CurrentProject, vm.DtoEntity);
            }
            else if (vm.ZipSelected.StartsWith(Constants.FolderDotNet))
            {
                // Generation DotNet files
                crudService.GenerateDotNetCrudFiles(this.entityName, vm.CurrentProject, vm.DtoEntity, vm.DotNetZipFilesContent);
            }
            else
            {
                consoleWriter.AddMessageLine($"*** Generation CRUD files not implemented ***", "Orange");
                return;
            }
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

        /// <summary>
        /// List all Zip files from Dotnet and Angular folders.
        /// </summary>
        private Dictionary<string, string> ListZipFiles()
        {
            Dictionary<string, string> zipFiles = new();
            foreach (var dir in new List<string>() { Constants.FolderAngular, Constants.FolderDotNet })
            {
                string path = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, dir, "docs");
                // List files
                var files = Directory.GetFiles(path, "*.zip").ToList();
                // Build dictionnary: key = file Name, Value = full path
                files.ForEach(x => zipFiles.Add($@"{dir}\{new FileInfo(x).Name}", new FileInfo(x).FullName));
            }

            return zipFiles;
        }

        private void ParseDtoFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
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
            string fileName = vm.ZipRootFilePath;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            try
            {
                (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZip(fileName, this.entityName, vm.CurrentProject.CompanyName, vm.CurrentProject.Name, Constants.FolderDotNet);
                if (string.IsNullOrWhiteSpace(workingDirectoryPath))
                {
                    consoleWriter.AddMessageLine($"Zip archive not found: '{fileName}'.", "Orange");
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
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }
        }

        private void ParseAngularZipFile()
        {
            string fileName = vm.ZipRootFilePath;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            try
            {
                (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZip(fileName, this.entityName, vm.CurrentProject.CompanyName, vm.CurrentProject.Name, Constants.FolderAngular);
                if (string.IsNullOrWhiteSpace(workingDirectoryPath))
                {
                    consoleWriter.AddMessageLine($"Zip archive not found: '{fileName}'.", "Orange");
                }

                // TODO NMA

            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }
        }

        private string GetEntityNameFromDto(string dtoFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(dtoFileName);
            if (fileName.ToLower().EndsWith("dto"))
            {
                return fileName[..^3];   // name without 'dto' suffix
            }

            return fileName;
        }
        #endregion
    }
}
