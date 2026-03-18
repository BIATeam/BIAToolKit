namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.ModifyProject;

    /// <summary>
    /// ViewModel specific to the ModifyProject tab (migration).
    /// Reacts to project changes via <see cref="UIEventBroker.OnProjectChanged"/>.
    /// </summary>
    public class ModifyProjectViewModel : ObservableObject
    {
        public ModifyProjectViewModel(UIEventBroker uiEventBroker)
        {
            ModifyProject = new ModifyProject();
            OverwriteBIAFromOriginal = true;
            uiEventBroker.OnProjectChanged += UiEventBroker_OnProjectChanged;
        }

        private void UiEventBroker_OnProjectChanged(Project project)
        {
            ModifyProject.CurrentProject = project;
            RaisePropertyChanged(nameof(IsProjectSelected));
            RaisePropertyChanged(nameof(IsProjectCompatibleRegenerateFeatures));
        }

        // ── Domain object used by migration code-behind ──────────────────────
        public ModifyProject ModifyProject { get; set; }

        // ── Computed from ModifyProject.CurrentProject ───────────────────────
        public Project CurrentProject => ModifyProject.CurrentProject;
        public string Name => ModifyProject.CurrentProject?.Name ?? "???";
        public string CompanyName => ModifyProject.CurrentProject?.CompanyName ?? "???";
        public bool IsProjectSelected => ModifyProject.CurrentProject != null;
        public bool IsProjectCompatibleRegenerateFeatures =>
            GenerateCrudService.IsProjectCompatibleForRegenerateFeatures(ModifyProject.CurrentProject);

        // ── Migration-specific state ─────────────────────────────────────────
        public bool OverwriteBIAFromOriginal { get; set; }
        public bool IncludeFeatureMigration { get; set; }

        private int selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set
            {
                selectedTabIndex = value;
                RaisePropertyChanged(nameof(SelectedTabIndex));
            }
        }
    }
}
