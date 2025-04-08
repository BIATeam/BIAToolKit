namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
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
    using static BIA.ToolKit.Application.Services.UIEventBroker;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class DtoGeneratorUC : UserControl
    {
        private readonly DtoGeneratorViewModel vm;

        private IConsoleWriter consoleWriter;
        private CSharpParserService parserService;
        private FileGeneratorService fileGeneratorService;
        private CRUDSettings settings;
        private Project project;
        private UIEventBroker uiEventBroker;
        private string dtoGenerationHistoryFile;
        private DtoGenerationHistory generationHistory = new DtoGenerationHistory();
        private DtoGeneration generation;
        private bool processSelectProperties;

        public DtoGeneratorUC()
        {
            InitializeComponent();
            vm = (DtoGeneratorViewModel)DataContext;
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(CSharpParserService parserService, SettingsService settingsService, IConsoleWriter consoleWriter, FileGeneratorService fileGeneratorService,
            UIEventBroker uiEventBroker)
        {
            this.consoleWriter = consoleWriter;
            this.parserService = parserService;
            this.settings = new(settingsService);
            this.fileGeneratorService = fileGeneratorService;
            this.uiEventBroker = uiEventBroker;
            this.uiEventBroker.OnProjectChanged += UIEventBroker_OnProjectChanged;

            vm.Inject(consoleWriter);
        }

        private void UIEventBroker_OnProjectChanged(Project project, TabItemModifyProjectEnum currentTabItem)
        {
            if (currentTabItem != TabItemModifyProjectEnum.DtoGenerator)
                return;

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

            if (!Version.TryParse(project.FrameworkVersion, out Version projectVersion))
            {
                consoleWriter.AddMessageLine($"ERROR: invalid project version", "red");
                return;
            }

            if (!fileGeneratorService.Init(projectVersion))
            {
                consoleWriter.AddMessageLine($"ERROR: incompatible project version", "red");
                return;
            }

            this.project = project;
            vm.SetProject(project);

            ListEntities();
            InitHistoryFile(project);
        }

        private void ListEntities()
        {
            var domainEntities = parserService.GetDomainEntities(project, settings);
            vm.SetEntities(domainEntities);
        }

        private void InitHistoryFile(Project project)
        {
            dtoGenerationHistoryFile = Path.Combine(project.Folder, Constants.FolderBia, settings.DtoGenerationHistoryFileName);
            if (File.Exists(dtoGenerationHistoryFile))
            {
                generationHistory = CommonTools.DeserializeJsonFile<DtoGenerationHistory>(dtoGenerationHistoryFile);
            }
        }

        private void RefreshEntitiesList_Click(object sender, RoutedEventArgs e)
        {
            ListEntities();
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

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateHistoryFile();
            await fileGeneratorService.GenerateDto(project, vm.SelectedEntityInfo, vm.EntityDomain, vm.MappingEntityProperties);
        }

        private void UpdateHistoryFile()
        {
            var isNewGeneration = generation is null;
            generation ??= new DtoGeneration();

            generation.DateTime = DateTime.Now;
            generation.EntityName = vm.SelectedEntityInfo.Name;
            generation.EntityNamespace = vm.SelectedEntityInfo.Namespace;
            generation.Domain = vm.EntityDomain;
            generation.PropertyMappings.Clear();

            foreach (var property in vm.MappingEntityProperties)
            {
                var generationPropertyMapping = new DtoGenerationPropertyMapping
                {
                    DateType = property.MappingDateType,
                    EntityPropertyCompositeName = property.EntityCompositeName,
                    IsRequired = property.IsRequired,
                    MappingName = property.MappingName,
                    OptionMappingDisplayProperty = property.OptionDisplayProperty,
                    OptionMappingEntityIdProperty = property.OptionEntityIdProperty,
                    OptionMappingIdProperty = property.OptionIdProperty
                };
                generation.PropertyMappings.Add(generationPropertyMapping);
            }

            if (isNewGeneration)
            {
                generationHistory.Generations.Add(generation);
            }

            if (!Directory.Exists(Path.GetDirectoryName(dtoGenerationHistoryFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dtoGenerationHistoryFile));
            }
            CommonTools.SerializeToJsonFile(generationHistory, dtoGenerationHistoryFile);
        }

        private void MappingPropertyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm.ComputePropertiesValidity();
        }

        private void MappingOptionId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(processSelectProperties)
                return;

            vm.ComputePropertiesValidity();
        }

        private void EntitiesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var entity = vm.SelectedEntityInfo;
            if (entity is null)
                return;

            LoadFromHistory(entity);
        }

        private void LoadFromHistory(EntityInfo entity)
        {
            generation = generationHistory.Generations.FirstOrDefault(g => g.EntityName.Equals(entity.Name) && g.EntityNamespace.Equals(entity.Namespace));
            if (generation is null)
            {
                vm.WasAlreadyGenerated = false;
                return;
            }

            vm.WasAlreadyGenerated = true;
            vm.EntityDomain = generation.Domain;


            var allEntityProperties = vm.AllEntityPropertiesRecursively.ToList();
            foreach (var property in allEntityProperties)
            {
                property.IsSelected = false;
            }
            foreach (var property in generation.PropertyMappings)
            {
                var entityProperty = allEntityProperties.FirstOrDefault(x => x.CompositeName == property.EntityPropertyCompositeName);
                if (entityProperty is null)
                    continue;

                entityProperty.IsSelected = true;
            }

            vm.RefreshMappingProperties();

            foreach(var property in generation.PropertyMappings)
            {
                var mappingProperty = vm.MappingEntityProperties.FirstOrDefault(x => x.EntityCompositeName == property.EntityPropertyCompositeName);
                if (mappingProperty is null)
                    continue;

                mappingProperty.MappingName = property.MappingName;
                mappingProperty.MappingDateType = property.DateType;
                mappingProperty.IsRequired = property.IsRequired;
                mappingProperty.OptionIdProperty = property.OptionMappingIdProperty;
                mappingProperty.OptionDisplayProperty = property.OptionMappingDisplayProperty;
                mappingProperty.OptionEntityIdProperty = property.OptionMappingEntityIdProperty;
            }

            vm.ComputePropertiesValidity();
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
