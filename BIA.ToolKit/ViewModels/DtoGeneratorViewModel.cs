namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.DTO;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Services.FileGenerator.Models;

    /// <summary>
    /// ViewModel for DTO generation.
    /// Refactored to use IDtoGenerationService for business logic while keeping UI mapping logic.
    /// </summary>
    public partial class DtoGeneratorViewModel : ObservableObject
    {
        #region Dependencies
        private readonly IDtoGenerationService _dtoService;
        private readonly FileGeneratorService _fileGeneratorService;
        private readonly IConsoleWriter _consoleWriter;
        private readonly IMessenger _messenger;
        private readonly DtoGeneratorHelper _helper;
        private readonly CRUDSettings _settings;
        #endregion

        #region Private Fields
        private Project _project;
        
        private readonly List<string> _optionCollectionsMappingTypes = ["icollection", "list"];
        private readonly List<string> _standardMappingTypes =
        [
            "bool", "byte", "sbyte", "char", "decimal", "double", "float",
            "int", "uint", "long", "ulong", "short", "ushort", "string",
            "DateTime", "DateOnly", "TimeOnly", "byte[]", "Guid"
        ];
        private readonly Dictionary<string, string> _specialTypeToRemap = new()
        {
            { "TimeSpan", "string" },
            { "TimeSpan?", "string" },
        };
        private readonly Dictionary<string, List<string>> _biaDtoFieldDateTypesByPropertyType = new()
        {
            { "TimeSpan", ["time"] },
            { "DateTime", ["datetime", "date", "time"] },
        };
        #endregion

        #region Constructor
        public DtoGeneratorViewModel(
            CSharpParserService parserService,
            SettingsService settingsService,
            IConsoleWriter consoleWriter,
            FileGeneratorService fileGeneratorService,
            IMessenger messenger,
            IDtoGenerationService dtoService)
        {
            _consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            _fileGeneratorService = fileGeneratorService ?? throw new ArgumentNullException(nameof(fileGeneratorService));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _dtoService = dtoService ?? throw new ArgumentNullException(nameof(dtoService));
            _settings = new CRUDSettings(settingsService ?? throw new ArgumentNullException(nameof(settingsService)));
            _helper = new DtoGeneratorHelper(parserService ?? throw new ArgumentNullException(nameof(parserService)), _settings, consoleWriter);

            Entities = [];
            EntityProperties = [];
            MappingEntityProperties = [];

            _messenger.Register<ProjectChangedMessage>(this, (r, m) => SetCurrentProject(m.Project));
            _messenger.Register<SolutionClassesParsedMessage>(this, async (r, m) => await RefreshEntitiesListAsync());
        }
        #endregion

        #region Observable Properties
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFileGeneratorInit))]
        private bool _isProjectChosen;

        [ObservableProperty]
        private ObservableCollection<EntityInfo> _entities;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEntitySelected))]
        [NotifyPropertyChangedFor(nameof(IsAuditable))]
        private EntityInfo _entity;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGenerationEnabled))]
        private string _entityDomain;

        [ObservableProperty]
        private string _ancestorTeam;

        [ObservableProperty]
        private ObservableCollection<EntityProperty> _entityProperties;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMappingProperties))]
        [NotifyPropertyChangedFor(nameof(IsGenerationEnabled))]
        private ObservableCollection<MappingEntityProperty> _mappingEntityProperties;

        [ObservableProperty]
        private bool _wasAlreadyGenerated;

        [ObservableProperty]
        private bool _isTeam;

        [ObservableProperty]
        private bool _isVersioned;

        [ObservableProperty]
        private bool _isArchivable;

        [ObservableProperty]
        private bool _isFixable;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGenerationEnabled))]
        private string _selectedBaseKeyType;

        [ObservableProperty]
        private bool _useDedicatedAudit;

        [ObservableProperty]
        private bool _resetMappingColumnsWidthsTrigger;
        #endregion

        partial void OnEntityChanged(EntityInfo value)
        {
            OnEntitySelected();
        }

        #region Computed Properties
        public string ProjectDomainNamespace { get; private set; }
        public bool IsFileGeneratorInit => _fileGeneratorService?.IsInit == true;
        public bool IsEntitySelected => Entity != null;
        public bool HasMappingProperties => MappingEntityProperties?.Count > 0;
        public bool IsAuditable => Entity?.IsAuditable == true;
        public bool IsVisibleUseDedicatedAudit => Version.TryParse(_project?.FrameworkVersion, out var version) && version.Major >= 6;
        public IEnumerable<string> BaseKeyTypeItems => Constants.PrimitiveTypes;
        public IEnumerable<EntityProperty> AllEntityPropertiesRecursively => EntityProperties?.SelectMany(x => x.GetAllPropertiesRecursively()) ?? [];

        public bool IsGenerationEnabled =>
            ((HasMappingProperties && MappingEntityProperties.All(x => x.IsValid) && MappingEntityProperties.Count == MappingEntityProperties.DistinctBy(x => x.MappingName).Count()) || !HasMappingProperties)
            && !string.IsNullOrWhiteSpace(EntityDomain)
            && !string.IsNullOrWhiteSpace(SelectedBaseKeyType);
        #endregion

        #region Commands
        public ICommand RemoveMappingPropertyCommand => new RelayCommand<MappingEntityProperty>(RemoveMappingProperty);
        public ICommand MoveMappedPropertyCommand => new RelayCommand<MoveItemArgs>(x => MoveMappedProperty(x.OldIndex, x.NewIndex));
        public ICommand SetMappedPropertyIsParentCommand => new RelayCommand<MappingEntityProperty>(SetMappedPropertyIsParent);
        public ICommand SelectPropertiesCommand => new RelayCommand(SelectProperties);
        public ICommand RemoveAllMappingPropertiesCommand => new RelayCommand(RemoveAllMappingProperties);
        public ICommand ComputePropertiesValidityCommand => new RelayCommand(ComputePropertiesValidity);

        [RelayCommand]
        private async Task RefreshEntitiesListAsync()
        {
            if (_project is null)
                return;

            await _helper.ListEntitiesAsync(_project, this);
        }

        [RelayCommand]
        private async Task GenerateDtoAsync()
        {
            if (_project is null)
                return;

            _helper.UpdateHistoryFile(this);
            await _fileGeneratorService.GenerateDtoAsync(new FileGeneratorDtoContext
            {
                CompanyName = _project.CompanyName,
                ProjectName = _project.Name,
                DomainName = EntityDomain,
                EntityName = Entity.Name,
                EntityNamePlural = Entity.NamePluralized,
                BaseKeyType = SelectedBaseKeyType,
                Properties = [.. MappingEntityProperties],
                IsTeam = IsTeam,
                IsVersioned = IsVersioned,
                IsArchivable = IsArchivable,
                IsFixable = IsFixable,
                AncestorTeamName = AncestorTeam,
                HasAncestorTeam = !string.IsNullOrEmpty(AncestorTeam),
                GenerateBack = true,
                HasAudit = UseDedicatedAudit
            });
        }

        [RelayCommand]
        private void OnEntitySelected()
        {
            AncestorTeam = null;
            EntityDomain = null;

            if (Entity is not null)
            {
                var namespaceElements = Entity.Namespace.Split('.').ToList();
                var dtoNamespaceIndex = namespaceElements.FindIndex(x => x == "Domain");
                if (dtoNamespaceIndex != -1)
                {
                    EntityDomain = namespaceElements.ElementAt(dtoNamespaceIndex + 1);
                }
            }

            RefreshEntityPropertiesTreeView();
            RemoveAllMappingProperties();

            IsTeam = Entity?.IsTeam == true;
            IsVersioned = Entity?.IsVersioned == true;
            IsFixable = Entity?.IsFixable == true;
            IsArchivable = Entity?.IsArchivable == true;
            SelectedBaseKeyType = Entity?.BaseKeyType;
            UseDedicatedAudit = false;

            if (Entity is not null)
            {
                _helper.LoadFromHistory(Entity, this);
            }
        }
        #endregion

        #region Lifecycle Methods
        public void SetCurrentProject(Project project)
        {
            if (project == _project)
                return;

            IsProjectChosen = false;
            Clear();

            if (project is null)
                return;

            InitProject(project);
        }

        public void SetProject(Project project)
        {
            ProjectDomainNamespace = GetProjectDomainNamespace(project);
            IsProjectChosen = true;
            _project = project;
            OnPropertyChanged(nameof(IsVisibleUseDedicatedAudit));
        }

        private void InitProject(Project project)
        {
            _project = project;
            _helper.InitProject(project, this);
        }

        public void Clear()
        {
            Entities?.Clear();
            EntityDomain = null;
            AncestorTeam = null;
            _project = null;
        }
        #endregion

        #region Entity & Property Management
        public void SetEntities(List<EntityInfo> entities)
        {
            WasAlreadyGenerated = false;
            EntityDomain = null;
            AncestorTeam = null;

            ComputeBaseKeyType(entities);
            Entities = new(entities);
        }

        private void RefreshEntityPropertiesTreeView()
        {
            EntityProperties.Clear();
            if (Entity == null)
                return;

            foreach (var property in Entity.Properties.OrderBy(x => x.Name))
            {
                var propertyViewModel = new EntityProperty
                {
                    Name = property.Name,
                    Type = property.Type,
                    CompositeName = property.Name,
                    IsSelected = true,
                    ParentType = Entity.Name
                };
                FillEntityProperties(propertyViewModel, Entity.Name);
                EntityProperties.Add(propertyViewModel);
            }
        }

        private void FillEntityProperties(EntityProperty property, string rootPropertyType)
        {
            var propertyInfo = Entities.FirstOrDefault(e => e.Name == property.Type);
            if (propertyInfo == null)
                return;

            var childProperties = propertyInfo.Properties
                .Where(p => p.Type != property.ParentType && p.Type != rootPropertyType)
                .Select(p => new EntityProperty
                {
                    Name = p.Name,
                    Type = p.Type,
                    CompositeName = $"{property.CompositeName}.{p.Name}",
                    ParentType = property.Type
                });
            property.Properties.AddRange(childProperties);
            property.Properties.ForEach(p => FillEntityProperties(p, rootPropertyType));
        }
        #endregion

        #region Mapping Properties Management
        private void SelectProperties()
        {
            RefreshMappingProperties();
            ResetMappingColumnsWidthsTrigger = true;
            ComputePropertiesValidity();
        }

        public void RefreshMappingProperties()
        {
            var mappingEntityProperties = new List<MappingEntityProperty>(MappingEntityProperties);
            AddMappingProperties(EntityProperties, mappingEntityProperties);
            MappingEntityProperties = new(mappingEntityProperties);

            foreach (var mappingEntityProperty in MappingEntityProperties.Where(x => x.IsOptionCollection))
            {
                mappingEntityProperty.OptionRelationPropertyComposite = AllEntityPropertiesRecursively
                    .SingleOrDefault(x =>
                        x.ParentType == mappingEntityProperty.ParentEntityType
                        && x.Type.Equals($"ICollection<{mappingEntityProperty.OptionRelationType}>")
                    )?.CompositeName;

                if (string.IsNullOrWhiteSpace(mappingEntityProperty.OptionRelationPropertyComposite))
                {
                    _consoleWriter.AddMessageLine($"ERROR: unable to find matching property of type ICollection<{mappingEntityProperty.OptionRelationType}> in type {mappingEntityProperty.ParentEntityType} to map {mappingEntityProperty.EntityCompositeName}", "red");
                }
            }

            OnPropertyChanged(nameof(HasMappingProperties));
            OnPropertyChanged(nameof(IsGenerationEnabled));
        }

        private void AddMappingProperties(IEnumerable<EntityProperty> entityProperties, List<MappingEntityProperty> mappingEntityProperties)
        {
            foreach (var selectedEntityProperty in entityProperties)
            {
                if (selectedEntityProperty.IsSelected && !mappingEntityProperties.Any(x => x.EntityCompositeName == selectedEntityProperty.CompositeName))
                {
                    var mappingEntityProperty = CreateMappingEntityProperty(selectedEntityProperty, entityProperties);
                    if (mappingEntityProperty != null)
                    {
                        mappingEntityProperties.Add(mappingEntityProperty);
                    }
                }

                AddMappingProperties(selectedEntityProperty.Properties, mappingEntityProperties);
            }
        }

        private MappingEntityProperty CreateMappingEntityProperty(EntityProperty selectedEntityProperty, IEnumerable<EntityProperty> entityProperties)
        {
            var mappingEntityProperty = new MappingEntityProperty
            {
                EntityCompositeName = selectedEntityProperty.CompositeName,
                EntityType = selectedEntityProperty.Type,
                ParentEntityType = selectedEntityProperty.ParentType,
                MappingName = selectedEntityProperty.CompositeName.Replace(".", string.Empty),
                MappingType = ComputeMappingType(selectedEntityProperty)
            };

            // Handle date types
            if (_biaDtoFieldDateTypesByPropertyType.TryGetValue(mappingEntityProperty.MappingType.Replace("?", string.Empty), out List<string> biaDtoFieldDateTypes))
            {
                var mappingDateTypes = new List<string>();
                if (mappingEntityProperty.MappingType == "string")
                {
                    mappingDateTypes.Add(string.Empty);
                }
                mappingDateTypes.AddRange(biaDtoFieldDateTypes);
                mappingEntityProperty.MappingDateTypes = mappingDateTypes;
                mappingEntityProperty.MappingDateType = mappingEntityProperty.MappingDateTypes.FirstOrDefault();
            }

            // Handle Option and OptionCollection types
            if (mappingEntityProperty.IsOption || mappingEntityProperty.IsOptionCollection)
            {
                if (!ConfigureOptionMapping(mappingEntityProperty, selectedEntityProperty, entityProperties))
                {
                    return null;
                }
            }

            return mappingEntityProperty;
        }

        private bool ConfigureOptionMapping(MappingEntityProperty mappingEntityProperty, EntityProperty selectedEntityProperty, IEnumerable<EntityProperty> entityProperties)
        {
            mappingEntityProperty.OptionType = ExtractOptionType(selectedEntityProperty.Type);
            var optionEntity = Entities.FirstOrDefault(x => x.Name == mappingEntityProperty.OptionType);
            
            if (optionEntity == null)
                return true;

            mappingEntityProperty.OptionDisplayProperties.AddRange(optionEntity.Properties.Select(x => x.Name));
            mappingEntityProperty.OptionDisplayProperty = mappingEntityProperty.OptionDisplayProperties
                .FirstOrDefault(x => !x.Equals("id", StringComparison.InvariantCultureIgnoreCase));

            if (mappingEntityProperty.IsOption)
            {
                return ConfigureOptionTypeMapping(mappingEntityProperty, optionEntity, selectedEntityProperty, entityProperties);
            }

            if (mappingEntityProperty.IsOptionCollection)
            {
                return ConfigureOptionCollectionMapping(mappingEntityProperty, optionEntity, selectedEntityProperty);
            }

            return true;
        }

        private bool ConfigureOptionTypeMapping(MappingEntityProperty mappingEntityProperty, EntityInfo optionEntity, EntityProperty selectedEntityProperty, IEnumerable<EntityProperty> entityProperties)
        {
            var optionBaseKeyType = optionEntity.BaseKeyType;
            if (string.IsNullOrEmpty(optionBaseKeyType))
            {
                _consoleWriter.AddMessageLine($"Unable to find base key type of related entity {optionEntity.Name}, the mapping for this property has been ignored.", "orange");
                return false;
            }

            mappingEntityProperty.OptionIdProperties.AddRange(optionEntity.Properties
                .Where(x => x.Type.Equals(optionBaseKeyType, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => x.Name));
            mappingEntityProperty.OptionIdProperty = mappingEntityProperty.OptionIdProperties
                .FirstOrDefault(x => x.Equals("id", StringComparison.InvariantCultureIgnoreCase));

            mappingEntityProperty.OptionEntityIdProperties.AddRange(
                entityProperties
                    .Where(x => x.Type.Replace("?", string.Empty).Equals(optionBaseKeyType, StringComparison.InvariantCultureIgnoreCase)
                        && !x.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                    .Select(x => x.Name));

            if (!mappingEntityProperty.OptionEntityIdProperties.Any())
            {
                _consoleWriter.AddMessageLine($"Unable to find {optionBaseKeyType} ID property related to {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                selectedEntityProperty.IsSelected = false;
                return false;
            }

            var optionEntityIdPropertyName = $"{mappingEntityProperty.EntityCompositeName.Split(".").Last()}Id";
            var automaticOptionEntityIdProperty = mappingEntityProperty.OptionEntityIdProperties.FirstOrDefault(x => x.Equals(optionEntityIdPropertyName));
            if (!string.IsNullOrWhiteSpace(automaticOptionEntityIdProperty))
            {
                mappingEntityProperty.OptionEntityIdProperty = automaticOptionEntityIdProperty;
            }

            return true;
        }

        private bool ConfigureOptionCollectionMapping(MappingEntityProperty mappingEntityProperty, EntityInfo optionEntity, EntityProperty selectedEntityProperty)
        {
            var optionRelationFirstType = selectedEntityProperty.ParentType;
            var relationTypeClassNames = new List<string> { optionRelationFirstType, mappingEntityProperty.OptionType };

            var entityInfo = Entities.SingleOrDefault(x =>
                string.IsNullOrEmpty(x.BaseKeyType)
                && relationTypeClassNames.All(y => x.Properties.Select(p => p.Type).Contains(y)));

            if (entityInfo is null)
            {
                _consoleWriter.AddMessageLine($"Unable to find relation's entity between types {optionRelationFirstType} and {mappingEntityProperty.OptionType} to map {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                selectedEntityProperty.IsSelected = false;
                return false;
            }

            mappingEntityProperty.OptionRelationType = entityInfo.Name;

            var optionRelationFirstIdPropertyName = optionRelationFirstType + "Id";
            mappingEntityProperty.OptionRelationFirstIdProperty = entityInfo.Properties.SingleOrDefault(x => x.Name.Equals(optionRelationFirstIdPropertyName))?.Name;
            if (string.IsNullOrWhiteSpace(mappingEntityProperty.OptionRelationFirstIdProperty))
            {
                _consoleWriter.AddMessageLine($"Unable to find matching relation property {optionRelationFirstIdPropertyName} in the entity {entityInfo.Name} to map {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                selectedEntityProperty.IsSelected = false;
                return false;
            }

            var optionRelationSecondIdPropertyName = mappingEntityProperty.OptionType + "Id";
            mappingEntityProperty.OptionRelationSecondIdProperty = entityInfo.Properties.SingleOrDefault(x => x.Name.Equals(optionRelationSecondIdPropertyName))?.Name;
            if (string.IsNullOrWhiteSpace(mappingEntityProperty.OptionRelationSecondIdProperty))
            {
                _consoleWriter.AddMessageLine($"Unable to find matching relation property {optionRelationSecondIdPropertyName} in the entity {entityInfo.Name} to map {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                selectedEntityProperty.IsSelected = false;
                return false;
            }

            mappingEntityProperty.OptionIdProperties.AddRange(optionEntity.Properties.Select(x => x.Name));
            mappingEntityProperty.OptionIdProperty = mappingEntityProperty.OptionIdProperties.FirstOrDefault(x => x.Equals("id", StringComparison.InvariantCultureIgnoreCase));

            return true;
        }

        private string ComputeMappingType(EntityProperty entityProperty)
        {
            if (_optionCollectionsMappingTypes.Any(x => entityProperty.Type.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                return Constants.BiaClassName.CollectionOptionDto;
            }

            if (_standardMappingTypes.Any(x => entityProperty.Type.Replace("?", string.Empty).Equals(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                return entityProperty.Type;
            }

            if (_specialTypeToRemap.Any(x => x.Key.Equals(entityProperty.Type)))
            {
                return _specialTypeToRemap.First(x => x.Key.Equals(entityProperty.Type)).Value;
            }

            return Constants.BiaClassName.OptionDto;
        }

        private static string ExtractOptionType(string optionType)
        {
            if (!optionType.Contains('<'))
                return optionType;

            var regex = new Regex(@"<\s*(\w+)\s*>");
            return regex.Match(optionType).Groups[1].Value;
        }

        private void RemoveMappingProperty(MappingEntityProperty mappingEntityProperty)
        {
            MappingEntityProperties.Remove(mappingEntityProperty);
            ComputePropertiesValidity();
            OnPropertyChanged(nameof(HasMappingProperties));
            OnPropertyChanged(nameof(IsGenerationEnabled));
        }

        private void SetMappedPropertyIsParent(MappingEntityProperty mappingEntityProperty)
        {
            var newValue = mappingEntityProperty.IsParent;
            foreach (var item in MappingEntityProperties)
            {
                item.IsParent = false;
            }
            mappingEntityProperty.IsParent = newValue;
        }

        public void RemoveAllMappingProperties()
        {
            MappingEntityProperties?.Clear();
            OnPropertyChanged(nameof(HasMappingProperties));
            OnPropertyChanged(nameof(IsGenerationEnabled));
        }

        public void ComputePropertiesValidity()
        {
            const string Error_DuplicateMappingName = "Duplicate property name";

            foreach (var mappingEntityProperty in MappingEntityProperties)
            {
                mappingEntityProperty.MappingNameError = null;
                var duplicateMappingNameProperties = MappingEntityProperties.Where(x => x != mappingEntityProperty && x.MappingName == mappingEntityProperty.MappingName);
                if (duplicateMappingNameProperties.Any())
                {
                    mappingEntityProperty.MappingNameError = Error_DuplicateMappingName;
                    foreach (var duplicateMappingProperty in duplicateMappingNameProperties)
                    {
                        duplicateMappingProperty.MappingNameError = Error_DuplicateMappingName;
                    }
                }
            }

            OnPropertyChanged(nameof(IsGenerationEnabled));
        }

        private void MoveMappedProperty(int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex || oldIndex < 0 || newIndex < 0 || oldIndex >= MappingEntityProperties.Count || newIndex >= MappingEntityProperties.Count)
                return;

            MappingEntityProperties.Move(oldIndex, newIndex);
        }
        #endregion

        #region Static Helpers
        private static void ComputeBaseKeyType(List<EntityInfo> entities)
        {
            foreach (var entity in entities)
            {
                if (!string.IsNullOrWhiteSpace(entity.BaseKeyType))
                    continue;

                var baseEntityInfo = GetBaseEntityInfoWithNonEmptyBaseKeyType(entity, entities);
                if (baseEntityInfo != null)
                {
                    entity.BaseKeyType = baseEntityInfo.BaseKeyType;
                }
            }
        }

        private static EntityInfo GetBaseEntityInfoWithNonEmptyBaseKeyType(EntityInfo entityInfo, List<EntityInfo> entities)
        {
            var baseTypeEntityInfo = entities.FirstOrDefault(e => entityInfo.BaseList.Contains(e.Name));
            if (baseTypeEntityInfo != null)
            {
                return string.IsNullOrWhiteSpace(baseTypeEntityInfo.BaseKeyType)
                    ? GetBaseEntityInfoWithNonEmptyBaseKeyType(baseTypeEntityInfo, entities)
                    : baseTypeEntityInfo;
            }
            return null;
        }

        private static string GetProjectDomainNamespace(Project project)
        {
            if (project == null)
                return string.Empty;

            return string.Join(".", project.CompanyName, project.Name, "Domain");
        }
        #endregion
    }

    /// <summary>
    /// Represents a property of an entity for selection in the UI.
    /// </summary>
    public class EntityProperty : ObservableObject
    {
        public string Name { get; set; }
        public string Type { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string CompositeName { get; set; }
        public List<EntityProperty> Properties { get; set; } = [];
        public string ParentType { get; set; }

        public List<EntityProperty> GetAllPropertiesRecursively()
        {
            var allProperties = new List<EntityProperty> { this };
            allProperties.AddRange(Properties.SelectMany(x => x.GetAllPropertiesRecursively()));
            return allProperties;
        }
    }
}
