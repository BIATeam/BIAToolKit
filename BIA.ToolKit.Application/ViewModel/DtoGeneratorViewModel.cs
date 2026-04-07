namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public partial class DtoGeneratorViewModel : ObservableObject, IDisposable,
        IRecipient<ProjectChangedMessage>,
        IRecipient<SolutionClassesParsedMessage>
    {
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly CRUDSettings settings;
        private readonly CSharpParserService parserService;

        private Project project;
        private string dtoGenerationHistoryFile;
        private DtoGenerationHistory generationHistory = new();
        private DtoGeneration generation;
        private bool disposed;

        /// <summary>
        /// Raised after mapping properties are refreshed so the View can reset grid column widths.
        /// </summary>
        public event Action OnMappingRefreshed;

        /// <summary>
        /// Constructor with dependency injection.
        /// </summary>
        public DtoGeneratorViewModel(
            CSharpParserService parserService,
            FileGeneratorService fileGeneratorService,
            IConsoleWriter consoleWriter,
            SettingsService settingsService)
        {
            this.parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            this.fileGeneratorService = fileGeneratorService ?? throw new ArgumentNullException(nameof(fileGeneratorService));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.settings = new CRUDSettings(settingsService ?? throw new ArgumentNullException(nameof(settingsService)));

            // Subscribe to messenger events
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        public void Receive(ProjectChangedMessage message) => OnProjectChanged(message.Project);
        public void Receive(SolutionClassesParsedMessage message) => OnSolutionClassesParsed();

        private readonly List<string> optionCollectionsMappingTypes =
        [
            "icollection",
            "list"
        ];
        private readonly List<string> standardMappingTypes =
        [
            "bool",
            "byte",
            "sbyte",
            "char",
            "decimal",
            "double",
            "float",
            "int",
            "uint",
            "long",
            "ulong",
            "short",
            "ushort",
            "string",
            "DateTime",
            "DateOnly",
            "TimeOnly",
            "byte[]",
            "Guid"
        ];
        private readonly Dictionary<string, string> specialdTypeToRemap = new()
        {
            { "TimeSpan", "string" },
            { "TimeSpan?", "string" },
        };

        private readonly Dictionary<string, List<string>> biaDtoFieldDateTypesByPropertyType = new()
        {
            { "TimeSpan", new List<string> { "time" } },
            { "DateTime", new List<string> { "datetime", "date", "time" } },
        };

        private bool isProjectChosen;
        public bool IsProjectChosen
        {
            get => isProjectChosen;
            set
            {
                isProjectChosen = value;
                OnPropertyChanged(nameof(IsProjectChosen));
                OnPropertyChanged(nameof(IsFileGeneratorInit));
            }
        }

        public bool IsFileGeneratorInit => fileGeneratorService?.IsInit == true;

        private ObservableCollection<EntityInfo> entities = [];
        public ObservableCollection<EntityInfo> Entities
        {
            get => entities;
            set
            {
                entities = value;
                OnPropertyChanged(nameof(Entities));
            }
        }

        private EntityInfo entity;
        public EntityInfo Entity
        {
            get => entity;
            set
            {
                entity = value;
                OnPropertyChanged(nameof(Entity));
                AncestorTeam = null;
                EntityDomain = null;

                if (entity is not null)
                {
                    List<string> namespaceElements = [.. entity.Namespace.Split('.')];
                    int dtoNamespaceIndex = namespaceElements.FindIndex(x => x == "Domain");
                    if (dtoNamespaceIndex != -1)
                    {
                        EntityDomain = namespaceElements.ElementAt(dtoNamespaceIndex + 1);
                    }
                }

                OnPropertyChanged(nameof(IsEntitySelected));
                OnPropertyChanged(nameof(IsAuditable));

                RefreshEntityPropertiesTreeView();
                RemoveAllMappingProperties();

                IsTeam = entity?.IsTeam == true;
                IsVersioned = entity?.IsVersioned == true;
                IsFixable = entity?.IsFixable == true;
                IsArchivable = entity?.IsArchivable == true;
                SelectedBaseKeyType = entity?.BaseKeyType;
                UseDedicatedAudit = false;

                OnEntitySelected();
            }
        }

        private string entityDomain;
        public string EntityDomain
        {
            get => entityDomain;
            set
            {
                entityDomain = value;
                OnPropertyChanged(nameof(EntityDomain));
                OnPropertyChanged(nameof(IsGenerationEnabled));
            }
        }

        private string ancestorTeam;

        public string AncestorTeam
        {
            get => ancestorTeam;
            set
            {
                ancestorTeam = value;
                OnPropertyChanged(nameof(AncestorTeam));
            }
        }

        private ObservableCollection<EntityProperty> entityProperties = [];
        public ObservableCollection<EntityProperty> EntityProperties
        {
            get => entityProperties;
            set { entityProperties = value; }
        }

        public IEnumerable<EntityProperty> AllEntityPropertiesRecursively => EntityProperties.SelectMany(x => x.GetAllPropertiesRecursively());

        private ObservableCollection<MappingEntityProperty> mappingEntityProperties = [];
        public ObservableCollection<MappingEntityProperty> MappingEntityProperties
        {
            get => mappingEntityProperties;
            set
            {
                mappingEntityProperties = value;
                OnPropertyChanged(nameof(MappingEntityProperties));
                OnPropertyChanged(nameof(IsGenerationEnabled));
            }
        }

        private bool wasAlreadyGenerated;
        public bool WasAlreadyGenerated
        {
            get => wasAlreadyGenerated;
            set
            {
                wasAlreadyGenerated = value;
                OnPropertyChanged(nameof(WasAlreadyGenerated));
            }
        }

        private bool _isTeam;
        public bool IsTeam
        {
            get => _isTeam;
            set
            {
                _isTeam = value;
                OnPropertyChanged(nameof(IsTeam));
            }
        }

        private bool _isVersioned;
        public bool IsVersioned
        {
            get => _isVersioned;
            set
            {
                _isVersioned = value;
                OnPropertyChanged(nameof(IsVersioned));
            }
        }

        private bool _isArchivable;
        public bool IsArchivable
        {
            get => _isArchivable;
            set
            {
                _isArchivable = value;
                OnPropertyChanged(nameof(IsArchivable));
            }
        }

        private bool _isFixable;
        public bool IsFixable
        {
            get => _isFixable;
            set
            {
                _isFixable = value;
                OnPropertyChanged(nameof(IsFixable));
            }
        }

        public static IEnumerable<string> BaseKeyTypeItems => Constants.PrimitiveTypes;
        private string selectedBaseKeyType;

        public string SelectedBaseKeyType
        {
            get { return selectedBaseKeyType; }
            set
            {
                selectedBaseKeyType = value;
                OnPropertyChanged(nameof(SelectedBaseKeyType));
                OnPropertyChanged(nameof(IsGenerationEnabled));
            }
        }

        private bool useDedicatedAudit;

        public bool UseDedicatedAudit
        {
            get { return useDedicatedAudit; }
            set
            {
                useDedicatedAudit = value;
                OnPropertyChanged(nameof(UseDedicatedAudit));
            }
        }

        public string ProjectDomainNamespace { get; private set; }
        public bool IsEntitySelected => Entity != null;
        public bool HasMappingProperties => MappingEntityProperties.Count > 0;
        public bool IsGenerationEnabled =>
            ((HasMappingProperties && MappingEntityProperties.All(x => x.IsValid) && MappingEntityProperties.Count == MappingEntityProperties.DistinctBy(x => x.MappingName).Count()) || !HasMappingProperties)
            && !string.IsNullOrWhiteSpace(EntityDomain)
            && !string.IsNullOrWhiteSpace(SelectedBaseKeyType);
        public bool IsAuditable => Entity?.IsAuditable == true;
        public bool IsVisibleUseDedicatedAudit => Version.TryParse(project?.FrameworkVersion, out Version version) && version.Major >= 6;

        public void SetProject(Project project)
        {
            ProjectDomainNamespace = GetProjectDomainNamespace(project);
            IsProjectChosen = true;

            this.project = project;
            OnPropertyChanged(nameof(IsVisibleUseDedicatedAudit));
            InitHistoryFile(project);
        }

        private void OnProjectChanged(Project project)
        {
            SetCurrentProject(project);
        }

        private void OnSolutionClassesParsed()
        {
            // Reload entities when solution classes are parsed
            LoadEntities(parserService);
        }

        public void LoadEntities(CSharpParserService parserService)
        {
            if (project is null)
                return;

            var domainEntities = parserService.GetDomainEntities(project).ToList();
            SetEntities(domainEntities);
        }

        public void SetCurrentProject(Project project)
        {
            if (project == this.project)
                return;

            IsProjectChosen = false;
            Clear();

            if (project is null)
                return;

            SetProject(project);
        }

        private void InitHistoryFile(Project project)
        {
            dtoGenerationHistoryFile = Path.Combine(project.Folder, Constants.FolderBia, settings.DtoGenerationHistoryFileName);
            generationHistory = File.Exists(dtoGenerationHistoryFile)
                ? CommonTools.DeserializeJsonFile<DtoGenerationHistory>(dtoGenerationHistoryFile)
                : new DtoGenerationHistory();
        }

        public void OnEntitySelected()
        {
            if (Entity is null)
                return;

            generation = generationHistory.Generations.FirstOrDefault(g => g.EntityName.Equals(Entity.Name) && g.EntityNamespace.Equals(Entity.Namespace));
            if (generation is null)
            {
                WasAlreadyGenerated = false;
                return;
            }

            WasAlreadyGenerated = true;
            EntityDomain = generation.Domain;
            AncestorTeam = generation.AncestorTeam;
            IsTeam = generation.IsTeam;
            IsVersioned = generation.IsVersioned;
            IsFixable = generation.IsFixable;
            IsArchivable = generation.IsArchivable;
            SelectedBaseKeyType = generation.EntityBaseKeyType;
            UseDedicatedAudit = generation.UseDedicatedAudit;

            var allEntityProperties = AllEntityPropertiesRecursively.ToList();
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

            RefreshMappingProperties();

            foreach (var property in generation.PropertyMappings)
            {
                var mappingProperty = MappingEntityProperties.FirstOrDefault(x => x.EntityCompositeName == property.EntityPropertyCompositeName);
                if (mappingProperty is null)
                    continue;

                mappingProperty.MappingName = property.MappingName;
                mappingProperty.MappingDateType = property.DateType;
                mappingProperty.IsRequired = property.IsRequired;
                mappingProperty.OptionIdProperty = property.OptionMappingIdProperty;
                mappingProperty.OptionDisplayProperty = property.OptionMappingDisplayProperty;
                mappingProperty.OptionEntityIdProperty = property.OptionMappingEntityIdProperty;
                mappingProperty.IsParent = property.IsParent;
            }

            ComputePropertiesValidity();
        }

        [RelayCommand]
        private void SelectProperties()
        {
            RefreshMappingProperties();
            OnMappingRefreshed?.Invoke();
            ComputePropertiesValidity();
        }

        [RelayCommand]
        private void Generate()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async () =>
            {
                UpdateHistoryFile();
                await fileGeneratorService.GenerateDtoAsync(new FileGeneratorDtoContext
                {
                    CompanyName = project.CompanyName,
                    ProjectName = project.Name,
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
            }));
        }

        private void UpdateHistoryFile()
        {
            var isNewGeneration = generation is null;
            generation ??= new DtoGeneration();

            generation.DateTime = DateTime.Now;
            generation.EntityName = Entity.Name;
            generation.EntityNamespace = Entity.Namespace;
            generation.Domain = EntityDomain;
            generation.AncestorTeam = AncestorTeam;
            generation.IsTeam = IsTeam;
            generation.IsVersioned = IsVersioned;
            generation.IsArchivable = IsArchivable;
            generation.IsFixable = IsFixable;
            generation.EntityBaseKeyType = SelectedBaseKeyType;
            generation.UseDedicatedAudit = UseDedicatedAudit;
            generation.PropertyMappings.Clear();

            foreach (var property in MappingEntityProperties)
            {
                generation.PropertyMappings.Add(new DtoGenerationPropertyMapping
                {
                    DateType = property.MappingDateType,
                    EntityPropertyCompositeName = property.EntityCompositeName,
                    IsRequired = property.IsRequired,
                    MappingName = property.MappingName,
                    OptionMappingDisplayProperty = property.OptionDisplayProperty,
                    OptionMappingEntityIdProperty = property.OptionEntityIdProperty,
                    OptionMappingIdProperty = property.OptionIdProperty,
                    IsParent = property.IsParent
                });
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

        [RelayCommand]
        public void RemoveAllMappingProperties()
        {
            MappingEntityProperties.Clear();

            OnPropertyChanged(nameof(HasMappingProperties));
            OnPropertyChanged(nameof(IsGenerationEnabled));
        }

        public void SetEntities(List<EntityInfo> entities)
        {
            WasAlreadyGenerated = false;
            EntityDomain = null;
            AncestorTeam = null;

            ComputeBaseKeyType(entities);

            Entities = new(entities);
        }

        private static void ComputeBaseKeyType(List<EntityInfo> entities)
        {
            foreach (EntityInfo entity in entities)
            {
                if (!string.IsNullOrWhiteSpace(entity.BaseKeyType))
                    continue;

                EntityInfo baseEntityInfo = GetBaseEntityInfoWithNonEmptyBaseKeyType(entity, entities);
                if (baseEntityInfo != null)
                {
                    entity.BaseKeyType = baseEntityInfo.BaseKeyType;
                }
            }
        }

        private static EntityInfo GetBaseEntityInfoWithNonEmptyBaseKeyType(EntityInfo entityInfo, List<EntityInfo> entities)
        {
            EntityInfo baseTypeEntityInfo = entities.FirstOrDefault(e => entityInfo.BaseList.Contains(e.Name));
            if (baseTypeEntityInfo != null)
            {
                return string.IsNullOrWhiteSpace(baseTypeEntityInfo.BaseKeyType) ?
                    GetBaseEntityInfoWithNonEmptyBaseKeyType(baseTypeEntityInfo, entities) :
                    baseTypeEntityInfo;
            }
            return null;
        }

        private static string GetProjectDomainNamespace(Project project)
        {
            if (project == null)
                return string.Empty;

            return string.Join(".", project.CompanyName, project.Name, "Domain");
        }

        private void RefreshEntityPropertiesTreeView()
        {
            EntityProperties.Clear();
            if (Entity == null)
                return;

            foreach (Domain.ModifyProject.DtoGenerator.PropertyInfo property in Entity.Properties.OrderBy(x => x.Name))
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
            EntityInfo propertyInfo = Entities.FirstOrDefault(e => e.Name == property.Type);
            if (propertyInfo == null)
            {
                return;
            }

            IEnumerable<EntityProperty> childProperties = propertyInfo.Properties
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

        public void RefreshMappingProperties()
        {
            var mappingEntityProperties = new List<MappingEntityProperty>(MappingEntityProperties);
            AddMappingProperties(EntityProperties, mappingEntityProperties);
            MappingEntityProperties = new(mappingEntityProperties);

            foreach (MappingEntityProperty mappingEntityProperty in MappingEntityProperties.Where(x => x.IsOptionCollection))
            {
                mappingEntityProperty.OptionRelationPropertyComposite = AllEntityPropertiesRecursively
                    .SingleOrDefault(x =>
                        x.ParentType == mappingEntityProperty.ParentEntityType
                        && x.Type.Equals($"ICollection<{mappingEntityProperty.OptionRelationType}>")
                    )?.CompositeName;

                if (string.IsNullOrWhiteSpace(mappingEntityProperty.OptionRelationPropertyComposite))
                {
                    consoleWriter.AddMessageLine($"ERROR: unable to find matching property of type ICollection<{mappingEntityProperty.OptionRelationType}> in type {mappingEntityProperty.ParentEntityType} to map {mappingEntityProperty.EntityCompositeName}", "red");
                }
            }

            OnPropertyChanged(nameof(HasMappingProperties));
            OnPropertyChanged(nameof(IsGenerationEnabled));
        }

        private void AddMappingProperties(IEnumerable<EntityProperty> entityProperties, List<MappingEntityProperty> mappingEntityProperties)
        {
            foreach (EntityProperty selectedEntityProperty in entityProperties)
            {
                if (selectedEntityProperty.IsSelected && !mappingEntityProperties.Any(x => x.EntityCompositeName == selectedEntityProperty.CompositeName))
                {
                    var mappingEntityProperty = new MappingEntityProperty
                    {
                        EntityCompositeName = selectedEntityProperty.CompositeName,
                        EntityType = selectedEntityProperty.Type,
                        ParentEntityType = selectedEntityProperty.ParentType,
                        MappingName = selectedEntityProperty.CompositeName.Replace(".", string.Empty),
                        MappingType = ComputeMappingType(selectedEntityProperty)
                    };

                    if (biaDtoFieldDateTypesByPropertyType.TryGetValue(mappingEntityProperty.MappingType.Replace("?", string.Empty), out List<string> biaDtoFieldDateTypes))
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

                    if (mappingEntityProperty.IsOption || mappingEntityProperty.IsOptionCollection)
                    {
                        mappingEntityProperty.OptionType = ExtractOptionType(selectedEntityProperty.Type);
                        EntityInfo optionEntity = Entities.FirstOrDefault(x => x.Name == mappingEntityProperty.OptionType);
                        if (optionEntity != null)
                        {
                            mappingEntityProperty.OptionDisplayProperties.AddRange(optionEntity.Properties.Select(x => x.Name));
                            mappingEntityProperty.OptionDisplayProperty = mappingEntityProperty.OptionDisplayProperties.Where(x => !x.Equals("id", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                            if (mappingEntityProperty.IsOption)
                            {
                                string optionBaseKeyType = optionEntity.BaseKeyType;
                                if (string.IsNullOrEmpty(optionBaseKeyType))
                                {
                                    consoleWriter.AddMessageLine($"Unable to find base key type of related entity {optionEntity.Name}, the mapping for this property has been ignored.", "orange");
                                    continue;
                                }

                                mappingEntityProperty.OptionIdProperties.AddRange(optionEntity.Properties.Where(x => x.Type.Equals(optionBaseKeyType, StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Name));
                                mappingEntityProperty.OptionIdProperty = mappingEntityProperty.OptionIdProperties.FirstOrDefault(x => x.Equals("id", StringComparison.InvariantCultureIgnoreCase));

                                mappingEntityProperty.OptionEntityIdProperties.AddRange(
                                    entityProperties
                                    .Where(x => x.Type.Replace("?", string.Empty).Equals(optionBaseKeyType, StringComparison.InvariantCultureIgnoreCase) && !x.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                                    .Select(x => x.Name));

                                if (mappingEntityProperty.OptionEntityIdProperties.Count == 0)
                                {
                                    consoleWriter.AddMessageLine($"Unable to find {optionBaseKeyType} ID property related to {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                                    selectedEntityProperty.IsSelected = false;
                                    continue;
                                }

                                string optionEntityIdPropertyName = $"{mappingEntityProperty.EntityCompositeName.Split(".").Last()}Id";
                                string automaticOptionEntityIdProperty = mappingEntityProperty.OptionEntityIdProperties.FirstOrDefault(x => x.Equals(optionEntityIdPropertyName));
                                if (!string.IsNullOrWhiteSpace(automaticOptionEntityIdProperty))
                                {
                                    mappingEntityProperty.OptionEntityIdProperty = automaticOptionEntityIdProperty;
                                }
                            }

                            if (mappingEntityProperty.IsOptionCollection)
                            {
                                string optionRelationFirstType = selectedEntityProperty.ParentType;
                                string optionRelationSecondType = mappingEntityProperty.OptionType;

                                var relationTypeClassNames = new List<string> { optionRelationFirstType, mappingEntityProperty.OptionType };
                                EntityInfo entityInfo = Entities.SingleOrDefault(x =>
                                    string.IsNullOrEmpty(x.BaseKeyType)
                                    && relationTypeClassNames.All(y => x.Properties.Select(x => x.Type).Contains(y)));

                                if (entityInfo is null)
                                {
                                    consoleWriter.AddMessageLine($"Unable to find relation's entity between types {optionRelationFirstType} and {optionRelationSecondType} to map {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                                    selectedEntityProperty.IsSelected = false;
                                    continue;
                                }

                                mappingEntityProperty.OptionRelationType = entityInfo.Name;

                                string optionRelationFirstIdPropertyName = optionRelationFirstType + "Id";
                                mappingEntityProperty.OptionRelationFirstIdProperty = entityInfo.Properties.SingleOrDefault(x => x.Name.Equals(optionRelationFirstIdPropertyName))?.Name;
                                if (string.IsNullOrWhiteSpace(mappingEntityProperty.OptionRelationFirstIdProperty))
                                {
                                    consoleWriter.AddMessageLine($"Unable to find matching relation property {optionRelationFirstIdPropertyName} in the entity {entityInfo.Name} to map {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                                    selectedEntityProperty.IsSelected = false;
                                    continue;
                                }

                                string optionRelationSecondIdPropertyName = optionRelationSecondType + "Id";
                                mappingEntityProperty.OptionRelationSecondIdProperty = entityInfo.Properties.SingleOrDefault(x => x.Name.Equals(optionRelationSecondIdPropertyName))?.Name;
                                if (string.IsNullOrWhiteSpace(mappingEntityProperty.OptionRelationSecondIdProperty))
                                {
                                    consoleWriter.AddMessageLine($"Unable to find matching relation property {optionRelationSecondIdPropertyName} in the entity {entityInfo.Name} to map {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                                    selectedEntityProperty.IsSelected = false;
                                    continue;
                                }

                                mappingEntityProperty.OptionIdProperties.AddRange(optionEntity.Properties.Select(x => x.Name));
                                mappingEntityProperty.OptionIdProperty = mappingEntityProperty.OptionIdProperties.FirstOrDefault(x => x.Equals("id", StringComparison.InvariantCultureIgnoreCase));
                            }
                        }
                    }

                    mappingEntityProperties.Add(mappingEntityProperty);
                }

                AddMappingProperties(selectedEntityProperty.Properties, mappingEntityProperties);
            }
        }

        private string ComputeMappingType(EntityProperty entityProperty)
        {
            if (optionCollectionsMappingTypes.Any(x => entityProperty.Type.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                return Constants.BiaClassName.CollectionOptionDto;
            }

            if (standardMappingTypes.Any(x => entityProperty.Type.Replace("?", string.Empty).Equals(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                return entityProperty.Type;
            }

            if (specialdTypeToRemap.Any(x => x.Key.Equals(entityProperty.Type)))
            {
                return specialdTypeToRemap.First(x => x.Key.Equals(entityProperty.Type)).Value;
            }

            return Constants.BiaClassName.OptionDto;
        }

        private static string ExtractOptionType(string optionType)
        {
            if (!optionType.Contains('<'))
                return optionType;

            Regex regex = MyRegex();
            return regex.Match(optionType).Groups[1].Value;
        }

        [RelayCommand]
        private void RemoveMappingProperty(MappingEntityProperty mappingEntityProperty)
        {
            MappingEntityProperties.Remove(mappingEntityProperty);

            ComputePropertiesValidity();

            OnPropertyChanged(nameof(HasMappingProperties));
            OnPropertyChanged(nameof(IsGenerationEnabled));
        }

        [RelayCommand]
        private void SetMappedPropertyIsParent(MappingEntityProperty mappingEntityProperty)
        {
            bool newValue = mappingEntityProperty.IsParent;
            foreach (MappingEntityProperty item in MappingEntityProperties)
            {
                item.IsParent = false;
            }
            mappingEntityProperty.IsParent = newValue;
        }

        public void ComputePropertiesValidity()
        {
            const string Error_DuplicateMappingName = "Duplicate property name";

            foreach (MappingEntityProperty mappingEntityproperty in MappingEntityProperties)
            {
                mappingEntityproperty.MappingNameError = null;
                IEnumerable<MappingEntityProperty> duplicateMappingNameProperties = MappingEntityProperties.Where(x => x != mappingEntityproperty && x.MappingName == mappingEntityproperty.MappingName);
                if (duplicateMappingNameProperties.Any())
                {
                    mappingEntityproperty.MappingNameError = Error_DuplicateMappingName;
                    foreach (MappingEntityProperty duplicateMappingProperty in duplicateMappingNameProperties)
                    {
                        duplicateMappingProperty.MappingNameError = Error_DuplicateMappingName;
                    }
                }
            }

            OnPropertyChanged(nameof(IsGenerationEnabled));
        }

        public void Clear()
        {
            Entities.Clear();
            EntityDomain = null;
            AncestorTeam = null;
            project = null;
        }

        [RelayCommand]
        private void MoveMappedProperty(MoveItemArgs args)
        {
            if (args.OldIndex == args.NewIndex || args.OldIndex < 0 || args.NewIndex < 0 || args.OldIndex >= MappingEntityProperties.Count || args.NewIndex >= MappingEntityProperties.Count)
                return;

            MappingEntityProperties.Move(args.OldIndex, args.NewIndex);
        }

        [GeneratedRegex(@"<\s*(\w+)\s*>")]
        private static partial Regex MyRegex();
    }

    public partial class EntityProperty : ObservableObject
    {
        public string Name { get; set; }
        public string Type { get; set; }

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
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

    public partial class MappingEntityProperty : ObservableObject
    {
        public string EntityCompositeName { get; set; }
        public string EntityType { get; set; }

        private string mappingName = null;
        public string MappingName
        {
            // Inteligent getter to simplify unitary test
            get { if (mappingName != null) { return mappingName; } else { return EntityCompositeName; } }
            set { mappingName = value; OnPropertyChanged(nameof(MappingName)); }
        }

        private string mappingType = null;
        public string MappingType
        {
            // Inteligent getter to simplify unitary test
            get { if (mappingType != null) { return mappingType; } else { return EntityType; } }
            set { mappingType = value; }
        }

        public string ParentEntityType { get; set; }
        public bool IsOption => MappingType.Equals(Constants.BiaClassName.OptionDto);
        public bool IsOptionCollection => MappingType.Equals(Constants.BiaClassName.CollectionOptionDto);
        public string OptionType { get; set; }

        private bool isRequired;
        public bool IsRequired
        {
            get => isRequired;
            set
            {
                isRequired = value;
                OnPropertyChanged(nameof(IsRequired));
            }
        }

        public List<string> OptionIdProperties { get; set; } = [];
        public List<string> OptionDisplayProperties { get; set; } = [];
        public List<string> OptionEntityIdProperties { get; set; } = [];

        private string optionDisplayProperty;
        public string OptionDisplayProperty
        {
            get => optionDisplayProperty;
            set
            {
                optionDisplayProperty = value;
                OnPropertyChanged(nameof(OptionDisplayProperty));
            }
        }

        private string optionIdProperty;
        public string OptionIdProperty
        {
            get => optionIdProperty;
            set
            {
                optionIdProperty = value;
                OnPropertyChanged(nameof(OptionIdProperty));
            }
        }

        public bool IsVisibleOptionPropertiesComboBox => IsOption || IsOptionCollection;
        public string OptionEntityIdPropertyComposite { get; private set; }

        private string optionEntityIdProperty;
        public string OptionEntityIdProperty
        {
            get => optionEntityIdProperty;
            set
            {
                optionEntityIdProperty = value;
                OptionEntityIdPropertyComposite = IsCompositeName ?
                    string.Join(".", EntityCompositeName.Split('.').SkipLast(1)) + $".{value}" :
                    value;
                OnPropertyChanged(nameof(OptionEntityIdProperty));
            }
        }

        public string OptionRelationType { get; set; }
        public string OptionRelationFirstIdProperty { get; set; }
        public string OptionRelationSecondIdProperty { get; set; }
        public string OptionRelationPropertyComposite { get; set; }

        private string mappingDateType;
        public string MappingDateType
        {
            get => mappingDateType;
            set
            {
                mappingDateType = value;
                OnPropertyChanged(nameof(MappingDateType));
            }
        }

        public List<string> MappingDateTypes { get; set; } = [];
        public bool IsVisibleDateTypesComboxBox => MappingDateTypes.Count > 0;

        public bool IsValid => ComputeValidity();
        private bool IsCompositeName => EntityCompositeName.Contains('.');

        public bool HasMappingNameError => !string.IsNullOrWhiteSpace(MappingNameError);
        private string mappingNameError;
        public string MappingNameError
        {
            get => mappingNameError;
            set
            {
                mappingNameError = value;
                OnPropertyChanged(nameof(MappingNameError));
                OnPropertyChanged(nameof(HasMappingNameError));
            }
        }

        private bool isParent;
        public bool IsParent
        {
            get => isParent;
            set
            {
                isParent = value;
                OnPropertyChanged(nameof(IsParent));
            }
        }

        private bool asLocalDateTime;
        public bool AsLocalDateTime
        {
            get => asLocalDateTime;
            set
            {
                asLocalDateTime = value;
                OnPropertyChanged(nameof(AsLocalDateTime));
            }
        }

        public bool IsVisibleIsParentCheckbox =>
            EntityCompositeName.EndsWith("Id")
            && !EntityCompositeName.Equals("Id")
            ;


        private bool ComputeValidity()
        {
            // Common validity
            bool isMappingNameValid = !string.IsNullOrWhiteSpace(MappingName) && !HasMappingNameError;
            // Options validity
            bool isOptionIdPropertyValid = !string.IsNullOrWhiteSpace(OptionIdProperty);
            bool isOptionDisplayPropertyValid = !string.IsNullOrWhiteSpace(OptionDisplayProperty);

            if (IsOption)
            {
                bool isOptionEntityIdPropertyValid = !string.IsNullOrWhiteSpace(OptionEntityIdProperty);
                return isMappingNameValid && isOptionIdPropertyValid && isOptionDisplayPropertyValid && isOptionEntityIdPropertyValid;
            }

            if (IsOptionCollection)
            {
                bool isOptionRelationTypeValid = !string.IsNullOrWhiteSpace(OptionRelationType);
                bool isOptionRelationPropertyValid = !string.IsNullOrWhiteSpace(OptionRelationPropertyComposite);
                bool isOptionRelationFirstIdPropertyValid = !string.IsNullOrWhiteSpace(OptionRelationFirstIdProperty);
                bool isOptionRelationSecondIdPropertyValid = !string.IsNullOrWhiteSpace(OptionRelationSecondIdProperty);
                return isMappingNameValid && isOptionIdPropertyValid && isOptionDisplayPropertyValid && isOptionRelationTypeValid && isOptionRelationPropertyValid
                    && isOptionRelationFirstIdPropertyValid && isOptionRelationSecondIdPropertyValid;
            }

            return isMappingNameValid;
        }
    }
}
