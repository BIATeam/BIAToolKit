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
        }

        public event EventHandler<EntityInfo> EntityChanged;

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
                NotifyEntityChanged();
            }
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
                .Select(x => string.Join(".", x.Namespace, x.Name).Replace($"{projectDomainNamespace}.", string.Empty))
                .OrderBy(x => x)
                .ToList();

            EntitiesNames.Clear();
            foreach (var entityName in entitiesNames)
            {
                EntitiesNames.Add(entityName);
            }
        }

        private void NotifyEntityChanged()
        {
            var selectedEntityNamespace = $"{projectDomainNamespace}.{SelectedEntityName}";
            var entity = entities.First(e => string.Join(".", e.Namespace, e.Name).Equals(selectedEntityNamespace));
            EntityChanged?.Invoke(this, entity);
        }

        private static string GetProjectDomainNamespace(Project project)
        {
            if (project == null)
                return string.Empty;

            return string.Join(".", project.CompanyName, project.Name, "Domain");
        }
    }
}
