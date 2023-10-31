namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Helper;
    using System;
    using System.IO;
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

        private CRUDGeneratorViewModel vm;

        /// <summary>
        /// Constructor
        /// </summary>
        public CRUDGeneratorUC()
        {
            InitializeComponent();
            vm = (CRUDGeneratorViewModel)base.DataContext;
        }

        public void Inject(CSharpParserService service, ZipParserService zipService, GenerateCrudService crudService, IConsoleWriter consoleWriter)
        {
            this.service = service;
            this.zipService = zipService;
            this.crudService = crudService;
            this.consoleWriter = consoleWriter;
        }

        public void SetCurrentProject(Project currentProject)
        {
            vm.CurrentProject = currentProject;
        }

        #region Action
        private void ModifyDtoRootFile_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void ModifyDtoRootFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            vm.DtoRootFilePath = FileDialog.BrowseFile(vm.DtoRootFilePath, "cs");
        }

        private void ModifyZipRootFile_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void ModifyZipRootFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            vm.ZipRootFilePath = FileDialog.BrowseFile(vm.ZipRootFilePath, "zip");
        }

        private void ParseDto_Click(object sender, RoutedEventArgs e)
        {
            ParseDtoFile(vm.DtoRootFilePath);
            vm.IsDtoParsed = true;
        }

        private void ParseZip_Click(object sender, RoutedEventArgs e)
        {
            ParseZipFile(vm.ZipRootFilePath, vm.DtoEntity.NamespaceLastPart);
            vm.IsZipParsed = true;
        }

        private void GenerateCrud_Click(object sender, RoutedEventArgs e)
        {
            crudService.GenerateCrudFiles(vm.CurrentProject, vm.DtoEntity);
        }
        #endregion

        #region Private method
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

        private void ParseZipFile(string fileName, string entityName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            try
            {
                string workingDir = zipService.ReadZip(fileName, entityName, vm.CurrentProject.CompanyName, vm.CurrentProject.Name);
                if (string.IsNullOrWhiteSpace(workingDir))
                {
                    consoleWriter.AddMessageLine("All files not found on zip archive.", "Orange");
                }

                foreach (FileInfo fi in new DirectoryInfo(workingDir).GetFiles())
                {
                    vm.ZipFilesContent.Add(service.ParseClassFile(fi.FullName));
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }
        }
        #endregion
    }
}
