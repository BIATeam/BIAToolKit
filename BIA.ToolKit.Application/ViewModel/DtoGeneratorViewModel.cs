namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public class DtoGeneratorViewModel : ObservableObject
    {
        private FileGeneratorService fileGeneratorService;
        private IConsoleWriter consoleWriter;
        private readonly List<EntityInfo> domainEntities = new();
        private readonly List<string> optionCollectionsMappingTypes = new()
        {
            "icollection",
            "list"
        };
        private readonly List<string> standardMappingTypes = new()
        {
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
            "byte[]"
        };
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
                RaisePropertyChanged(nameof(IsProjectChosen));
                RaisePropertyChanged(nameof(IsFileGeneratorInit));
            }
        }

        public bool IsFileGeneratorInit => this.fileGeneratorService?.IsInit == true;

        private ObservableCollection<string> entitiesNames = new();
        public ObservableCollection<string> EntitiesNames
        {
            get => entitiesNames;
            set
            {
                entitiesNames = value;
                RaisePropertyChanged(nameof(EntitiesNames));
            }
        }

        private string selectedEntityName;
        public string SelectedEntityName
        {
            get => selectedEntityName;
            set
            {
                selectedEntityName = value;
                RaisePropertyChanged(nameof(SelectedEntityName));
                AncestorTeam = null;

                if (selectedEntityName != null)
                {
                    var regexMatches = Regex.Match(selectedEntityName, @"\(([^)]*)\)");
                    var entityEndNamespace = $"{regexMatches.Groups[1].Value}.{selectedEntityName.Replace(regexMatches.Groups[0].Value, string.Empty).Trim()}";
                    SelectedEntityInfo = domainEntities.First(e => e.FullNamespace.EndsWith(entityEndNamespace));
                    EntityDomain = entityEndNamespace.Split(".").First();
                }
                else
                {
                    SelectedEntityInfo = null;
                }

                RaisePropertyChanged(nameof(IsEntitySelected));

                RefreshEntityPropertiesTreeView();
                RemoveAllMappingProperties();

                IsTeam = SelectedEntityInfo.IsTeam;
                IsVersioned = SelectedEntityInfo.IsVersioned;
                IsFixable = SelectedEntityInfo.IsFixable;
                IsArchivable = SelectedEntityInfo.IsArchivable;
            }
        }

        private string entityDomain;
        public string EntityDomain
        {
            get => entityDomain;
            set
            {
                entityDomain = value;
                RaisePropertyChanged(nameof(EntityDomain));
                RaisePropertyChanged(nameof(IsGenerationEnabled));
            }
        }

        private string ancestorTeam;

        public string AncestorTeam
        {
            get => ancestorTeam;
            set 
            { 
                ancestorTeam = value; 
                RaisePropertyChanged(nameof(AncestorTeam));
            }
        }

        private ObservableCollection<EntityProperty> entityProperties = new();
        public ObservableCollection<EntityProperty> EntityProperties
        {
            get => entityProperties;
            set { entityProperties = value; }
        }

        public IEnumerable<EntityProperty> AllEntityPropertiesRecursively => EntityProperties.SelectMany(x => x.GetAllPropertiesRecursively());

        private ObservableCollection<MappingEntityProperty> mappingEntityProperties = new();
        public ObservableCollection<MappingEntityProperty> MappingEntityProperties
        {
            get => mappingEntityProperties;
            set
            {
                mappingEntityProperties = value;
                RaisePropertyChanged(nameof(MappingEntityProperties));
                RaisePropertyChanged(nameof(IsGenerationEnabled));
            }
        }

        private bool wasAlreadyGenerated;
        public bool WasAlreadyGenerated
        {
            get => wasAlreadyGenerated;
            set
            {
                wasAlreadyGenerated = value;
                RaisePropertyChanged(nameof(WasAlreadyGenerated));
            }
        }

        private bool _isTeam;
        public bool IsTeam
        {
            get => _isTeam;
            set
            {
                _isTeam = value;
                RaisePropertyChanged(nameof(IsTeam));
            }
        }

        private bool _isVersioned;
        public bool IsVersioned
        {
            get => _isVersioned;
            set
            {
                _isVersioned = value;
                RaisePropertyChanged(nameof(IsVersioned));
            }
        }

        private bool _isArchivable;
        public bool IsArchivable
        {
            get => _isArchivable;
            set
            {
                _isArchivable = value;
                RaisePropertyChanged(nameof(IsArchivable));
            }
        }

        private bool _isFixable;
        public bool IsFixable
        {
            get => _isFixable;
            set
            {
                _isFixable = value;
                RaisePropertyChanged(nameof(IsFixable));
            }
        }

        public string ProjectDomainNamespace { get; private set; }
        public EntityInfo SelectedEntityInfo { get; private set; }
        public bool IsEntitySelected => SelectedEntityInfo != null;
        public bool HasMappingProperties => MappingEntityProperties.Count > 0;
        public bool IsGenerationEnabled => 
            HasMappingProperties 
            && MappingEntityProperties.All(x => x.IsValid) 
            && !string.IsNullOrWhiteSpace(EntityDomain)
            && MappingEntityProperties.Count == MappingEntityProperties.DistinctBy(x => x.MappingName).Count();

        public ICommand RemoveMappingPropertyCommand => new RelayCommand<MappingEntityProperty>(RemoveMappingProperty);
        public ICommand MoveMappedPropertyCommand => new RelayCommand<MoveItemArgs>(x => MoveMappedProperty(x.OldIndex, x.NewIndex));
        public ICommand SetMappedPropertyIsParentCommand => new RelayCommand<MappingEntityProperty>(SetMappedPropertyIsParent);

        public void SetProject(Project project)
        {
            ProjectDomainNamespace = GetProjectDomainNamespace(project);
            IsProjectChosen = true;
        }

        public void Inject(FileGeneratorService fileGeneratorService, IConsoleWriter consoleWriter)
        {
            this.fileGeneratorService = fileGeneratorService;
            this.consoleWriter = consoleWriter;
        }

        public void SetEntities(List<EntityInfo> entities)
        {
            WasAlreadyGenerated = false;
            EntityDomain = null;
            AncestorTeam = null;

            domainEntities.Clear();
            domainEntities.AddRange(entities);

            ComputeBaseKeyType(domainEntities);

            var entitiesNames = entities
                .Select(x => $"{x.Name} ({x.FullNamespace.Replace($"{ProjectDomainNamespace}.", string.Empty).Replace($".{x.Name}", string.Empty)})")
                .OrderBy(x => x)
                .ToList();

            EntitiesNames.Clear();
            foreach (var entityName in entitiesNames)
            {
                EntitiesNames.Add(entityName);
            }
        }

        private static void ComputeBaseKeyType(List<EntityInfo> entities)
        {
            foreach(var entity in entities)
            {
                if (!string.IsNullOrWhiteSpace(entity.BaseKeyType))
                    continue;

                var baseEntityInfo = GetBaseEntityInfoWithNonEmptyBaseKeyType(entity, entities);
                if(baseEntityInfo != null)
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
            if (SelectedEntityInfo == null)
                return;

            foreach (var property in SelectedEntityInfo.Properties)
            {
                var propertyViewModel = new EntityProperty
                {
                    Name = property.Name,
                    Type = property.Type,
                    CompositeName = property.Name,
                    IsSelected = true,
                    ParentType = SelectedEntityInfo.Name
                };
                FillEntityProperties(propertyViewModel, SelectedEntityInfo.Name);
                EntityProperties.Add(propertyViewModel);
            }
        }

        private void FillEntityProperties(EntityProperty property, string rootPropertyType)
        {
            var propertyInfo = domainEntities.FirstOrDefault(e => e.Name == property.Type);
            if (propertyInfo == null)
            {
                return;
            }

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

        public void RefreshMappingProperties()
        {
            var mappingEntityProperties = new List<MappingEntityProperty>(MappingEntityProperties);
            AddMappingProperties(EntityProperties, mappingEntityProperties);
            MappingEntityProperties = new(mappingEntityProperties);

            foreach(var mappingEntityProperty in MappingEntityProperties.Where(x => x.IsOptionCollection))
            {
                mappingEntityProperty.OptionRelationPropertyComposite = AllEntityPropertiesRecursively
                    .SingleOrDefault(x => 
                        x.ParentType == mappingEntityProperty.ParentEntityType 
                        && x.Type.Equals($"ICollection<{mappingEntityProperty.OptionRelationType}>")
                    )?.CompositeName;

                if(string.IsNullOrWhiteSpace(mappingEntityProperty.OptionRelationPropertyComposite))
                {
                    consoleWriter.AddMessageLine($"ERROR: unable to find matching property of type ICollection<{mappingEntityProperty.OptionRelationType}> in type {mappingEntityProperty.ParentEntityType} to map {mappingEntityProperty.EntityCompositeName}", "red");
                }
            }

            RaisePropertyChanged(nameof(HasMappingProperties));
            RaisePropertyChanged(nameof(IsGenerationEnabled));
        }

        private void AddMappingProperties(IEnumerable<EntityProperty> entityProperties, List<MappingEntityProperty> mappingEntityProperties)
        {
            foreach (var selectedEntityProperty in entityProperties)
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

                    if(biaDtoFieldDateTypesByPropertyType.TryGetValue(mappingEntityProperty.MappingType.Replace("?", string.Empty), out List<string> biaDtoFieldDateTypes))
                    {
                        var mappingDateTypes = new List<string>();
                        if(mappingEntityProperty.MappingType == "string")
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
                        var optionEntity = domainEntities.FirstOrDefault(x => x.Name == mappingEntityProperty.OptionType);
                        if (optionEntity != null)
                        {
                            mappingEntityProperty.OptionDisplayProperties.AddRange(optionEntity.Properties.Select(x => x.Name));
                            mappingEntityProperty.OptionDisplayProperty = mappingEntityProperty.OptionDisplayProperties.Where(x => !x.Equals("id", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                            mappingEntityProperty.OptionIdProperties.AddRange(optionEntity.Properties.Where(x => x.Type.Equals("int", StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Name));
                            mappingEntityProperty.OptionIdProperty = mappingEntityProperty.OptionIdProperties.FirstOrDefault(x => x.Equals("id", StringComparison.InvariantCultureIgnoreCase));

                            var entityProperty = AllEntityPropertiesRecursively.First(x => x.CompositeName == mappingEntityProperty.EntityCompositeName);

                            if (mappingEntityProperty.IsOption)
                            {
                                mappingEntityProperty.OptionEntityIdProperties.AddRange(
                                    entityProperties
                                    .Where(x => x.Type.Replace("?", string.Empty).Equals(mappingEntityProperty.OptionType, StringComparison.InvariantCultureIgnoreCase) && !x.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                                    .Select(x => x.Name));

                                if(!mappingEntityProperty.OptionEntityIdProperties.Any())
                                {
                                    consoleWriter.AddMessageLine($"Unable to find {mappingEntityProperty.OptionType} property related to {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                                    entityProperty.IsSelected = false;
                                    continue;
                                }

                                var optionEntityIdPropertyName = $"{mappingEntityProperty.EntityCompositeName.Split(".").Last()}Id";
                                var automaticOptionEntityIdProperty = mappingEntityProperty.OptionEntityIdProperties.FirstOrDefault(x => x.Equals(optionEntityIdPropertyName));
                                if (!string.IsNullOrWhiteSpace(automaticOptionEntityIdProperty))
                                {
                                    mappingEntityProperty.OptionEntityIdProperty = automaticOptionEntityIdProperty;
                                }
                            }

                            if(mappingEntityProperty.IsOptionCollection)
                            {
                                var optionRelationFirstType = selectedEntityProperty.ParentType;
                                var optionRelationSecondType = mappingEntityProperty.OptionType;

                                var relationTypeClassNames = new List<string> { optionRelationFirstType, mappingEntityProperty.OptionType };
                                var entityInfo = domainEntities.SingleOrDefault(x =>
                                    string.IsNullOrEmpty(x.BaseKeyType)
                                    && relationTypeClassNames.All(y => x.Properties.Select(x => x.Type).Contains(y)));

                                if (entityInfo is null)
                                {
                                    consoleWriter.AddMessageLine($"Unable to find relation's entity between types {optionRelationFirstType} and {optionRelationSecondType} to map {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                                    entityProperty.IsSelected = false;
                                    continue;
                                }

                                mappingEntityProperty.OptionRelationType = entityInfo.Name;

                                var optionRelationFirstIdPropertyName = optionRelationFirstType + "Id";
                                mappingEntityProperty.OptionRelationFirstIdProperty = entityInfo.Properties.SingleOrDefault(x => x.Type == "int" && x.Name.Equals(optionRelationFirstIdPropertyName))?.Name;
                                if(string.IsNullOrWhiteSpace(mappingEntityProperty.OptionRelationFirstIdProperty))
                                {
                                    consoleWriter.AddMessageLine($"Unable to find matching relation property {optionRelationFirstIdPropertyName} in the entity {entityInfo.Name} to map {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                                    entityProperty.IsSelected = false;
                                    continue;
                                }

                                var optionRelationSecondIdPropertyName = optionRelationSecondType + "Id";
                                mappingEntityProperty.OptionRelationSecondIdProperty = entityInfo.Properties.SingleOrDefault(x => x.Type == "int" && x.Name.Equals(optionRelationSecondIdPropertyName))?.Name;
                                if (string.IsNullOrWhiteSpace(mappingEntityProperty.OptionRelationSecondIdProperty))
                                {
                                    consoleWriter.AddMessageLine($"Unable to find matching relation property {optionRelationSecondIdPropertyName} in the entity {entityInfo.Name} to map {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                                    entityProperty.IsSelected = false;
                                    continue;
                                }
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

            var regex = new Regex(@"<\s*(\w+)\s*>");
            return regex.Match(optionType).Groups[1].Value;
        }

        private void RemoveMappingProperty(MappingEntityProperty mappingEntityProperty)
        {
            MappingEntityProperties.Remove(mappingEntityProperty);

            ComputePropertiesValidity();

            RaisePropertyChanged(nameof(HasMappingProperties));
            RaisePropertyChanged(nameof(IsGenerationEnabled));
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
            MappingEntityProperties.Clear();

            RaisePropertyChanged(nameof(HasMappingProperties));
            RaisePropertyChanged(nameof(IsGenerationEnabled));
        }

        public void ComputePropertiesValidity()
        {
            const string Error_DuplicateMappingName = "Duplicate property name";

            foreach (var mappingEntityproperty in MappingEntityProperties)
            {
                mappingEntityproperty.MappingNameError = null;
                var duplicateMappingNameProperties = MappingEntityProperties.Where(x => x != mappingEntityproperty && x.MappingName == mappingEntityproperty.MappingName);
                if (duplicateMappingNameProperties.Any())
                {
                    mappingEntityproperty.MappingNameError = Error_DuplicateMappingName;
                    foreach (var duplicateMappingProperty in duplicateMappingNameProperties)
                    {
                        duplicateMappingProperty.MappingNameError = Error_DuplicateMappingName;
                    }
                }
            }

            RaisePropertyChanged(nameof(IsGenerationEnabled));
        }

        public void Clear()
        {
            EntitiesNames.Clear();
            EntityDomain = null;
            AncestorTeam = null;
        }

        private void MoveMappedProperty(int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex || oldIndex < 0 || newIndex < 0 || oldIndex >= MappingEntityProperties.Count || newIndex >= MappingEntityProperties.Count)
                return;

            MappingEntityProperties.Move(oldIndex, newIndex);
        }
    }

    public class EntityProperty : ObservableObject
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
                RaisePropertyChanged(nameof(IsSelected));
            }
        }
        public string CompositeName { get; set; }
        public List<EntityProperty> Properties { get; set; } = new();
        public string ParentType { get; set; }

        public List<EntityProperty> GetAllPropertiesRecursively()
        {
            var allProperties = new List<EntityProperty> { this };
            allProperties.AddRange(Properties.SelectMany(x => x.GetAllPropertiesRecursively()));
            return allProperties;
        }
    }

    public class MappingEntityProperty : ObservableObject
    {
        public string EntityCompositeName { get; set; }
        public string EntityType { get; set; }

        private string mappingName = null;
        public string MappingName
        {
            // Inteligent getter to simplify unitary test
            get { if (mappingName != null) { return mappingName; } else { return EntityCompositeName; } }
            set { mappingName = value; RaisePropertyChanged(nameof(MappingName)); }
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
                RaisePropertyChanged(nameof(IsRequired));
            }
        }

        public List<string> OptionIdProperties { get; set; } = new();
        public List<string> OptionDisplayProperties { get; set; } = new();
        public List<string> OptionEntityIdProperties { get; set; } = new();

        private string optionDisplayProperty;
        public string OptionDisplayProperty
        {
            get => optionDisplayProperty;
            set
            {
                optionDisplayProperty = value;
                RaisePropertyChanged(nameof(OptionDisplayProperty));
            }
        }

        private string optionIdProperty;
        public string OptionIdProperty
        {
            get => optionIdProperty;
            set
            {
                optionIdProperty = value;
                RaisePropertyChanged(nameof(OptionIdProperty));
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
                RaisePropertyChanged(nameof(OptionEntityIdProperty));
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
                RaisePropertyChanged(nameof(MappingDateType));
            }
        }

        public List<string> MappingDateTypes { get; set; } = new();
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
                RaisePropertyChanged(nameof(MappingNameError));
                RaisePropertyChanged(nameof(HasMappingNameError));
            }
        }

        private bool isParent;
        public bool IsParent
        {
            get => isParent;
            set 
            { 
                isParent = value; 
                RaisePropertyChanged(nameof(IsParent));
            }
        }
        public bool IsVisibleIsParentCheckbox => 
            MappingType == "int" 
            && EntityCompositeName.EndsWith("Id") 
            && !EntityCompositeName.Equals("Id")
            ;


        private bool ComputeValidity()
        {
            // Common validity
            var isMappingNameValid = !string.IsNullOrWhiteSpace(MappingName) && !HasMappingNameError;
            // Options validity
            var isOptionIdPropertyValid = !string.IsNullOrWhiteSpace(OptionIdProperty);
            var isOptionDisplayPropertyValid = !string.IsNullOrWhiteSpace(OptionDisplayProperty);

            if(IsOption)
            {
                var isOptionEntityIdPropertyValid = !string.IsNullOrWhiteSpace(OptionEntityIdProperty);
                return isMappingNameValid && isOptionIdPropertyValid && isOptionDisplayPropertyValid && isOptionEntityIdPropertyValid;
            }

            if (IsOptionCollection)
            {
                var isOptionRelationTypeValid = !string.IsNullOrWhiteSpace(OptionRelationType);
                var isOptionRelationPropertyValid = !string.IsNullOrWhiteSpace(OptionRelationPropertyComposite);
                var isOptionRelationFirstIdPropertyValid = !string.IsNullOrWhiteSpace(OptionRelationFirstIdProperty);
                var isOptionRelationSecondIdPropertyValid = !string.IsNullOrWhiteSpace(OptionRelationSecondIdProperty);
                return isMappingNameValid && isOptionIdPropertyValid && isOptionDisplayPropertyValid && isOptionRelationTypeValid && isOptionRelationPropertyValid
                    && isOptionRelationFirstIdPropertyValid && isOptionRelationSecondIdPropertyValid;
            }

            return isMappingNameValid;
        }
    }
}
