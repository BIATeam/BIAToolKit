namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services.Option;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    /// <summary>
    /// ViewModel for Option generation.
    /// Refactored to use IOptionGenerationService for business logic.
    /// </summary>
    public partial class OptionGeneratorViewModel : ObservableObject
    {
        #region Dependencies
        private readonly IOptionGenerationService _optionService;
        private readonly IMessenger _messenger;
        private readonly IConsoleWriter _consoleWriter;
        #endregion

        #region Constructor
        public OptionGeneratorViewModel(
            IOptionGenerationService optionService,
            IMessenger messenger,
            IConsoleWriter consoleWriter)
        {
            _optionService = optionService ?? throw new ArgumentNullException(nameof(optionService));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));

            Entities = new ObservableCollection<EntityInfo>();
            EntityDisplayItems = new ObservableCollection<string>();
            BiaFronts = new ObservableCollection<string>();

            _messenger.Register<ProjectChangedMessage>(this, (r, m) => SetCurrentProject(m.Project));
            _messenger.Register<SolutionClassesParsedMessage>(this, (r, m) => RefreshEntitiesList());
        }
        #endregion

        #region Observable Properties
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateOptionEnable))]
        private Project _currentProject;

        [ObservableProperty]
        private bool _isProjectChosen;

        [ObservableProperty]
        private string _biaFront;

        [ObservableProperty]
        private ObservableCollection<string> _biaFronts;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateOptionEnable))]
        private EntityInfo _entity;

        [ObservableProperty]
        private ObservableCollection<EntityInfo> _entities;

        [ObservableProperty]
        private ObservableCollection<string> _entityDisplayItems;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateOptionEnable))]
        private bool _isEntityParsed;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateOptionEnable))]
        private string _entityDisplayItemSelected;

        [ObservableProperty]
        private bool _isGenerated;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateOptionEnable))]
        private string _entityNamePlural;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateOptionEnable))]
        private string _domain;
        #endregion

        #region Computed Properties
        public bool IsButtonGenerateOptionEnable =>
            IsEntityParsed
            && !string.IsNullOrWhiteSpace(EntityNamePlural)
            && !string.IsNullOrWhiteSpace(EntityDisplayItemSelected)
            && !string.IsNullOrEmpty(Domain);
        #endregion

        #region Commands
        [RelayCommand]
        private void OnEntitySelected()
        {
            IsEntityParsed = false;
            EntityDisplayItems.Clear();
            Domain = null;
            EntityNamePlural = null;

            // Load from history if available
            if (Entity != null)
            {
                var history = _optionService.LoadEntityHistory(GetEntitySelectedPath());
                if (history != null)
                {
                    EntityNamePlural = history.EntityNamePlural;
                    Domain = history.Domain;
                    BiaFront = history.BiaFront;
                    IsGenerated = true;
                }
            }

            // Parse entity
            var parseResult = _optionService.ParseEntityFile(Entity);
            if (parseResult.Success)
            {
                Domain = parseResult.Domain;
                EntityNamePlural = parseResult.EntityNamePlural;
                foreach (var item in parseResult.DisplayItems)
                {
                    EntityDisplayItems.Add(item);
                }
                EntityDisplayItemSelected = parseResult.DefaultDisplayItem;
                IsEntityParsed = true;
            }
            else if (!string.IsNullOrEmpty(parseResult.ErrorMessage))
            {
                _consoleWriter.AddMessageLine(parseResult.ErrorMessage, "Orange");
            }
        }

        [RelayCommand]
        private void OnBiaFrontSelected(string selectedFront)
        {
            if (string.IsNullOrWhiteSpace(selectedFront))
                return;

            _optionService.LoadFrontSettings(selectedFront);
        }

        [RelayCommand]
        private void RefreshEntitiesList()
        {
            if (CurrentProject is null)
                return;

            var entities = _optionService.ListEntities(CurrentProject);
            Entities = new ObservableCollection<EntityInfo>(entities);
        }

        [RelayCommand]
        private async Task GenerateOptionAsync()
        {
            if (CurrentProject is null || Entity is null)
                return;

            var request = new OptionGenerationRequest
            {
                Entity = Entity,
                EntityNamePlural = EntityNamePlural,
                DisplayItem = EntityDisplayItemSelected,
                Domain = Domain,
                BiaFront = BiaFront
            };

            IsGenerated = await _optionService.GenerateAsync(request);

            if (IsGenerated)
            {
                UpdateGenerationHistory();
                _consoleWriter.AddMessageLine($"End of '{Entity.Name}' option generation.", "Blue");
            }
        }

        [RelayCommand]
        private void DeleteLastGeneration()
        {
            try
            {
                var history = _optionService.LoadEntityHistory(GetEntitySelectedPath());
                if (history == null)
                {
                    _consoleWriter.AddMessageLine($"No previous '{Entity?.Name}' generation found.", "Orange");
                    return;
                }

                var request = new OptionDeletionRequest
                {
                    History = history,
                    Domain = history.Domain
                };

                _optionService.DeleteLastGeneration(request);
                IsGenerated = false;

                _consoleWriter.AddMessageLine($"End of '{Entity?.Name}' suppression.", "Purple");
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"Error on deleting last '{Entity?.Name}' generation: {ex.Message}", "Red");
            }
        }

        [RelayCommand]
        private async Task DeleteAnnotationsAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    "Do you want to permanently remove all BIAToolkit annotations in code?\nAfter that you will no longer be able to regenerate old CRUDs.\n\nBe careful, this action is irreversible.",
                    "Warning",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning,
                    MessageBoxResult.Cancel);

                if (result == MessageBoxResult.OK)
                {
                    var folders = new[]
                    {
                        Path.Combine(CurrentProject.Folder, Constants.FolderDotNet),
                        Path.Combine(CurrentProject.Folder, BiaFront, "src", "app")
                    };

                    await _optionService.DeleteAnnotationsAsync(folders);
                }

                _consoleWriter.AddMessageLine("End of annotations suppression.", "Purple");
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"Error on cleaning annotations for project '{CurrentProject?.Name}': {ex.Message}", "Red");
            }
        }
        #endregion

        #region Lifecycle Methods
        public async void SetCurrentProject(Project project)
        {
            if (project == CurrentProject)
                return;

            ClearAll();
            CurrentProject = project;
            IsProjectChosen = project != null && project.BIAFronts.Count > 0;

            if (IsProjectChosen)
            {
                await InitializeProjectAsync();
                
                foreach (var biaFront in project.BIAFronts)
                {
                    BiaFronts.Add(biaFront);
                }
                BiaFront = BiaFronts.FirstOrDefault();
            }
        }

        private async Task InitializeProjectAsync()
        {
            try
            {
                await _optionService.InitializeAsync(CurrentProject);
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"Error on initializing project: {ex.Message}", "Red");
            }
        }

        private void ClearAll()
        {
            EntityDisplayItems.Clear();
            EntityDisplayItemSelected = null;
            Entity = null;
            Entities.Clear();
            EntityNamePlural = null;
            Domain = null;
            BiaFronts.Clear();
            BiaFront = null;
            IsEntityParsed = false;
            IsGenerated = false;
        }
        #endregion

        #region Private Helpers
        private string GetEntitySelectedPath()
        {
            if (Entity is null || CurrentProject is null)
                return null;

            string dotNetPath = Path.Combine(CurrentProject.Folder, Constants.FolderDotNet);
            return Entity.Name.Replace(dotNetPath, string.Empty).TrimStart(Path.DirectorySeparatorChar);
        }

        private void UpdateGenerationHistory()
        {
            try
            {
                var history = new OptionGenerationHistory
                {
                    Date = DateTime.Now,
                    EntityNameSingular = Entity.Name,
                    EntityNamePlural = EntityNamePlural,
                    DisplayItem = EntityDisplayItemSelected,
                    Domain = Domain,
                    BiaFront = BiaFront,
                    Mapping = new EntityMapping
                    {
                        Entity = GetEntitySelectedPath(),
                        Type = "DotNet"
                    }
                };

                _optionService.UpdateHistory(history);
                IsGenerated = true;
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"Error on Option generation history: {ex.Message}", "Red");
            }
        }
        #endregion
    }
}
