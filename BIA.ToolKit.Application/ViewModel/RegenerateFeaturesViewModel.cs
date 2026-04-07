namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using CommunityToolkit.Mvvm.ComponentModel;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;

    /// <summary>
    /// ViewModel for the "Regenerate Features" tab in ModifyProject.
    /// </summary>
    public class RegenerateFeaturesViewModel : ObservableObject
    {
        private bool isLoaded;
        private List<string> availableVersions = [];

        public ObservableCollection<RegenerableEntityRowViewModel> EntityRows { get; } = [];
        public ObservableCollection<FeatureRegenerationItem> SelectedFeatures { get; } = [];

        public bool IsLoaded
        {
            get => isLoaded;
            private set { isLoaded = value; OnPropertyChanged(nameof(IsLoaded)); OnPropertyChanged(nameof(HasEntities)); OnPropertyChanged(nameof(NoEntities)); }
        }

        public bool HasEntities => IsLoaded && EntityRows.Count > 0;
        public bool NoEntities => IsLoaded && EntityRows.Count == 0;
        public bool HasSelectedFeatures => SelectedFeatures.Count > 0;

        /// <summary>True when all selected features have an effective FROM version filled in.</summary>
        public bool CanRegenerate => SelectedFeatures.Count > 0
            && SelectedFeatures.All(f => !string.IsNullOrEmpty(f.EffectiveFromVersion));

        /// <summary>
        /// Three-state select-all for the entity list header.
        /// Returns true when all enabled rows are fully selected, false when none are, null otherwise.
        /// Setting to true selects all; setting to false (or null) deselects all.
        /// </summary>
        public bool? SelectAllEntities
        {
            get
            {
                if (EntityRows.Count == 0) return false;
                bool allSel = EntityRows.All(r => r.IsEntitySelected == true);
                bool noneSel = EntityRows.All(r => r.IsEntitySelected == false);
                return allSel ? true : noneSel ? false : (bool?)null;
            }
            set
            {
                bool select = value == true;
                foreach (RegenerableEntityRowViewModel row in EntityRows)
                    row.IsEntitySelected = select;
                OnPropertyChanged(nameof(SelectAllEntities));
            }
        }

        public Project CurrentProject { get; private set; }

        /// <summary>
        /// Load entities discovered from the project history files and the list of available framework versions.
        /// Only entities whose stored framework version is lower than the current project version (or has no stored version) are shown.
        /// </summary>
        public void Initialize(Project project, IEnumerable<RegenerableEntity> allEntities, IEnumerable<string> versions)
        {
            CurrentProject = project;
            availableVersions = [.. versions];
            EntityRows.Clear();
            SelectedFeatures.Clear();

            Version projectVersion = ParseVersion(project?.FrameworkVersion);

            foreach (RegenerableEntity entity in allEntities)
            {
                // Show only entities that have at least one feature where the stored version is
                // lower than the current project version, or no version is stored.
                bool showCrud = entity.HasCrudHistory && IsEligible(entity.CrudHistory?.FrameworkVersion, projectVersion);
                bool showOption = entity.HasOptionHistory && IsEligible(entity.OptionHistory?.FrameworkVersion, projectVersion);
                bool showDto = entity.HasDtoHistory && IsEligible(entity.DtoHistory?.FrameworkVersion, projectVersion);

                if (!showCrud && !showOption && !showDto)
                    continue;

                var row = new RegenerableEntityRowViewModel(entity);
                row.SelectionChanged += (_, _) => RefreshSelectedFeatures();
                EntityRows.Add(row);
            }

            IsLoaded = true;
            RefreshSelectedFeatures();
            OnPropertyChanged(nameof(EntityRows));
        }

        /// <summary>Refresh the summary list of features to regenerate from the current checkbox state.</summary>
        public void RefreshSelectedFeatures()
        {
            // Unsubscribe previous items before clearing to avoid stale handlers
            foreach (FeatureRegenerationItem item in SelectedFeatures)
                item.PropertyChanged -= OnFeatureItemPropertyChanged;

            SelectedFeatures.Clear();

            // Ordered by dependency: DTO first, then Option, then CRUD
            foreach (RegenerableEntityRowViewModel row in EntityRows.OrderBy(r => r.EntityNameSingular))
            {
                if (row.IsDtoEnabled && row.IsDtoSelected)
                    SelectedFeatures.Add(BuildItem(row.EntityNameSingular, "DTO", row.Entity.DtoHistory?.FrameworkVersion));

                if (row.IsOptionEnabled && row.IsOptionSelected)
                    SelectedFeatures.Add(BuildItem(row.EntityNameSingular, "Option", row.Entity.OptionHistory?.FrameworkVersion));

                if (row.IsCrudEnabled && row.IsCrudSelected)
                    SelectedFeatures.Add(BuildItem(row.EntityNameSingular, "CRUD", row.Entity.CrudHistory?.FrameworkVersion));
            }

            // Subscribe to new items so CanRegenerate updates when user picks a FROM version
            foreach (FeatureRegenerationItem item in SelectedFeatures)
                item.PropertyChanged += OnFeatureItemPropertyChanged;

            OnPropertyChanged(nameof(HasSelectedFeatures));
            OnPropertyChanged(nameof(CanRegenerate));
            OnPropertyChanged(nameof(SelectAllEntities));
            OnPropertyChanged(nameof(SelectedFeatures));
        }

        private void OnFeatureItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FeatureRegenerationItem.EffectiveFromVersion))
                OnPropertyChanged(nameof(CanRegenerate));
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private FeatureRegenerationItem BuildItem(string entityName, string featureType, string storedVersion)
        {
            return new FeatureRegenerationItem
            {
                EntityNameSingular = entityName,
                FeatureType = featureType,
                StoredFromVersion = storedVersion,
                ToVersion = CurrentProject?.FrameworkVersion,
                AvailableVersions = new ObservableCollection<string>(availableVersions),
            };
        }

        private static bool IsEligible(string storedVersion, Version projectVersion)
        {
            if (string.IsNullOrEmpty(storedVersion))
                return true; // No stored version → always eligible

            Version sv = ParseVersion(storedVersion);
            return sv == null || projectVersion == null || sv < projectVersion;
        }

        private static Version ParseVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return null;
            // Strip leading 'V' / 'v'
            string v = version.TrimStart('v', 'V');
            return System.Version.TryParse(v, out Version result) ? result : null;
        }
    }
}
