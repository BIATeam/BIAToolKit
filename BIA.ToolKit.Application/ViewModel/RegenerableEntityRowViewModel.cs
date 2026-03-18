namespace BIA.ToolKit.Application.ViewModel
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;

    /// <summary>
    /// ViewModel row representing one entity and its regenerable features (CRUD, Option, DTO)
    /// for the RegenerateFeatures tab.
    /// </summary>
    public class RegenerableEntityRowViewModel : INotifyPropertyChanged
    {
        private bool isCrudSelected;
        private bool isOptionSelected;
        private bool isDtoSelected;
        private string crudVersionOverride;
        private string optionVersionOverride;
        private string dtoVersionOverride;
        private bool updatingAll;

        public event PropertyChangedEventHandler PropertyChanged;

        public RegenerableEntity Entity { get; }
        public ObservableCollection<string> AvailableVersions { get; set; } = [];

        public string EntityNameSingular => Entity.EntityNameSingular;
        public string EntityNamePlural => Entity.EntityNamePlural;

        // ── CRUD ──────────────────────────────────────────────────────────────
        public bool HasCrud => Entity.HasCrudHistory;
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
                    if (!updatingAll) OnFeatureSelectionChanged();
                }
            }
        }

        public string CrudStoredVersion => Entity.CrudHistory?.FrameworkVersion;
        public bool IsCrudVersionStoredInHistory => !string.IsNullOrEmpty(CrudStoredVersion);
        public bool IsCrudVersionNotStoredInHistory => string.IsNullOrEmpty(CrudStoredVersion);

        public string CrudVersionOverride
        {
            get => crudVersionOverride;
            set
            {
                if (crudVersionOverride != value)
                {
                    crudVersionOverride = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EffectiveCrudFromVersion));
                }
            }
        }

        public string EffectiveCrudFromVersion => CrudStoredVersion ?? CrudVersionOverride;

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

        public string OptionStoredVersion => Entity.OptionHistory?.FrameworkVersion;
        public bool IsOptionVersionStoredInHistory => !string.IsNullOrEmpty(OptionStoredVersion);
        public bool IsOptionVersionNotStoredInHistory => string.IsNullOrEmpty(OptionStoredVersion);

        public string OptionVersionOverride
        {
            get => optionVersionOverride;
            set
            {
                if (optionVersionOverride != value)
                {
                    optionVersionOverride = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EffectiveOptionFromVersion));
                }
            }
        }

        public string EffectiveOptionFromVersion => OptionStoredVersion ?? OptionVersionOverride;

        // ── DTO ───────────────────────────────────────────────────────────────
        public bool HasDto => Entity.HasDtoHistory;
        public bool IsDtoEnabled => Entity.CanRegenerateDto;

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

        public string DtoStoredVersion => Entity.DtoHistory?.FrameworkVersion;
        public bool IsDtoVersionStoredInHistory => !string.IsNullOrEmpty(DtoStoredVersion);
        public bool IsDtoVersionNotStoredInHistory => string.IsNullOrEmpty(DtoStoredVersion);

        public string DtoVersionOverride
        {
            get => dtoVersionOverride;
            set
            {
                if (dtoVersionOverride != value)
                {
                    dtoVersionOverride = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EffectiveDtoFromVersion));
                }
            }
        }

        public string EffectiveDtoFromVersion => DtoStoredVersion ?? DtoVersionOverride;

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

        public RegenerableEntityRowViewModel(RegenerableEntity entity)
        {
            Entity = entity;
        }

        private void OnFeatureSelectionChanged()
        {
            OnPropertyChanged(nameof(IsEntitySelected));
            SelectionChanged?.Invoke(this, System.EventArgs.Empty);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
