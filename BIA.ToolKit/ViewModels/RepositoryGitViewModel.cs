namespace BIA.ToolKit.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using CommunityToolkit.Mvvm.ComponentModel;
    using BIA.ToolKit.Domain;

    public sealed partial class RepositoryGitViewModel(RepositoryGit repositoryGit, GitService gitService, IMessenger messenger, IConsoleWriter consoleWriter) : RepositoryViewModel(repositoryGit, gitService, messenger, consoleWriter)
    {
        public string GitRepositoryName
        {
            get => repositoryGit.GitRepositoryName;
            set
            {
                repositoryGit.GitRepositoryName = value;
                OnPropertyChanged(nameof(GitRepositoryName));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public string LocalClonedFolderPath
        {
            get => repositoryGit.LocalClonedFolderPath;
            set
            {
                repositoryGit.LocalClonedFolderPath = value;
                OnPropertyChanged(nameof(LocalClonedFolderPath));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public string Owner
        {
            get => repositoryGit.Owner;
            set
            {
                repositoryGit.Owner = value;
                OnPropertyChanged(nameof(Owner));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public string ReleasesFolderRegexPattern
        {
            get => repositoryGit.ReleasesFolderRegexPattern;
            set
            {
                repositoryGit.ReleasesFolderRegexPattern = value;
                OnPropertyChanged(nameof(ReleasesFolderRegexPattern));
            }
        }

        public Array ReleaseTypes => Enum.GetValues<ReleaseType>();

        public ReleaseType ReleaseType
        {
            get => repositoryGit.ReleaseType;
            set
            {
                repositoryGit.ReleaseType = value;
                OnPropertyChanged(nameof(ReleaseType));
                OnPropertyChanged(nameof(IsReleaseTypeGit));
                OnPropertyChanged(nameof(IsReleaseTypeFolder));
                OnPropertyChanged(nameof(IsReleaseTypeTag));
                OnPropertyChanged(nameof(IsValid));
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
                repositoryGit.Url = value;
                OnPropertyChanged(nameof(Url));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public bool UseLocalClonedFolder
        {
            get => repositoryGit.UseLocalClonedFolder;
            set
            {
                repositoryGit.UseLocalClonedFolder = value;
                OnPropertyChanged(nameof(UseLocalClonedFolder));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public RepositoryGitKind RepositoryGitKind
        {
            get => repositoryGit.RepositoryGitKind;
            set
            {
                repositoryGit.RepositoryGitKind = value;
                OnPropertyChanged(nameof(RepositoryGitKind));
            }
        }

        public string UrlRelease
        {
            get => repositoryGit.UrlRelease;
            set
            {
                repositoryGit.UrlRelease = value;
                OnPropertyChanged(nameof(UrlRelease));
            }
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
