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

    public class DtoGeneratorViewModel : ObservableObject
    {
        private string projectDomainNamespace;
        private readonly List<EntityInfo> entities;

        public DtoGeneratorViewModel()
        {
            entities = new();
            EntitiesNames = new();
            EntityProperties = new();
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
            }
        }

        private ObservableCollection<EntityPropertyViewModel> entityProperties;

        public ObservableCollection<EntityPropertyViewModel> EntityProperties
        {
            get => entityProperties;
            set { entityProperties = value; }
        }


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

            var selectedEntity = entities.First(e => e.FullNamespace.EndsWith(SelectedEntityName));
            foreach(var property in selectedEntity.Properties)
            {
                var propertyViewModel = new EntityPropertyViewModel
                {
                    Name = property.Name,
                    Type = property.Type,
                    IsPrincipal = true
                };
                FillEntityProperties(propertyViewModel);
                EntityProperties.Add(propertyViewModel);
            }
        }

        private void FillEntityProperties(EntityPropertyViewModel property)
        {
            var propertyEntity = entities.FirstOrDefault(e => e.Name == property.Type);
            if(propertyEntity == null)
            {
                return;
            }

            property.Properties.AddRange(propertyEntity.Properties.Select(p => new EntityPropertyViewModel { Name = p.Name, Type = p.Type }));
            property.Properties.ForEach(p => FillEntityProperties(p));
        }
    }

    public class EntityPropertyViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsSelected { get; set; }
        public bool IsPrincipal { get; set; }
        public bool IsChild => !IsPrincipal;
        public List<EntityPropertyViewModel> Properties { get; set; } = new();
    }
}
