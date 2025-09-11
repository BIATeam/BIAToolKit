namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain;

    public sealed class RepositoryFolderViewModel(RepositoryFolder repositoryFolder, GitService gitService, UIEventBroker eventBroker, IConsoleWriter consoleWriter)
        : RepositoryViewModel(repositoryFolder, gitService, eventBroker, consoleWriter)
    {
        public string Path
        {
            get => repositoryFolder.Path;
            set { repositoryFolder.Path = value; RaisePropertyChanged(nameof(Path)); }
        }

        public string ReleaseFolderRegexPattern
        {
            get => repositoryFolder.ReleaseFolderRegexPattern;
            set { repositoryFolder.ReleaseFolderRegexPattern = value; RaisePropertyChanged(nameof(ReleaseFolderRegexPattern)); }
        }
    }
}
