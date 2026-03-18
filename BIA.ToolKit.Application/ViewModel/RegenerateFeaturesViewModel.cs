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

        public Project CurrentProject { get; private set; }

        /// <summary>
        /// Load entities discovered from the project history files and the list of available framework versions.
        /// Only entities whose stored framework version is lower than the current project version (or has no stored version) are shown.
        /// </summary>
        public void Initialize(Project project, IEnumerable<RegenerableEntity> allEntities, IEnumerable<string> availableVersions)
        {
            CurrentProject = project;
            EntityRows.Clear();
            SelectedFeatures.Clear();

            var versions = availableVersions.ToList();
            var projectVersion = ParseVersion(project?.FrameworkVersion);

            foreach (var entity in allEntities)
            {
                // Show only entities that have at least one feature where the stored version is
                // lower than the current project version, or no version is stored.
                bool showCrud = entity.HasCrudHistory && IsEligible(entity.CrudHistory?.FrameworkVersion, projectVersion);
                bool showOption = entity.HasOptionHistory && IsEligible(entity.OptionHistory?.FrameworkVersion, projectVersion);
                bool showDto = entity.HasDtoHistory && IsEligible(entity.DtoHistory?.FrameworkVersion, projectVersion);

                if (!showCrud && !showOption && !showDto)
                    continue;

                var row = new RegenerableEntityRowViewModel(entity)
                {
                    AvailableVersions = new ObservableCollection<string>(versions),
                };
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
            SelectedFeatures.Clear();

            // Ordered by dependency: DTO first, then Option, then CRUD
            foreach (var row in EntityRows.OrderBy(r => r.EntityNameSingular))
            {
                if (row.IsDtoEnabled && row.IsDtoSelected)
                    SelectedFeatures.Add(new FeatureRegenerationItem { EntityNameSingular = row.EntityNameSingular, FeatureType = "DTO", FromVersion = row.EffectiveDtoFromVersion, ToVersion = CurrentProject?.FrameworkVersion });

                if (row.IsOptionEnabled && row.IsOptionSelected)
                    SelectedFeatures.Add(new FeatureRegenerationItem { EntityNameSingular = row.EntityNameSingular, FeatureType = "Option", FromVersion = row.EffectiveOptionFromVersion, ToVersion = CurrentProject?.FrameworkVersion });

                if (row.IsCrudEnabled && row.IsCrudSelected)
                    SelectedFeatures.Add(new FeatureRegenerationItem { EntityNameSingular = row.EntityNameSingular, FeatureType = "CRUD", FromVersion = row.EffectiveCrudFromVersion, ToVersion = CurrentProject?.FrameworkVersion });
            }

            RaisePropertyChanged(nameof(HasSelectedFeatures));
            RaisePropertyChanged(nameof(SelectedFeatures));
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static bool IsEligible(string storedVersion, Version projectVersion)
        {
            if (string.IsNullOrEmpty(storedVersion))
                return true; // No stored version → always eligible

            var sv = ParseVersion(storedVersion);
            return sv == null || projectVersion == null || sv < projectVersion;
        }

        private static Version ParseVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return null;
            // Strip leading 'V' / 'v'
            var v = version.TrimStart('v', 'V');
            return System.Version.TryParse(v, out var result) ? result : null;
        }
    }
}
