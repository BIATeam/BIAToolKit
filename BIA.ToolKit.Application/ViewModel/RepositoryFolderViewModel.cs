namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Domain;
    using CommunityToolkit.Mvvm.Messaging;

    public sealed class RepositoryFolderViewModel(RepositoryFolder repositoryFolder, GitService gitService, IMessenger messenger, IConsoleWriter consoleWriter)
        : RepositoryViewModel(repositoryFolder, gitService, messenger, consoleWriter)
    {
        public string Path
        {
            get => repositoryFolder.Path;
            set
            {
                repositoryFolder.Path = value; RaisePropertyChanged(nameof(Path));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        public string ReleasesFolderRegexPattern
        {
            get => repositoryFolder.ReleasesFolderRegexPattern;
            set { repositoryFolder.ReleasesFolderRegexPattern = value; RaisePropertyChanged(nameof(ReleasesFolderRegexPattern)); }
        }

        protected override bool EnsureIsValid()
        {
            return !string.IsNullOrWhiteSpace(Path) && Directory.Exists(Path);
        }
    }
}
