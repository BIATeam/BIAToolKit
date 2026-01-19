namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.ViewModels;
    using BIA.ToolKit.Behaviors;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using Microsoft.Xaml.Behaviors;
    using System;
    using System.Data.Common;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.Messaging;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class DtoGeneratorUC : UserControl
    {
        private readonly DtoGeneratorViewModel vm;

        private CSharpParserService parserService;
        private FileGeneratorService fileGeneratorService;
        private CRUDSettings settings;
        private Project project;
        private IMessenger messenger;
        private bool processSelectProperties;
        private ViewModels.DtoGeneratorHelper helper;

        public DtoGeneratorUC()
        {
            InitializeComponent();
            vm = (DtoGeneratorViewModel)DataContext;
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(CSharpParserService parserService, SettingsService settingsService, IConsoleWriter consoleWriter, FileGeneratorService fileGeneratorService,
            IMessenger messenger)
        {
            this.parserService = parserService;
            this.settings = new(settingsService);
            this.fileGeneratorService = fileGeneratorService;
            this.messenger = messenger;
            messenger.Register<ProjectChangedMessage>(this, (r, m) => UIEventBroker_OnProjectChanged(m.Project));
            messenger.Register<SolutionClassesParsedMessage>(this, (r, m) => UiEventBroker_OnSolutionClassesParsed());

            vm.Inject(fileGeneratorService, consoleWriter);
            helper = new ViewModels.DtoGeneratorHelper(parserService, settings, consoleWriter);
        }

        private void UiEventBroker_OnSolutionClassesParsed()
        {
            ListEntities();
        }

        private void UIEventBroker_OnProjectChanged(Project project)
        {
            SetCurrentProject(project);
        }

        public void SetCurrentProject(Project project)
        {
            if (project == this.project)
                return;

            vm.IsProjectChosen = false;
            vm.Clear();

            if (project is null)
                return;

            InitProject(project);
        }

        private void InitProject(Project project)
        {
            this.project = project;
            helper.InitProject(project, vm);
        }

        private Task ListEntities()
        {
            if (project is null)
                return Task.CompletedTask;

            return helper.ListEntitiesAsync(project, vm);
        }

        private void RefreshEntitiesList_Click(object sender, RoutedEventArgs e)
        {
            messenger.Send(new ExecuteActionWithWaiterMessage(async () => await ListEntities()));
        }

        private void SelectProperties_Click(object sender, RoutedEventArgs e)
        {
            processSelectProperties = true;
            vm.RefreshMappingProperties();
            ResetMappingColumnsWidths();
            vm.ComputePropertiesValidity();
            processSelectProperties = false;
        }

        private void RemoveAllMappingProperties_Click(object sender, RoutedEventArgs e)
        {
            vm.RemoveAllMappingProperties();
        }

        private void ResetMappingColumnsWidths()
        {
            var gridView = PropertiesListView.View as GridView;
            foreach (var column in gridView.Columns)
            {
                column.Width = 0;
                column.Width = double.NaN;
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
            {
                helper.UpdateHistoryFile(vm);
                await fileGeneratorService.GenerateDtoAsync(new FileGeneratorDtoContext
                {
                    CompanyName = project.CompanyName,
                    ProjectName = project.Name,
                    DomainName = vm.EntityDomain,
                    EntityName = vm.Entity.Name,
                    EntityNamePlural = vm.Entity.NamePluralized,
                    BaseKeyType = vm.SelectedBaseKeyType,
                    Properties = [.. vm.MappingEntityProperties],
                    IsTeam = vm.IsTeam,
                    IsVersioned = vm.IsVersioned,
                    IsArchivable = vm.IsArchivable,
                    IsFixable = vm.IsFixable,
                    AncestorTeamName = vm.AncestorTeam,
                    HasAncestorTeam = !string.IsNullOrEmpty(vm.AncestorTeam),
                    GenerateBack = true,
                    HasAudit = vm.UseDedicatedAudit
                });
            }));
        }

        private void MappingPropertyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm.ComputePropertiesValidity();
        }

        private void MappingOptionId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (processSelectProperties)
                return;

            vm.ComputePropertiesValidity();
        }

        private void EntitiesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (vm.Entity is null)
                return;

            helper.LoadFromHistory(vm.Entity, vm);
        }


        private void DragHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var behavior = GetDragDropBehavior();
            behavior?.HandleDragStart(sender, e);
        }

        private void DragHandle_MouseMove(object sender, MouseEventArgs e)
        {
            var behavior = GetDragDropBehavior();
            behavior?.HandleDragMove(sender, e);
        }

        private ListViewDragDropBehavior GetDragDropBehavior()
        {
            return Interaction.GetBehaviors(PropertiesListView)
                              .OfType<ListViewDragDropBehavior>()
                              .FirstOrDefault();
        }
    }
}
