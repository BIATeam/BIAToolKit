namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain;

    public abstract class RepositoryViewModelBase : ObservableObject
    {
        private readonly Repository repository;

        protected RepositoryViewModelBase(Repository repository)
        {
            ArgumentNullException.ThrowIfNull(repository, nameof(repository));
            this.repository = repository;
        }

        public bool IsGitRepository => RepositoryType == RepositoryType.Git;
        public bool IsFolderRepository => RepositoryType == RepositoryType.Folder;

        public string CompanyName
        {
            get => repository.CompanyName;
            set
            {
                repository.CompanyName = value;
                RaisePropertyChanged(nameof(CompanyName));
            }
        }

        public string Name
        {
            get => repository.Name;
            set
            {
                repository.Name = value;
                RaisePropertyChanged(nameof(Name));
            }
        }

        public string ProjectName
        {
            get => repository.ProjectName;
            set
            {
                repository.ProjectName = value;
                RaisePropertyChanged(nameof(ProjectName));
            }
        }

        public RepositoryType RepositoryType
        {
            get => repository.RepositoryType;
            set
            {
                repository.RepositoryType = value;
                RaisePropertyChanged(nameof(RepositoryType));
                RaisePropertyChanged(nameof(IsGitRepository));
                RaisePropertyChanged(nameof(IsFolderRepository));
            }
        }
    }
}
