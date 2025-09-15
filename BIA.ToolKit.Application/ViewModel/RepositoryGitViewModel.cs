namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain;

    public sealed class RepositoryGitViewModel(RepositoryGit repositoryGit, GitService gitService, UIEventBroker eventBroker, IConsoleWriter consoleWriter) : RepositoryViewModel(repositoryGit, gitService, eventBroker, consoleWriter)
    {
        public string GitRepositoryName
        {
            get => repositoryGit.GitRepositoryName;
            set
            {
                repositoryGit.GitRepositoryName = value; RaisePropertyChanged(nameof(GitRepositoryName));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        public string LocalClonedFolderPath
        {
            get => repositoryGit.LocalClonedFolderPath;
            set
            {
                repositoryGit.LocalClonedFolderPath = value; RaisePropertyChanged(nameof(LocalClonedFolderPath));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        public string Owner
        {
            get => repositoryGit.Owner;
            set
            {
                repositoryGit.Owner = value; RaisePropertyChanged(nameof(Owner));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        public string ReleasesFolderRegexPattern
        {
            get => repositoryGit.ReleasesFolderRegexPattern;
            set { repositoryGit.ReleasesFolderRegexPattern = value; RaisePropertyChanged(nameof(ReleasesFolderRegexPattern)); }
        }

        public Array ReleaseTypes => Enum.GetValues<ReleaseType>();

        public ReleaseType ReleaseType
        {
            get => repositoryGit.ReleaseType;
            set
            {
                repositoryGit.ReleaseType = value;
                RaisePropertyChanged(nameof(ReleaseType));
                RaisePropertyChanged(nameof(IsReleaseTypeGit));
                RaisePropertyChanged(nameof(IsReleaseTypeFolder));
                RaisePropertyChanged(nameof(IsReleaseTypeTag));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        public bool IsReleaseTypeGit => ReleaseType == ReleaseType.Git;
        public bool IsReleaseTypeFolder => ReleaseType == ReleaseType.Folder;
        public bool IsReleaseTypeTag => ReleaseType == ReleaseType.Tag;

        public string Url
        {
            get => repositoryGit.Url;
            set
            {
                repositoryGit.Url = value; RaisePropertyChanged(nameof(Url));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        public bool UseLocalClonedFolder
        {
            get => repositoryGit.UseLocalClonedFolder;
            set
            {
                repositoryGit.UseLocalClonedFolder = value; RaisePropertyChanged(nameof(UseLocalClonedFolder));
                RaisePropertyChanged(nameof(IsValid));
            }
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

        protected override bool EnsureIsValid()
        {
            var releaseFieldsValid = ReleaseType switch
            {
                ReleaseType.Git => !string.IsNullOrWhiteSpace(GitRepositoryName) && !string.IsNullOrWhiteSpace(Owner),
                ReleaseType.Folder => true,
                ReleaseType.Tag => true,
                _ => throw new NotImplementedException(),
            };

            return !string.IsNullOrWhiteSpace(Url) && (!UseLocalClonedFolder || UseLocalClonedFolder && !string.IsNullOrWhiteSpace(LocalClonedFolderPath) && Directory.Exists(LocalClonedFolderPath)) && releaseFieldsValid;
        }
    }
}
