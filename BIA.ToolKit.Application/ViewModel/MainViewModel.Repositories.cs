namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;

    public partial class MainViewModel
    {
        private bool waitAddTemplateRepository;
        private bool waitAddCompanyFilesRepository;

        public ObservableCollection<RepositoryViewModel> TemplateRepositories { get; } = [];
        public ObservableCollection<RepositoryViewModel> CompanyFilesRepositories { get; } = [];

        [ObservableProperty]
        private RepositoryViewModel toolkitRepository;

        // --- Validation ---

        public bool EnsureValidRepositoriesConfiguration()
        {
            bool templatesValid = CheckTemplateRepositories(settingsService.Settings);
            bool companyFilesValid = CheckCompanyFilesRepositories(settingsService.Settings);

            if (!templatesValid || !companyFilesValid)
            {
                WeakReferenceMessenger.Default.Send(new NavigateToConfigTabMessage());
            }

            return templatesValid && companyFilesValid;
        }

        public bool CheckTemplateRepositories(IBIATKSettings biaTKsettings)
        {
            if (!biaTKsettings.TemplateRepositories.Where(r => r.UseRepository).Any())
            {
                consoleWriter.AddMessageLine("You must use at least one Templates repository", "red");
                return false;
            }

            foreach (IRepository repository in biaTKsettings.TemplateRepositories.Where(r => r.UseRepository))
            {
                if (!repositoryService.CheckRepoFolder(repository))
                {
                    return false;
                }
            }

            IRepository repositoryVersionXYZ = biaTKsettings.TemplateRepositories.FirstOrDefault(r => r is RepositoryGit repoGit && repoGit.IsVersionXYZ);
            if (repositoryVersionXYZ is not null)
            {
                if (!repositoryService.CheckRepoFolder(repositoryVersionXYZ))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CheckCompanyFilesRepositories(IBIATKSettings biaTKsettings)
        {
            if (biaTKsettings.UseCompanyFiles)
            {
                if (!biaTKsettings.CompanyFilesRepositories.Where(r => r.UseRepository).Any())
                {
                    consoleWriter.AddMessageLine("You must use at least one Company Files repository", "red");
                    return false;
                }

                foreach (IRepository repository in biaTKsettings.CompanyFilesRepositories.Where(r => r.UseRepository))
                {
                    if (!repositoryService.CheckRepoFolder(repository))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // --- Repository CRUD ---

        private void OnRepositoryAdded(RepositoryViewModel repository)
        {
            if (waitAddTemplateRepository)
            {
                TemplateRepositories.Add(repository);
            }

            if (waitAddCompanyFilesRepository)
            {
                CompanyFilesRepositories.Add(repository);
            }

            waitAddTemplateRepository = false;
            waitAddCompanyFilesRepository = false;

            if (repository.Model.RepositoryType == RepositoryType.Git && repository.Model is IRepositoryGit repositoryGit)
            {
                WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await gitService.Synchronize(repositoryGit, ct)));
            }
        }

        private void OnRepositoryDeleted(RepositoryViewModel repository)
        {
            for (int i = 0; i < TemplateRepositories.Count; i++)
            {
                if (TemplateRepositories[i] == repository)
                {
                    TemplateRepositories.RemoveAt(i);
                }
            }

            for (int i = 0; i < CompanyFilesRepositories.Count; i++)
            {
                if (CompanyFilesRepositories[i] == repository)
                {
                    CompanyFilesRepositories.RemoveAt(i);
                }
            }

            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                consoleWriter.AddMessageLine($"Deleting repository {repository.Name}...", "pink");
                await Task.Run(repository.Model.Clean, ct);
                consoleWriter.AddMessageLine("Repository deleted", "green");
            }));
        }

        private void OnRepositoryChanged(RepositoryViewModel oldRepository, RepositoryViewModel newRepository)
        {
            for (int i = 0; i < TemplateRepositories.Count; i++)
            {
                if (TemplateRepositories[i] == oldRepository)
                {
                    TemplateRepositories.RemoveAt(i);
                    TemplateRepositories.Insert(i, newRepository);
                }
            }

            for (int i = 0; i < CompanyFilesRepositories.Count; i++)
            {
                if (CompanyFilesRepositories[i] == oldRepository)
                {
                    CompanyFilesRepositories.RemoveAt(i);
                    CompanyFilesRepositories.Insert(i, newRepository);
                }
            }

            if (ToolkitRepository == oldRepository)
            {
                ToolkitRepository = newRepository;
                settingsService.SetToolkitRepository(ToolkitRepository.Model);
            }
        }

        private void OnRepositoriesUpdated()
        {
            WeakReferenceMessenger.Default.Send(new SettingsUpdatedMessage(settingsService.Settings));
        }

        private void OnOpenRepositoryForm(RepositoryViewModel repository, RepositoryFormMode mode)
        {
            RepositoryViewModel result = dialogService.ShowRepositoryForm(repository);
            if (result is null)
                return;

            switch (mode)
            {
                case RepositoryFormMode.Edit:
                    WeakReferenceMessenger.Default.Send(new RepositoryChangedMessage(repository, result));
                    break;
                case RepositoryFormMode.Create:
                    WeakReferenceMessenger.Default.Send(new RepositoryAddedMessage(result));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void CompanyFilesRepositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settingsService.SetCompanyFilesRepositories([.. CompanyFilesRepositories.Select(x => x.Model)]);
        }

        private void TemplateRepositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settingsService.SetTemplateRepositories([.. TemplateRepositories.Select(x => x.Model)]);
        }

        [RelayCommand]
        private void OpenToolkitRepositorySettings() => WeakReferenceMessenger.Default.Send(new OpenRepositoryFormMessage(ToolkitRepository, RepositoryFormMode.Edit));

        [RelayCommand]
        private void AddTemplateRepository()
        {
            waitAddTemplateRepository = true;
            waitAddCompanyFilesRepository = false;
            WeakReferenceMessenger.Default.Send(new OpenRepositoryFormMessage(new RepositoryGitViewModel(RepositoryGit.CreateEmpty(), gitService, consoleWriter), RepositoryFormMode.Create));
        }

        [RelayCommand]
        private void AddCompanyFilesRepository()
        {
            waitAddCompanyFilesRepository = true;
            waitAddTemplateRepository = false;
            WeakReferenceMessenger.Default.Send(new OpenRepositoryFormMessage(new RepositoryGitViewModel(RepositoryGit.CreateEmpty(), gitService, consoleWriter), RepositoryFormMode.Create));
        }

        public void UpdateRepositories(IBIATKSettings settings)
        {
            TemplateRepositories.CollectionChanged -= TemplateRepositories_CollectionChanged;
            CompanyFilesRepositories.CollectionChanged -= CompanyFilesRepositories_CollectionChanged;

            TemplateRepositories.Clear();
            foreach (IRepository repository in settings.TemplateRepositories)
            {
                if (repository is RepositoryGit repositoryGit)
                {
                    var viewModel = new RepositoryGitViewModel(repositoryGit, gitService, consoleWriter);
                    TemplateRepositories.Add(viewModel);
                }

                if (repository is RepositoryFolder repositoryFolder)
                {
                    TemplateRepositories.Add(new RepositoryFolderViewModel(repositoryFolder, gitService, consoleWriter));
                }
            }

            CompanyFilesRepositories.Clear();
            foreach (IRepository repository in settings.CompanyFilesRepositories)
            {
                if (repository is RepositoryGit repositoryGit)
                {
                    CompanyFilesRepositories.Add(new RepositoryGitViewModel(repositoryGit, gitService, consoleWriter));
                }

                if (repository is RepositoryFolder repositoryFolder)
                {
                    CompanyFilesRepositories.Add(new RepositoryFolderViewModel(repositoryFolder, gitService, consoleWriter));
                }
            }

            ToolkitRepository = settings.ToolkitRepository switch
            {
                RepositoryGit repositoryGit => new RepositoryGitViewModel(repositoryGit, gitService, consoleWriter),
                RepositoryFolder repositoryFolder => new RepositoryFolderViewModel(repositoryFolder, gitService, consoleWriter),
                _ => throw new NotImplementedException()
            };
            ToolkitRepository.IsVisibleCompanyName = false;
            ToolkitRepository.IsVisibleProjectName = false;

            TemplateRepositories.CollectionChanged += TemplateRepositories_CollectionChanged;
            CompanyFilesRepositories.CollectionChanged += CompanyFilesRepositories_CollectionChanged;
        }
    }
}
