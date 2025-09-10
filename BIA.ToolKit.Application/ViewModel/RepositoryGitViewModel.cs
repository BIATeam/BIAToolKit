namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain;

    public sealed class RepositoryGitViewModel(RepositoryGit repositoryGit) : RepositoryViewModelBase(repositoryGit)
    {
        public string GitRepositoryName
        {
            get => repositoryGit.GitRepositoryName;
            set { repositoryGit.GitRepositoryName = value; RaisePropertyChanged(nameof(GitRepositoryName)); }
        }

        public string LocalClonedFolderPath
        {
            get => repositoryGit.LocalClonedFolderPath;
            set { repositoryGit.LocalClonedFolderPath = value; RaisePropertyChanged(nameof(LocalClonedFolderPath)); }
        }

        public string Owner
        {
            get => repositoryGit.Owner;
            set { repositoryGit.Owner = value; RaisePropertyChanged(nameof(Owner)); }
        }

        public string ReleasesFolderRegexPattern
        {
            get => repositoryGit.ReleasesFolderRegexPattern;
            set { repositoryGit.ReleasesFolderRegexPattern = value; RaisePropertyChanged(nameof(ReleasesFolderRegexPattern)); }
        }

        public ReleaseType ReleaseType
        {
            get => repositoryGit.ReleaseType;
            set { repositoryGit.ReleaseType = value; RaisePropertyChanged(nameof(ReleaseType)); }
        }

        public string Url
        {
            get => repositoryGit.Url;
            set { repositoryGit.Url = value; RaisePropertyChanged(nameof(Url)); }
        }

        public bool UseLocalClonedFolder
        {
            get => repositoryGit.UseLocalClonedFolder;
            set { repositoryGit.UseLocalClonedFolder = value; RaisePropertyChanged(nameof(UseLocalClonedFolder)); }
        }

        public RepositoryGitKind RepositoryGitKind
        {
            get => repositoryGit.RepositoryGitKind;
            set { repositoryGit.RepositoryGitKind = value; RaisePropertyChanged(nameof(RepositoryGitKind)); }
        }

        public string UrlRelease
        {
            get => repositoryGit.UrlRelease;
            set { repositoryGit.UrlRelease = value; RaisePropertyChanged(nameof(UrlRelease)); }
        }
    }
}
