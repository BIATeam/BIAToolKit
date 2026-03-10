namespace BIA.ToolKit.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.ViewModel.Messaging.Messages;
    using CommunityToolkit.Mvvm.ComponentModel;
    using BIA.ToolKit.Domain;

    public sealed partial class RepositoryFolderViewModel(RepositoryFolder repositoryFolder, GitService gitService, IMessenger messenger, IConsoleWriter consoleWriter)
        : RepositoryViewModel(repositoryFolder, gitService, messenger, consoleWriter)
    {
        public string Path
        {
            get => repositoryFolder.Path;
            set
            {
                repositoryFolder.Path = value;
                OnPropertyChanged(nameof(Path));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public string ReleasesFolderRegexPattern
        {
            get => repositoryFolder.ReleasesFolderRegexPattern;
            set
            {
                repositoryFolder.ReleasesFolderRegexPattern = value;
                OnPropertyChanged(nameof(ReleasesFolderRegexPattern));
            }
        }

        protected override bool EnsureIsValid()
        {
            return !string.IsNullOrWhiteSpace(Path) && Directory.Exists(Path);
        }
    }
}
