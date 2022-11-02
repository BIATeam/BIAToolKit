namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.ViewModel;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class DtoGeneratorUC : UserControl
    {
        DtoGeneratorViewModel _viewModel;

        public DtoGeneratorUC()
        {
            InitializeComponent();
            _viewModel = (DtoGeneratorViewModel)base.DataContext;
        }

        private void LoadProject_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ProjectBrowse_Click(object sender, RoutedEventArgs e)
        {

        }

/*
        private void ModifyProjectAddDTOEntityBrowse_Click(object sender, RoutedEventArgs e)
        {

            string projectDir = "";
            string path = ModifyProjectRootFolderText.Text;
            if (Directory.Exists(path))
            {
                projectDir = path;
                path = projectDir + "\\" + ModifyProjectName.Content;
                if (Directory.Exists(path))
                {
                    projectDir = path;
                    path = projectDir + "\\DotNet";
                    if (Directory.Exists(path))
                    {
                        projectDir = path;
                        path = projectDir + "\\" + ModifyProjectCompany.Content.ToString() + "." + ModifyProjectName.Content + ".Domain";
                        if (Directory.Exists(path))
                        {
                            projectDir = path;
                        }
                    }
                }
            }
            
                 if (FileDialog.BrowseFile(ModifyProjectAddDTOEntity, projectDir))
                 {
                     cSharpParserService.ParseEntity(ModifyProjectAddDTOEntity.Text);
                 }
        }

        private void ModifyProjectAddDTOLoadSolution_Click(object sender, RoutedEventArgs e)
        {
            string projectDir = "";
            string projectPath = "";
            string path = ModifyProjectRootFolderText.Text;
            if (Directory.Exists(path))
            {
                projectDir = path;
                path = projectDir + "\\" + ModifyProjectName.Content;
                if (Directory.Exists(path))
                {
                    projectDir = path;
                    path = projectDir + "\\DotNet";
                    if (Directory.Exists(path))
                    {
                        projectDir = path;
                        path = projectDir + "\\" + ModifyProjectCompany.Content.ToString() + "." + ModifyProjectName.Content + ".Domain";
                        if (Directory.Exists(path))
                        {
                            projectDir = path;
                            path = projectDir + "\\" + ModifyProjectCompany.Content.ToString() + "." + ModifyProjectName.Content + ".Domain.csproj";
                            if (File.Exists(path))
                            {
                                projectPath = path;
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(projectPath))
            {
                _ = cSharpParserService.ParseSolution(projectPath);
            }
        }*/
    }
}
