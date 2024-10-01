namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
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
                RefreshEntityPropertiesTreeView();
                RemoveAllMappingProperties();
            }
        }

        private ObservableCollection<EntityPropertyViewModel> entityProperties;

        public ObservableCollection<EntityPropertyViewModel> EntityProperties
        {
            get => entityProperties;
            set { entityProperties = value; }
        }

        private ObservableCollection<MappingEntityPropertyViewModel> mappingEntityProperties;

        public ObservableCollection<MappingEntityPropertyViewModel> MappingEntityProperties
        {
            get => mappingEntityProperties;
            set
            {
                mappingEntityProperties = value;
                RaisePropertyChanged(nameof(MappingEntityProperties));
            }
        }

        public bool IsGenerateButtonEnabled => MappingEntityProperties.Count > 0;

        public ICommand RemoveMappingPropertyCommand => new RelayCommand<MappingEntityPropertyViewModel>((mappingEntityProperty) => RemoveMappingProperty(mappingEntityProperty));

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
            if (SelectedEntityName == null)
                return;

            var selectedEntity = entities.First(e => e.FullNamespace.EndsWith(SelectedEntityName));
            foreach (var property in selectedEntity.Properties)
            {
                var propertyViewModel = new EntityPropertyViewModel
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

        private void FillEntityProperties(EntityPropertyViewModel property)
        {
            var propertyEntity = entities.FirstOrDefault(e => e.Name == property.Type);
            if (propertyEntity == null)
            {
                return;
            }

            property.Properties.AddRange(propertyEntity.Properties.Select(p => new EntityPropertyViewModel { Name = p.Name, Type = p.Type, CompositeName = $"{property.CompositeName}.{p.Name}" }));
            property.Properties.ForEach(p => FillEntityProperties(p));
        }

        public void RefreshMappingProperties()
        {
            var mappingEntityProperties = new List<MappingEntityPropertyViewModel>(MappingEntityProperties);
            AddMappingProperties(EntityProperties, mappingEntityProperties);
            MappingEntityProperties = new(mappingEntityProperties.OrderBy(x => x.CompositeName));
            RaisePropertyChanged(nameof(IsGenerateButtonEnabled));
        }

        private void AddMappingProperties(IEnumerable<EntityPropertyViewModel> entityProperties, List<MappingEntityPropertyViewModel> mappingEntityProperties)
        {
            foreach (var selectedEntityProperty in entityProperties)
            {
                if (selectedEntityProperty.IsSelected && !mappingEntityProperties.Any(x => x.CompositeName == selectedEntityProperty.CompositeName))
                {
                    mappingEntityProperties.Add(new MappingEntityPropertyViewModel
                    {
                        CompositeName = selectedEntityProperty.CompositeName,
                        MappingName = selectedEntityProperty.CompositeName.Replace(".", string.Empty),
                        MappingType = ComputeMappingType(selectedEntityProperty)
                    });
                }
                AddMappingProperties(selectedEntityProperty.Properties, mappingEntityProperties);
            }
        }

        private string ComputeMappingType(EntityPropertyViewModel entityProperty)
        {
            const string ListOptionDto = "List<OptionDto>";
            const string OptionDto = "OptionDto";

            if (OptionsMappingTypes.Any(x => entityProperty.Type.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                return ListOptionDto;
            }

            if(StandardMappingTypes.Any(x => entityProperty.Type.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                return entityProperty.Type;
            }

            return OptionDto;
        }

        private void RemoveMappingProperty(MappingEntityPropertyViewModel mappingEntityProperty)
        {
            MappingEntityProperties.Remove(mappingEntityProperty);
            RaisePropertyChanged(nameof(IsGenerateButtonEnabled));
        }

        public void RemoveAllMappingProperties()
        {
            MappingEntityProperties.Clear();
            RaisePropertyChanged(nameof(IsGenerateButtonEnabled));
        }
    }

    public class EntityPropertyViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsSelected { get; set; }
        public string CompositeName { get; set; }
        public List<EntityPropertyViewModel> Properties { get; set; } = new();
    }

    public class MappingEntityPropertyViewModel
    {
        public string CompositeName { get; set; }
        public string MappingName { get; set; }
        public string MappingType { get; set; }
    }
}
