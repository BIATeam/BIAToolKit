namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using Microsoft.Extensions.FileSystemGlobbing.Internal;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection.Metadata;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public class DtoGeneratorViewModel : ObservableObject
    {
        private string projectDomainNamespace;
        private readonly List<EntityInfo> entities;
        private readonly List<string> OptionsMappingTypes = new()
        {
            "icollection",
            "list"
        };
        private readonly List<string> StandardMappingTypes = new()
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

        public DtoGeneratorViewModel()
        {
            entities = new();
            EntitiesNames = new();
            EntityProperties = new();
            MappingEntityProperties = new();
        }

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

        private ObservableCollection<string> entitiesNames;
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

                SelectedEntityInfo = selectedEntityName == null ? null : entities.First(e => e.FullNamespace.EndsWith(selectedEntityName));
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

        private ObservableCollection<EntityProperty> entityProperties;
        public ObservableCollection<EntityProperty> EntityProperties
        {
            get => entityProperties;
            set { entityProperties = value; }
        }

        private ObservableCollection<MappingEntityProperty> mappingEntityProperties;
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
        public bool IsGenerationEnabled => HasMappingProperties && !string.IsNullOrWhiteSpace(EntityDomain);

        public ICommand RemoveMappingPropertyCommand => new RelayCommand<MappingEntityProperty>((mappingEntityProperty) => RemoveMappingProperty(mappingEntityProperty));

        public void SetProject(Project project)
        {
            projectDomainNamespace = GetProjectDomainNamespace(project);
            IsProjectChosen = true;
        }

        public void SetEntities(List<EntityInfo> entities)
        {
            this.entities.Clear();
            this.entities.AddRange(entities);

            var entitiesNames = entities
                .Select(x => x.FullNamespace.Replace($"{projectDomainNamespace}.", string.Empty))
                .OrderBy(x => x)
                .ToList();

            EntitiesNames.Clear();
            foreach (var entityName in entitiesNames)
            {
                EntitiesNames.Add(entityName);
            }
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
                    IsSelected = true
                };
                FillEntityProperties(propertyViewModel);
                EntityProperties.Add(propertyViewModel);
            }
        }

        private void FillEntityProperties(EntityProperty property)
        {
            var propertyEntity = entities.FirstOrDefault(e => e.Name == property.Type);
            if (propertyEntity == null)
            {
                return;
            }

            property.Properties.AddRange(propertyEntity.Properties.Select(p => new EntityProperty { Name = p.Name, Type = p.Type, CompositeName = $"{property.CompositeName}.{p.Name}" }));
            property.Properties.ForEach(p => FillEntityProperties(p));
        }

        public void RefreshMappingProperties()
        {
            var mappingEntityProperties = new List<MappingEntityProperty>(MappingEntityProperties);
            AddMappingProperties(EntityProperties, mappingEntityProperties);
            MappingEntityProperties = new(mappingEntityProperties.OrderBy(x => x.CompositeName));

            RaisePropertyChanged(nameof(HasMappingProperties));
            RaisePropertyChanged(nameof(IsGenerationEnabled));
        }

        private void AddMappingProperties(IEnumerable<EntityProperty> entityProperties, List<MappingEntityProperty> mappingEntityProperties)
        {
            foreach (var selectedEntityProperty in entityProperties)
            {
                if (selectedEntityProperty.IsSelected && !mappingEntityProperties.Any(x => x.CompositeName == selectedEntityProperty.CompositeName))
                {
                    var mappingEntityProperty = new MappingEntityProperty
                    {
                        CompositeName = selectedEntityProperty.CompositeName,
                        MappingName = selectedEntityProperty.CompositeName.Replace(".", string.Empty),
                        MappingType = ComputeMappingType(selectedEntityProperty)
                    };

                    if(mappingEntityProperty.IsOption)
                    {
                        mappingEntityProperty.OptionType = ExtractOptionType(selectedEntityProperty.Type);
                    }

                    mappingEntityProperties.Add(mappingEntityProperty);
                }
                AddMappingProperties(selectedEntityProperty.Properties, mappingEntityProperties);
            }
        }

        private string ComputeMappingType(EntityProperty entityProperty)
        {
            if (OptionsMappingTypes.Any(x => entityProperty.Type.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                return Constants.BiaClassName.CollectionOptionDto;
            }

            if(StandardMappingTypes.Any(x => entityProperty.Type.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
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

            RaisePropertyChanged(nameof(HasMappingProperties));
            RaisePropertyChanged(nameof(IsGenerationEnabled));
        }

        public void RemoveAllMappingProperties()
        {
            MappingEntityProperties.Clear();

            RaisePropertyChanged(nameof(HasMappingProperties));
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
    }

    public class MappingEntityProperty
    {
        public string CompositeName { get; set; }
        public string MappingName { get; set; }
        public string MappingType { get; set; }
        public bool IsOption => MappingType.Equals(Constants.BiaClassName.OptionDto) || MappingType.Equals(Constants.BiaClassName.CollectionOptionDto);
        public string OptionType { get; set; }
    }
}
