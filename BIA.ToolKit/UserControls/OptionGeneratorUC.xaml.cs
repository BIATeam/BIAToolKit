namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Markup;
    using Windows.Data.Xml.Dom;
    using static BIA.ToolKit.Application.Services.UIEventBroker;

    /// <summary>
    /// Interaction logic for OptionGenerator.xaml
    /// </summary>
    public partial class OptionGeneratorUC : UserControl
    {
        private UIEventBroker uiEventBroker;
        private OptionGeneratorViewModel vm;

        /// <summary>
        /// Constructor
        /// </summary>
        public OptionGeneratorUC()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(UIEventBroker uiEventBroker, OptionGeneratorViewModel optionGeneratorViewModel)
        {
            this.uiEventBroker = uiEventBroker;
            this.vm = optionGeneratorViewModel;
            DataContext = vm;
            Loaded += (_, _) => vm.Initialize();
            Unloaded += (_, _) => vm.Cleanup();
        }

        /// <summary>
        /// Action linked with "Entity files" combobox.
        /// </summary>
        private void ModifyEntity_SelectionChange(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;

            vm.OnEntitySelected(vm.Entity);

            OptionAlreadyGeneratedLabel.Visibility = vm.IsGenerated ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Action linked with "Refresh entities List" button.
        /// </summary>
        private void RefreshEntitiesList_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(() =>
            {
                vm.ListEntityFiles();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Action linked with "Generate" button.
        /// </summary>
        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(vm.GenerateOptionAsync);
        }

        /// <summary>
        /// Action linked with "Delete last generation" button.
        /// </summary>
        private void DeleteLastGeneration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var history = vm.GetCurrentEntityHistory();
                uiEventBroker.RequestExecuteActionWithWaiter(async () =>
                {
                    await vm.DeleteLastOptionAsync(history);
                    OptionAlreadyGeneratedLabel.Visibility = Visibility.Hidden;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error on deleting last '{vm.Entity?.Name}' generation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void BIAFront_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                vm.SetFrontGenerationSettings(e.AddedItems[0] as string);
            }
        }
    }
