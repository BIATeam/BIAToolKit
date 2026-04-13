namespace BIA.ToolKit.ViewModels
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Messages;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Messaging;

    public partial class MainViewModel : ObservableObject, IDisposable,
        IRecipient<SettingsUpdatedMessage>,
        IRecipient<RepositoryChangedMessage>,
        IRecipient<RepositoryDeletedMessage>,
        IRecipient<RepositoryAddedMessage>,
        IRecipient<RepositoriesUpdatedMessage>,
        IRecipient<ExecuteActionWithWaiterMessage>,
        IRecipient<NewVersionAvailableMessage>,
        IRecipient<OpenRepositoryFormMessage>
    {
        private readonly Version applicationVersion;
        private readonly SettingsService settingsService;
        private readonly GitService gitService;
        private readonly IConsoleWriter consoleWriter;
        private readonly RepositoryService repositoryService;
        private readonly ProjectCreatorService projectCreatorService;
        private readonly UpdateService updateService;
        private readonly CSharpParserService cSharpParserService;
        private readonly IDialogService dialogService;
        private bool firstTimeSettingsUpdated = true;
        private bool disposed;

        private readonly SemaphoreSlim semaphore = new(1, 1);

        public MainViewModel(
            Version applicationVersion,
            SettingsService settingsService,
            GitService gitService,
            IConsoleWriter consoleWriter,
            RepositoryService repositoryService,
            ProjectCreatorService projectCreatorService,
            UpdateService updateService,
            CSharpParserService cSharpParserService,
            IDialogService dialogService)
        {
            this.applicationVersion = applicationVersion;
            this.settingsService = settingsService;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            this.repositoryService = repositoryService;
            this.projectCreatorService = projectCreatorService;
            this.updateService = updateService;
            this.cSharpParserService = cSharpParserService;
            this.dialogService = dialogService;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        // --- Message handlers ---

        public void Receive(SettingsUpdatedMessage message) => OnSettingsUpdated(message.Settings);
        public void Receive(RepositoryChangedMessage message) => OnRepositoryChanged(message.OldRepository, message.NewRepository);
        public void Receive(RepositoryDeletedMessage message) => OnRepositoryDeleted(message.Repository);
        public void Receive(RepositoryAddedMessage message) => OnRepositoryAdded(message.Repository);
        public void Receive(RepositoriesUpdatedMessage message) => OnRepositoriesUpdated();
        public async void Receive(ExecuteActionWithWaiterMessage message) => await ExecuteWithBusyAsync(message.Action);
        public async void Receive(NewVersionAvailableMessage message) => await OnNewVersionAvailable();
        public void Receive(OpenRepositoryFormMessage message) => OnOpenRepositoryForm(message.Repository, message.Mode);

        // --- IsBusy / Waiter / Init --- (see MainViewModel.BusyState.cs)

        // --- Repositories --- (see MainViewModel.Repositories.cs)

        // --- Settings --- (see MainViewModel.Settings.cs)

        // --- Create project --- (see MainViewModel.CreateProject.cs)

        // --- Update --- (see MainViewModel.Update.cs)

        // --- Console, Browse, Import/Export, Tab selection --- (see MainViewModel.Commands.cs)
    }
}
