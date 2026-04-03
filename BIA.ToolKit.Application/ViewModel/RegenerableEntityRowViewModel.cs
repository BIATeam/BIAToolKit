namespace BIA.ToolKit.Application.ViewModel
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;

    /// <summary>
    /// ViewModel row representing one entity and its regenerable features (CRUD, Option, DTO)
    /// for the RegenerateFeatures tab.
    /// </summary>
    public class RegenerableEntityRowViewModel(RegenerableEntity entity) : INotifyPropertyChanged
    {
        private bool isCrudSelected;
        private bool isOptionSelected;
        private bool isDtoSelected;
        private bool updatingAll;

        // ── Dynamic parent-blocking state (set by RegenerateFeaturesViewModel) ──
        private bool isCrudBlockedByParent;
        private bool isDtoBlockedByParent;
        private string dynamicCrudWarningMessage;
        private string dynamicDtoWarningMessage;

        public event PropertyChangedEventHandler PropertyChanged;

        public RegenerableEntity Entity { get; } = entity;

        public string EntityNameSingular => Entity.EntityNameSingular;
        public string EntityNamePlural => Entity.EntityNamePlural;

        // ── CRUD ──────────────────────────────────────────────────────────────
        public bool HasCrud => Entity.HasCrudHistory;

        /// <summary>True when the CRUD feature can be selected (ready AND not blocked by a parent dependency).</summary>
        public bool IsCrudEnabled => Entity.CanRegenerateCrud && !isCrudBlockedByParent;

        public bool IsCrudSelected
        {
            get => isCrudSelected;
            set
            {
                if (isCrudSelected != value)
                {
                    isCrudSelected = value;
                    OnPropertyChanged();
                    if (!updatingAll) OnFeatureSelectionChanged();
                }
            }
        }

        /// <summary>Warning/blocking message for the CRUD feature (dynamic parent message takes priority over static one).</summary>
        public string CrudWarningMessage =>
            !string.IsNullOrEmpty(dynamicCrudWarningMessage) ? dynamicCrudWarningMessage : Entity.CrudWarningMessage;

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

        /// <summary>True when the DTO feature can be selected (ready AND not blocked by a parent dependency).</summary>
        public bool IsDtoEnabled => Entity.CanRegenerateDto && !isDtoBlockedByParent;

        public bool IsDtoSelected
        {
            get => isDtoSelected;
            set
            {
                if (isDtoSelected != value)
                {
                    isDtoSelected = value;
                    OnPropertyChanged();
                    if (!updatingAll) OnFeatureSelectionChanged();
                }
            }
        }

        /// <summary>Warning/blocking message for the DTO feature (dynamic parent message takes priority over static one).</summary>
        public string DtoWarningMessage =>
            !string.IsNullOrEmpty(dynamicDtoWarningMessage) ? dynamicDtoWarningMessage : Entity.DtoWarningMessage;

        /// <summary>True when there is a DTO warning message to display.</summary>
        public bool HasDtoWarning => !string.IsNullOrEmpty(DtoWarningMessage);

        // ── Entity-level selection ────────────────────────────────────────────
        public bool? IsEntitySelected
        {
            get
            {
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

        // ── Dynamic blocking API (called by RegenerateFeaturesViewModel) ──────

        /// <summary>
        /// Updates the dynamic parent-dependency blocking state for this row's CRUD and DTO features.
        /// Called by <see cref="RegenerateFeaturesViewModel"/> whenever the selection state changes.
        /// </summary>
        /// <param name="crudBlocked">Whether the CRUD checkbox should be blocked.</param>
        /// <param name="dtoBlocked">Whether the DTO checkbox should be blocked.</param>
        /// <param name="message">Warning message to display (null to clear).</param>
        public void SetParentBlocking(bool crudBlocked, bool dtoBlocked, string message)
        {
            bool crudChanged = isCrudBlockedByParent != crudBlocked;
            bool dtoChanged = isDtoBlockedByParent != dtoBlocked;
            string newCrudMsg = crudBlocked ? message : null;
            string newDtoMsg = dtoBlocked ? message : null;
            bool crudMsgChanged = dynamicCrudWarningMessage != newCrudMsg;
            bool dtoMsgChanged = dynamicDtoWarningMessage != newDtoMsg;

            isCrudBlockedByParent = crudBlocked;
            isDtoBlockedByParent = dtoBlocked;
            dynamicCrudWarningMessage = newCrudMsg;
            dynamicDtoWarningMessage = newDtoMsg;

            if (crudChanged)
            {
                OnPropertyChanged(nameof(IsCrudEnabled));
                OnPropertyChanged(nameof(IsEntitySelected));
            }

            if (dtoChanged)
            {
                OnPropertyChanged(nameof(IsDtoEnabled));
                OnPropertyChanged(nameof(IsEntitySelected));
            }

            if (crudMsgChanged)
            {
                OnPropertyChanged(nameof(CrudWarningMessage));
                OnPropertyChanged(nameof(HasCrudWarning));
            }

            if (dtoMsgChanged)
            {
                OnPropertyChanged(nameof(DtoWarningMessage));
                OnPropertyChanged(nameof(HasDtoWarning));
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void OnFeatureSelectionChanged()
        {
            OnPropertyChanged(nameof(IsEntitySelected));
            SelectionChanged?.Invoke(this, System.EventArgs.Empty);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
