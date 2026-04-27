namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Models.DtoGenerator;
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
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
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
        /// Guards auto-mapping while the ViewModel performs bulk updates (history reload,
        /// entity switch). When true, IsSelected changes on EntityProperty do not trigger
        /// immediate add/remove — the caller is expected to refresh mappings explicitly.
        /// </summary>
        private bool isBulkLoading;

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

        private readonly Dictionary<string, List<string>> biaDtoFieldDateTypesByPropertyType = DtoMappingService.DateTypesByPropertyType;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFileGeneratorInit))]
        private bool isProjectChosen;

        public bool IsFileGeneratorInit => fileGeneratorService?.IsInit == true;

        [ObservableProperty]
        private ObservableCollection<EntityInfo> entities = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEntitySelected))]
        [NotifyPropertyChangedFor(nameof(IsAuditable))]
        [NotifyPropertyChangedFor(nameof(IsVersionedEnabled))]
        private EntityInfo entity;

        partial void OnEntityChanged(EntityInfo value)
        {
            try
            {
                AncestorTeam = null;
                EntityDomain = null;

                if (value is not null)
                {
                    List<string> namespaceElements = [.. value.Namespace.Split('.')];
                    int dtoNamespaceIndex = namespaceElements.FindIndex(x => x == "Domain");
                    if (dtoNamespaceIndex != -1)
                    {
                        EntityDomain = namespaceElements.ElementAt(dtoNamespaceIndex + 1);
                    }
                }

                RefreshEntityPropertiesTreeView();
                RemoveAllMappingProperties();

                IsVersioned = value?.IsVersioned == true;
                IsTeam = value?.IsTeam == true;
                IsFixable = value?.IsFixable == true;
                IsArchivable = value?.IsArchivable == true;
                SelectedBaseKeyType = value?.BaseKeyType;
                UseDedicatedAudit = false;

                OnEntitySelected();
            }
            catch (Exception ex)
            {
                DiagLog.Write($"Dto.OnEntityChanged: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                // Surface the error to the console rather than crashing the WPF Dispatcher.
                consoleWriter?.AddMessageLine($"Error while preparing DTO entity '{value?.Name}': {ex.Message}", "red");
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGenerationEnabled))]
        private string entityDomain;

        [ObservableProperty]
        private string ancestorTeam;

        public ObservableCollection<EntityProperty> EntityProperties { get; } = [];

        public IEnumerable<EntityProperty> AllEntityPropertiesRecursively => EntityProperties.SelectMany(x => x.GetAllPropertiesRecursively());

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGenerationEnabled))]
        private ObservableCollection<MappingEntityProperty> mappingEntityProperties = [];

        [ObservableProperty]
        private bool wasAlreadyGenerated;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsVersionedEnabled))]
        private bool isTeam;

        [ObservableProperty]
        private bool isVersioned;

        // Team requires Versioned: enabling Team forces Versioned on, and the
        // Versioned checkbox is locked while Team is on.
        partial void OnIsTeamChanged(bool value)
        {
            if (value)
            {
                IsVersioned = true;
            }
        }

        [ObservableProperty]
        private bool isArchivable;

        [ObservableProperty]
        private bool isFixable;

        public static IEnumerable<string> BaseKeyTypeItems => Constants.PrimitiveTypes;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGenerationEnabled))]
        private string selectedBaseKeyType;

        [ObservableProperty]
        private bool useDedicatedAudit;

        public string ProjectDomainNamespace { get; private set; }
        public bool IsEntitySelected => Entity != null;
        public bool IsVersionedEnabled => IsEntitySelected && !IsTeam;
        public bool HasMappingProperties => MappingEntityProperties.Count > 0;
        public bool CanReorderMappingProperties => MappingEntityProperties.Count >= 2;
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
            IsVersioned = generation.IsVersioned;
            IsTeam = generation.IsTeam;
            IsFixable = generation.IsFixable;
            IsArchivable = generation.IsArchivable;
            SelectedBaseKeyType = generation.EntityBaseKeyType;
            UseDedicatedAudit = generation.UseDedicatedAudit;

            // Disable auto-mapping while we replay history: we set IsSelected in bulk and
            // refresh mappings in a single pass at the end.
            isBulkLoading = true;
            try
            {
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
            }
            finally
            {
                isBulkLoading = false;
            }

            SyncMappingReferences();
            ComputePropertiesValidity();
        }

        [RelayCommand]
        private void Generate()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
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

                WeakReferenceMessenger.Default.Send(new EntityGenerationCompletedMessage());
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
            isBulkLoading = true;
            try
            {
                foreach (EntityProperty ep in AllEntityPropertiesRecursively)
                {
                    ep.IsSelected = false;
                }
                MappingEntityProperties.Clear();
            }
            finally
            {
                isBulkLoading = false;
            }

            SyncMappingReferences();
            OnPropertyChanged(nameof(HasMappingProperties));
            OnPropertyChanged(nameof(CanReorderMappingProperties));
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
            UnhookEntityPropertySubscriptions();
            EntityProperties.Clear();
            if (Entity == null)
                return;

            // Delegate to DtoMappingService to keep the tree-building logic in one place.
            List<EntityProperty> tree = DtoMappingService.BuildEntityPropertyTree(Entity, Entities);
            foreach (EntityProperty ep in tree)
                EntityProperties.Add(ep);

            HookEntityPropertySubscriptions();
        }

        /// <summary>
        /// Subscribe to <see cref="EntityProperty.IsSelected"/> on every node of the tree so
        /// ticking/unticking a checkbox immediately adds/removes the corresponding mapping.
        /// </summary>
        private void HookEntityPropertySubscriptions()
        {
            foreach (EntityProperty ep in AllEntityPropertiesRecursively)
            {
                ep.PropertyChanged -= OnEntityPropertyPropertyChanged;
                ep.PropertyChanged += OnEntityPropertyPropertyChanged;
            }
        }

        private void UnhookEntityPropertySubscriptions()
        {
            foreach (EntityProperty ep in AllEntityPropertiesRecursively)
            {
                ep.PropertyChanged -= OnEntityPropertyPropertyChanged;
            }
        }

        private void OnEntityPropertyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (isBulkLoading) return;
            if (e.PropertyName != nameof(EntityProperty.IsSelected)) return;
            if (sender is not EntityProperty ep) return;

            if (ep.IsSelected)
            {
                // Add: RefreshMappingProperties scans all selected properties and appends
                // any that are not yet in MappingEntityProperties. It also resolves option
                // relations. Cheaper than duplicating single-property creation logic.
                RefreshMappingProperties();
            }
            else
            {
                MappingEntityProperty mapping = MappingEntityProperties.FirstOrDefault(x => x.EntityCompositeName == ep.CompositeName);
                if (mapping != null)
                {
                    MappingEntityProperties.Remove(mapping);
                }
            }

            SyncMappingReferences();
            ComputePropertiesValidity();
            OnPropertyChanged(nameof(HasMappingProperties));
            OnPropertyChanged(nameof(CanReorderMappingProperties));
            OnPropertyChanged(nameof(IsGenerationEnabled));
        }

        /// <summary>
        /// Keep each EntityProperty's MappingEntityProperty reference in sync with the
        /// current MappingEntityProperties collection. Used by the XAML to bind inline
        /// mapping editors directly off the tree nodes.
        /// </summary>
        private void SyncMappingReferences()
        {
            foreach (EntityProperty ep in AllEntityPropertiesRecursively)
            {
                ep.MappingEntityProperty = MappingEntityProperties.FirstOrDefault(x => x.EntityCompositeName == ep.CompositeName);
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
            // Mutate the existing collection rather than replacing it: preserves drag-drop
            // behaviour references and any XAML bindings watching the ObservableCollection.
            AddMappingProperties(EntityProperties, MappingEntityProperties);

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
            OnPropertyChanged(nameof(CanReorderMappingProperties));
            OnPropertyChanged(nameof(IsGenerationEnabled));
        }

        private void AddMappingProperties(IEnumerable<EntityProperty> entityProperties, ICollection<MappingEntityProperty> mappingEntityProperties)
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
                        MappingType = DtoMappingService.ComputeMappingType(selectedEntityProperty)
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
                        mappingEntityProperty.OptionType = DtoMappingService.ExtractOptionType(selectedEntityProperty.Type);
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
                                    consoleWriter.AddMessageLine($"Unable to find matching relation property {optionRelationSecondIdPropertyName} in the entity {entityInfo.Name} to map {mappingEntityProperty.EntityCompositeName}", "orange");
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

        [RelayCommand]
        private void RemoveMappingProperty(MappingEntityProperty mappingEntityProperty)
        {
            MappingEntityProperties.Remove(mappingEntityProperty);

            ComputePropertiesValidity();

            OnPropertyChanged(nameof(HasMappingProperties));
            OnPropertyChanged(nameof(CanReorderMappingProperties));
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
    }
}
