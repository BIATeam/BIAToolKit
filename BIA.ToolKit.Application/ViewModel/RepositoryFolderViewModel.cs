namespace BIA.ToolKit.Application.ViewModel
{
    using System.IO;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Domain;

    public sealed partial class RepositoryFolderViewModel(RepositoryFolder repositoryFolder, GitService gitService, IConsoleWriter consoleWriter)
        : RepositoryViewModel(repositoryFolder, gitService, consoleWriter)
    {
        public string Path
        {
            get => repositoryFolder.Path;
            set
            {
                repositoryFolder.Path = value; OnPropertyChanged(nameof(Path));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public string ReleasesFolderRegexPattern
        {
            get => repositoryFolder.ReleasesFolderRegexPattern;
            set { repositoryFolder.ReleasesFolderRegexPattern = value; OnPropertyChanged(nameof(ReleasesFolderRegexPattern)); }
        }

        protected override bool EnsureIsValid()
        {
            return !string.IsNullOrWhiteSpace(Path) && Directory.Exists(Path);
        }
    }
}
