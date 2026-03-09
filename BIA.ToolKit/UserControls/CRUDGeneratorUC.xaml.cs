namespace BIA.ToolKit.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;

    /// <summary>
    /// Interaction logic for CRUDGeneratorUC.xaml
    /// </summary>
    public partial class CRUDGeneratorUC : UserControl
    {
        private const string DOTNET_TYPE = "DotNet";
        private const string ANGULAR_TYPE = "Angular";

        private IMessenger messenger;
        private CRUDGeneratorViewModel vm;

        /// <summary>
        /// Constructor
        /// </summary>
        public CRUDGeneratorUC()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(IMessenger messenger, CRUDGeneratorViewModel crudGeneratorViewModel)
        {
            this.messenger = messenger;
            this.vm = crudGeneratorViewModel;
            DataContext = vm;
            Loaded += (_, _) => vm.Initialize();
            Unloaded += (_, _) => vm.Cleanup();
        }

        /// <summary>
        /// Action linked with "Dto files" combobox.
        /// </summary>
        private void ModifyDto_SelectionChange(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;

            vm.OnDtoSelected(vm.DtoEntity);

            CrudAlreadyGeneratedLabel.Visibility = vm.IsDtoGenerated ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Action linked with "Entity name (singular)" textbox.
        /// </summary>
        private void ModifyEntitySingular_TextChange(object sender, TextChangedEventArgs e)
        {
            vm.CRUDNamePlural = string.Empty;
        }

        /// <summary>
        /// Action linked with "Entity name (plural)" textbox.
        /// </summary>
        private void ModifyEntityPlural_TextChange(object sender, TextChangedEventArgs e)
        {
            vm.IsSelectionChange = true;
        }

        /// <summary>
        /// Action linked with "Refresh Dto List" button.
        /// </summary>
        private void RefreshDtoList_Click(object sender, RoutedEventArgs e)
        {
            messenger.Send(new ExecuteWithWaiterMessage
            {
                Task = () =>
                {
                    vm.ListDtoFiles();
                    return System.Threading.Tasks.Task.CompletedTask;
                }
            });
        }

        /// <summary>
        /// Action linked with "Generate CRUD" button.
        /// </summary>
        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            messenger.Send(new ExecuteWithWaiterMessage { Task = vm.GenerateCRUDAsync });
        }

        /// <summary>
        /// Action linked with "Delete last generation" button.
        /// </summary>
        private void DeleteLastGeneration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var history = vm.GetCurrentDtoHistory();
                messenger.Send(new ExecuteWithWaiterMessage
                {
                    Task = async () =>
                    {
                        await vm.DeleteLastGenerationAsync(history);
                        CrudAlreadyGeneratedLabel.Visibility = Visibility.Collapsed;
                    }
                });
            }
            catch (Exception ex)
            {
                // consoleWriter is on VM, use MessageBox for unexpected errors
                MessageBox.Show($"Error on deleting last '{vm.CRUDNameSingular}' generation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Action linked with "Delete Annotations" button.
        /// </summary>
        private async void DeleteBIAToolkitAnnotations_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder message = new();
                message.AppendLine("Do you want to permanently remove all BIAToolkit annotations in code?");
                message.AppendLine("After that you will no longer be able to regenerate old CRUDs.");
                message.AppendLine();
                message.AppendLine("Be careful, this action is irreversible.");
                MessageBoxResult result = MessageBox.Show(message.ToString(), "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);

                if (result == MessageBoxResult.OK)
                {
                    List<string> folders = [
                        Path.Combine(vm.CurrentProject.Folder, Constants.FolderDotNet),
                        Path.Combine(vm.CurrentProject.Folder, vm.BiaFront, "src",  "app")
                    ];

                    await vm.DeleteAnnotationsAsync(folders);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error on cleaning annotations for project '{vm.CurrentProject.Name}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BiaFront_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedFront = e.AddedItems[0] as string;
                vm.SetFrontGenerationSettings(selectedFront);
                vm.ParseFrontDomains();
            }
        }
    }
}
