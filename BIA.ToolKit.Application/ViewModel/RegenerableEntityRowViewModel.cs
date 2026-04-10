namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;
    using CommunityToolkit.Mvvm.ComponentModel;

    /// <summary>
    /// ViewModel row representing one entity and its regenerable features (CRUD, Option, DTO)
    /// for the RegenerateFeatures tab.
    /// </summary>
    public class RegenerableEntityRowViewModel(RegenerableEntity entity) : ObservableObject
    {
        private bool isCrudSelected;
        private bool isOptionSelected;
        private bool isDtoSelected;
        private bool updatingAll;

        public RegenerableEntity Entity { get; } = entity;

        public string EntityNameSingular => Entity.EntityNameSingular;
        public string EntityNamePlural => Entity.EntityNamePlural;

        // ── CRUD ──────────────────────────────────────────────────────────────
        public bool HasCrud => Entity.HasCrudHistory;

        /// <summary>True when the CRUD feature can be selected (entity status is Ready).</summary>
        public bool IsCrudEnabled => Entity.CanRegenerateCrud;

        public bool IsCrudSelected
        {
            get => isCrudSelected;
            set
            {
                if (isCrudSelected != value)
                {
                    isCrudSelected = value;
                    OnPropertyChanged();

                    // CRUD → DTO coupling: selecting CRUD auto-selects and locks DTO.
                    if (value && Entity.CanRegenerateDto)
                    {
                        if (!isDtoSelected)
                        {
                            isDtoSelected = true;
                            OnPropertyChanged(nameof(IsDtoSelected));
                        }
                    }

                    // IsDtoEnabled depends on isCrudSelected — notify UI
                    OnPropertyChanged(nameof(IsDtoEnabled));
                    OnPropertyChanged(nameof(IsEntitySelected));

                    if (!updatingAll) OnFeatureSelectionChanged();
                }
            }
        }

        /// <summary>Warning/blocking message for the CRUD feature.</summary>
        public string CrudWarningMessage => Entity.CrudWarningMessage;

        /// <summary>True when there is a CRUD warning message to display.</summary>
        public bool HasCrudWarning => !string.IsNullOrEmpty(CrudWarningMessage);

        // ── Option ────────────────────────────────────────────────────────────
        public bool HasOption => Entity.HasOptionHistory;
        public bool IsOptionEnabled => Entity.CanRegenerateOption;

        public bool IsOptionSelected
        {
            get => isOptionSelected;
            set
            {
                if (isOptionSelected != value)
                {
                    isOptionSelected = value;
                    OnPropertyChanged();
                    if (!updatingAll) OnFeatureSelectionChanged();
                }
            }
        }

        /// <summary>Informational warning message for option dependencies.</summary>
        public string OptionWarningMessage => Entity.OptionWarningMessage;

        /// <summary>True when there is an option warning message to display.</summary>
        public bool HasOptionWarning => !string.IsNullOrEmpty(OptionWarningMessage);

        // ── DTO ───────────────────────────────────────────────────────────────
        public bool HasDto => Entity.HasDtoHistory;

        /// <summary>
        /// True when the DTO feature can be interacted with.
        /// Returns <see langword="false"/> when DTO is not Ready OR when DTO is locked because CRUD is selected
        /// (in which case the DTO checkbox is shown as checked-and-disabled to indicate it is required by CRUD).
        /// </summary>
        public bool IsDtoEnabled => Entity.CanRegenerateDto && !isCrudSelected;

        public bool IsDtoSelected
        {
            get => isDtoSelected;
            set
            {
                // While CRUD is selected, DTO is locked — silently ignore any attempt to deselect it
                if (!value && isCrudSelected && Entity.CanRegenerateDto)
                    return;

                if (isDtoSelected != value)
                {
                    isDtoSelected = value;
                    OnPropertyChanged();
                    if (!updatingAll) OnFeatureSelectionChanged();
                }
            }
        }

        /// <summary>Warning/blocking message for the DTO feature.</summary>
        public string DtoWarningMessage => Entity.DtoWarningMessage;

        /// <summary>True when there is a DTO warning message to display.</summary>
        public bool HasDtoWarning => !string.IsNullOrEmpty(DtoWarningMessage);

        // ── Entity-level selection ────────────────────────────────────────────

        /// <summary>
        /// Three-state: <see langword="true"/> when all enabled features are selected,
        /// <see langword="false"/> when none are (or when no features are enabled),
        /// <see langword="null"/> when some but not all are selected.
        /// </summary>
        public bool? IsEntitySelected
        {
            get
            {
                bool anyEnabled = IsCrudEnabled || IsOptionEnabled || IsDtoEnabled;
                if (!anyEnabled) return false;

                bool anySel = (IsCrudEnabled && IsCrudSelected) || (IsOptionEnabled && IsOptionSelected) || (IsDtoEnabled && IsDtoSelected);
                bool allSel = (!IsCrudEnabled || IsCrudSelected) && (!IsOptionEnabled || IsOptionSelected) && (!IsDtoEnabled || IsDtoSelected);
                return allSel ? true : anySel ? null : (bool?)false;
            }
            set
            {
                bool select = value == true;
                updatingAll = true;
                if (IsCrudEnabled) IsCrudSelected = select;
                if (IsOptionEnabled) IsOptionSelected = select;
                if (IsDtoEnabled) IsDtoSelected = select;
                updatingAll = false;
                OnPropertyChanged();
                OnFeatureSelectionChanged();
            }
        }

        /// <summary>Raised when any per-feature selection changes so the parent VM can refresh the summary.</summary>
        public event System.EventHandler SelectionChanged;

        // ── Helpers ───────────────────────────────────────────────────────────

        private void OnFeatureSelectionChanged()
        {
            OnPropertyChanged(nameof(IsEntitySelected));
            SelectionChanged?.Invoke(this, System.EventArgs.Empty);
        }
    }
}
