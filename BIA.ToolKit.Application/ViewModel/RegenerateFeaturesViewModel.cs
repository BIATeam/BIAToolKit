namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
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
            private set { isLoaded = value; RaisePropertyChanged(nameof(IsLoaded)); RaisePropertyChanged(nameof(HasEntities)); RaisePropertyChanged(nameof(NoEntities)); }
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
                RaisePropertyChanged(nameof(SelectAllEntities));
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
            RaisePropertyChanged(nameof(EntityRows));
        }

        /// <summary>Refresh the summary list of features to regenerate from the current checkbox state.</summary>
        public void RefreshSelectedFeatures()
        {
            // 1. Re-evaluate dynamic parent-dependency blocking based on current selection
            RefreshParentDependencyBlocking();

            // 2. Unsubscribe previous items before clearing to avoid stale handlers
            foreach (FeatureRegenerationItem item in SelectedFeatures)
                item.PropertyChanged -= OnFeatureItemPropertyChanged;

            SelectedFeatures.Clear();

            // 3. All selected Options first (alphabetical by entity name)
            foreach (RegenerableEntityRowViewModel row in EntityRows.OrderBy(r => r.EntityNameSingular))
            {
                if (row.IsOptionEnabled && row.IsOptionSelected)
                    SelectedFeatures.Add(BuildItem(row.EntityNameSingular, "Option", row.Entity.OptionHistory?.FrameworkVersion));
            }

            // 4. Collect rows that have at least one of CRUD or DTO selected
            List<RegenerableEntityRowViewModel> crudDtoRows = EntityRows
                .Where(r => (r.IsCrudEnabled && r.IsCrudSelected) || (r.IsDtoEnabled && r.IsDtoSelected))
                .ToList();

            // 5. Sort them in dependency order (parents before children, alphabetical within same level)
            IEnumerable<RegenerableEntityRowViewModel> sortedRows = TopologicalSort(crudDtoRows);

            // 6. For each entity in dependency order: DTO first, then CRUD
            foreach (RegenerableEntityRowViewModel row in sortedRows)
            {
                if (row.IsDtoEnabled && row.IsDtoSelected)
                    SelectedFeatures.Add(BuildItem(row.EntityNameSingular, "DTO", row.Entity.DtoHistory?.FrameworkVersion));

                if (row.IsCrudEnabled && row.IsCrudSelected)
                    SelectedFeatures.Add(BuildItem(row.EntityNameSingular, "CRUD", row.Entity.CrudHistory?.FrameworkVersion));
            }

            // 7. Subscribe to new items so CanRegenerate updates when user picks a FROM version
            foreach (FeatureRegenerationItem item in SelectedFeatures)
                item.PropertyChanged += OnFeatureItemPropertyChanged;

            RaisePropertyChanged(nameof(HasSelectedFeatures));
            RaisePropertyChanged(nameof(CanRegenerate));
            RaisePropertyChanged(nameof(SelectAllEntities));
            RaisePropertyChanged(nameof(SelectedFeatures));
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Updates the dynamic parent-blocking state on each child row based on the current
        /// selection state of their parent row.
        /// A child's CRUD/DTO is blocked when its parent has enabled-but-unselected features.
        /// </summary>
        private void RefreshParentDependencyBlocking()
        {
            var rowLookup = EntityRows.ToDictionary(r => r.EntityNameSingular, StringComparer.OrdinalIgnoreCase);

            foreach (RegenerableEntityRowViewModel row in EntityRows)
            {
                string parentName = row.Entity.ParentEntityName;

                if (string.IsNullOrEmpty(parentName) || !row.Entity.HasParentDependency)
                {
                    // No dynamic parent dependency — clear any previous blocking
                    row.SetParentBlocking(false, false, null);
                    continue;
                }

                if (!rowLookup.TryGetValue(parentName, out RegenerableEntityRowViewModel parentRow))
                {
                    // Parent not in displayed rows (e.g. already up to date) — no blocking needed
                    row.SetParentBlocking(false, false, null);
                    continue;
                }

                // Parent is in rows — it must be fully selected (all enabled features selected)
                bool parentFullySelected =
                    (!parentRow.IsCrudEnabled || parentRow.IsCrudSelected) &&
                    (!parentRow.IsDtoEnabled || parentRow.IsDtoSelected);

                if (parentFullySelected)
                {
                    row.SetParentBlocking(false, false, null);
                }
                else
                {
                    string message = $"L'entité parente '{parentName}' doit être entièrement sélectionnée pour migration en premier.";
                    row.SetParentBlocking(crudBlocked: true, dtoBlocked: true, message: message);
                }
            }
        }

        /// <summary>
        /// Returns the rows sorted in dependency order: parents before their children.
        /// Rows without a parent (or whose parent is not in the list) appear first, sorted alphabetically.
        /// </summary>
        private static IEnumerable<RegenerableEntityRowViewModel> TopologicalSort(List<RegenerableEntityRowViewModel> rows)
        {
            var lookup = rows.ToDictionary(r => r.EntityNameSingular, StringComparer.OrdinalIgnoreCase);
            var result = new List<RegenerableEntityRowViewModel>(rows.Count);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Visit(RegenerableEntityRowViewModel row)
            {
                if (visited.Contains(row.EntityNameSingular))
                    return;

                visited.Add(row.EntityNameSingular);

                // Visit the parent first (if it is among the selected rows)
                string parentName = row.Entity.ParentEntityName;
                if (!string.IsNullOrEmpty(parentName) && lookup.TryGetValue(parentName, out RegenerableEntityRowViewModel parentRow))
                    Visit(parentRow);

                result.Add(row);
            }

            // Iterate in alphabetical order so siblings appear in a predictable order
            foreach (RegenerableEntityRowViewModel row in rows.OrderBy(r => r.EntityNameSingular))
                Visit(row);

            return result;
        }

        private void OnFeatureItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FeatureRegenerationItem.EffectiveFromVersion))
                RaisePropertyChanged(nameof(CanRegenerate));
        }

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
