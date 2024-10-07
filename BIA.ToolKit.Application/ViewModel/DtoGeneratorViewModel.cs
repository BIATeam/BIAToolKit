﻿namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using LibGit2Sharp;
    using Microsoft.Extensions.FileSystemGlobbing.Internal;
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
        private string projectDomainNamespace;
        private IConsoleWriter consoleWriter;
        private readonly List<EntityInfo> domainEntities = new();
        private readonly List<string> excludedEntityDomains = new()
        {
            "AuditModule",
            "NotificationModule",
            "SiteModule",
            "TranslationModule",
            "UserModule",
            "RepoContract",
            "ViewModule"
        };
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
            "TimeSpan",
            "TimeOnly",
            "byte[]"
        };
        private readonly Dictionary<string, List<string>> biaDtoFieldDateTypesByPropertyType = new()
        {
            { "string", new List<string> { "Time" } },
            { "TimeSpan", new List<string> { "Time" } },
            { "DateTime", new List<string> { "Datetime", "Date", "Time" } }
        };

        private bool isProjectChosen;
        public bool IsProjectChosen
        {
            get => isProjectChosen;
            set
            {
                isProjectChosen = value;
                RaisePropertyChanged(nameof(IsProjectChosen));
            }
        }

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

                SelectedEntityInfo = selectedEntityName == null ? null : domainEntities.First(e => e.FullNamespace.EndsWith(selectedEntityName));
                RaisePropertyChanged(nameof(IsEntitySelected));

                EntityDomain = selectedEntityName?.Split(".").First();

                RefreshEntityPropertiesTreeView();
                RemoveAllMappingProperties();
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

        private ObservableCollection<EntityProperty> entityProperties = new();
        public ObservableCollection<EntityProperty> EntityProperties
        {
            get => entityProperties;
            set { entityProperties = value; }
        }

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

        public EntityInfo SelectedEntityInfo { get; private set; }
        public bool IsEntitySelected => SelectedEntityInfo != null;
        public bool HasMappingProperties => MappingEntityProperties.Count > 0;
        public bool IsGenerationEnabled => 
            HasMappingProperties 
            && MappingEntityProperties.All(x => x.IsValid) 
            && !string.IsNullOrWhiteSpace(EntityDomain)
            && MappingEntityProperties.Count == MappingEntityProperties.DistinctBy(x => x.MappingName).Count();

        public ICommand RemoveMappingPropertyCommand => new RelayCommand<MappingEntityProperty>((mappingEntityProperty) => RemoveMappingProperty(mappingEntityProperty));

        public void SetProject(Project project)
        {
            projectDomainNamespace = GetProjectDomainNamespace(project);
            IsProjectChosen = true;
        }

        public void Inject(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void SetEntities(List<EntityInfo> entities)
        {
            domainEntities.Clear();
            domainEntities.AddRange(entities);

            ComputeBaseKeyType(domainEntities);

            var entitiesNames = entities
                .Where(x => !excludedEntityDomains.Any(y => x.Path.Contains(y)))
                .Select(x => x.FullNamespace.Replace($"{projectDomainNamespace}.", string.Empty))
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
                FillEntityProperties(propertyViewModel);
                EntityProperties.Add(propertyViewModel);
            }
        }

        private void FillEntityProperties(EntityProperty property)
        {
            var propertyEntity = domainEntities.FirstOrDefault(e => e.Name == property.Type);
            if (propertyEntity == null)
            {
                return;
            }

            property.Properties.AddRange(propertyEntity.Properties.Select(p => new EntityProperty { Name = p.Name, Type = p.Type, CompositeName = $"{property.CompositeName}.{p.Name}", ParentType = property.Type }));
            property.Properties.ForEach(p => FillEntityProperties(p));
        }

        public void RefreshMappingProperties()
        {
            var mappingEntityProperties = new List<MappingEntityProperty>(MappingEntityProperties);
            AddMappingProperties(EntityProperties, mappingEntityProperties);
            MappingEntityProperties = new(mappingEntityProperties.OrderBy(x => x.EntityCompositeName));

            foreach(var mappingEntityProperty in MappingEntityProperties.Where(x => x.IsOptionCollection))
            {
                mappingEntityProperty.OptionRelationPropertyComposite = EntityProperties
                    .SelectMany(x => x.GetAllPropertiesRecursively())
                    .SingleOrDefault(x => 
                        x.ParentType == mappingEntityProperty.ParentEntityType 
                        && x.Type.Equals($"ICollection<{mappingEntityProperty.OptionRelationType}>")
                    )?.CompositeName;

                if(string.IsNullOrWhiteSpace(mappingEntityProperty.OptionRelationPropertyComposite))
                {
                    consoleWriter.AddMessageLine($"ERROR: unable to find matching property of type ICollection<{mappingEntityProperty.OptionRelationType}> in type {mappingEntityProperty.ParentEntityType} to map {mappingEntityProperty.EntityCompositeName}", "red");
                }
            }

            ComputePropertiesValidity();

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
                        ParentEntityType = selectedEntityProperty.ParentType,
                        MappingName = selectedEntityProperty.CompositeName.Replace(".", string.Empty),
                        MappingType = ComputeMappingType(selectedEntityProperty)
                    };

                    if(biaDtoFieldDateTypesByPropertyType.TryGetValue(mappingEntityProperty.MappingType, out List<string> biaDtoFieldDateTypes))
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

                            if (mappingEntityProperty.IsOption)
                            {
                                mappingEntityProperty.OptionEntityIdProperties.AddRange(
                                    entityProperties
                                    .Where(x => x.Type.Equals("int", StringComparison.InvariantCultureIgnoreCase) && !x.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                                    .Select(x => x.Name));

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
                                    continue;
                                }

                                mappingEntityProperty.OptionRelationType = entityInfo.Name;

                                var optionRelationFirstIdPropertyName = optionRelationFirstType + "Id";
                                mappingEntityProperty.OptionRelationFirstIdProperty = entityInfo.Properties.SingleOrDefault(x => x.Type == "int" && x.Name.Equals(optionRelationFirstIdPropertyName))?.Name;
                                if(string.IsNullOrWhiteSpace(mappingEntityProperty.OptionRelationFirstIdProperty))
                                {
                                    consoleWriter.AddMessageLine($"Unable to find matching relation property {optionRelationFirstIdPropertyName} in the entity {entityInfo.Name} to map {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
                                    continue;
                                }

                                var optionRelationSecondIdPropertyName = optionRelationSecondType + "Id";
                                mappingEntityProperty.OptionRelationSecondIdProperty = entityInfo.Properties.SingleOrDefault(x => x.Type == "int" && x.Name.Equals(optionRelationSecondIdPropertyName))?.Name;
                                if (string.IsNullOrWhiteSpace(mappingEntityProperty.OptionRelationSecondIdProperty))
                                {
                                    consoleWriter.AddMessageLine($"Unable to find matching relation property {optionRelationSecondIdPropertyName} in the entity {entityInfo.Name} to map {mappingEntityProperty.EntityCompositeName}, the mapping for this property has been ignored.", "orange");
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

            if (standardMappingTypes.Any(x => entityProperty.Type.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                return entityProperty.Type;
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

        public void RemoveAllMappingProperties()
        {
            MappingEntityProperties.Clear();

            RaisePropertyChanged(nameof(HasMappingProperties));
            RaisePropertyChanged(nameof(IsGenerationEnabled));
        }

        public void ComputePropertiesValidity()
        {
            const string Error_DuplicateMappingName = "Duplicate property name";

            var propertiesToRemove = new List<MappingEntityProperty>();
            foreach (var property in MappingEntityProperties)
            {
                var mappingOptionWithSameEntityIdProperty = MappingEntityProperties.FirstOrDefault(x => x.IsOption && x.OptionEntityIdPropertyComposite.Equals(property.EntityCompositeName));
                if (mappingOptionWithSameEntityIdProperty != null)
                {
                    consoleWriter.AddMessageLine($"The entity's property {property.EntityCompositeName} is already used as mapping ID property for the OptionDto {mappingOptionWithSameEntityIdProperty.MappingName}, the mapping for this property has been ignored.", "orange");
                    propertiesToRemove.Add(property);
                }
            }

            foreach (var property in propertiesToRemove)
            {
                MappingEntityProperties.Remove(property);
            }

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
    }

    public class EntityProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsSelected { get; set; }
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
        public string MappingName { get; set; }
        public string ParentEntityType { get; set; }
        public string MappingType { get; set; }
        public bool IsOption => MappingType.Equals(Constants.BiaClassName.OptionDto);
        public bool IsOptionCollection => MappingType.Equals(Constants.BiaClassName.CollectionOptionDto);
        public string OptionType { get; set; }
        public bool IsRequired { get; set; }
        public List<string> OptionIdProperties { get; set; } = new();
        public List<string> OptionDisplayProperties { get; set; } = new();
        public List<string> OptionEntityIdProperties { get; set; } = new();
        public string OptionDisplayProperty { get; set; }
        public string OptionIdProperty { get; set; }
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
        public string MappingDateType { get; set; }
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
