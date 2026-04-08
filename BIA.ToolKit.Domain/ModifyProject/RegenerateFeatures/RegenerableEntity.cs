namespace BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;

    public class RegenerableEntity : INotifyPropertyChanged
    {
        private bool isSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        public string EntityNameSingular { get; set; }
        public string EntityNamePlural { get; set; }

        public CRUDGenerationHistory CrudHistory { get; set; }
        public OptionGenerationHistory OptionHistory { get; set; }
        public DtoGeneration DtoHistory { get; set; }

        public RegenerableFeatureStatus CrudStatus { get; set; }
        public RegenerableFeatureStatus OptionStatus { get; set; }
        public RegenerableFeatureStatus DtoStatus { get; set; }

        // ── Parsed entity information (populated when CSharpParserService has loaded the solution) ──

        /// <summary>
        /// Domain entity info resolved via <c>CSharpParserService.GetDomainEntities</c>.
        /// Used to supply complete entity data (e.g. <see cref="EntityInfo.BaseKeyType"/>) to the
        /// Option feature generator, which does not store that information in history.
        /// Null when the parser has not yet parsed the solution.
        /// </summary>
        public EntityInfo OptionEntityInfo { get; set; }

        /// <summary>
        /// Domain entity info resolved via <c>CSharpParserService.GetDomainEntities</c>.
        /// Supplements the stored DTO history data with live parsed values.
        /// Null when the parser has not yet parsed the solution.
        /// </summary>
        public EntityInfo DtoEntityInfo { get; set; }

        /// <summary>
        /// DTO entity info resolved from <c>CSharpParserService.CurrentSolutionClasses</c>
        /// (the same approach as <c>CRUDGeneratorUC.ListDtoFiles</c>).
        /// Used by the CRUD feature generator instead of re-fetching from the parser.
        /// Null when the parser has not yet parsed the solution.
        /// </summary>
        public EntityInfo CrudEntityInfo { get; set; }

        // ── Dependency metadata (populated by coherence checks) ──────────────

        /// <summary>Name of the parent entity, if this entity has a parent/child relationship.</summary>
        public string ParentEntityName { get; set; }

        /// <summary>
        /// True when the parent entity is present in the generation history (dynamic blocking applies).
        /// False when the parent is absent (static blocking via <see cref="RegenerableFeatureStatus.BlockedParentNotMigrated"/> applies).
        /// </summary>
        public bool HasParentDependency { get; set; }

        /// <summary>Option entity names referenced by this entity's CRUD or DTO.</summary>
        public List<string> OptionDependencies { get; set; } = [];

        // ── Warning messages (populated by coherence checks) ─────────────────

        /// <summary>Warning or blocking reason for the CRUD feature. Null when no issue.</summary>
        public string CrudWarningMessage { get; set; }

        /// <summary>Warning or blocking reason for the DTO feature. Null when no issue.</summary>
        public string DtoWarningMessage { get; set; }

        /// <summary>Informational warning for option dependencies. Null when no issue.</summary>
        public string OptionWarningMessage { get; set; }

        // ─────────────────────────────────────────────────────────────────────

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasCrudHistory => CrudHistory != null;
        public bool HasOptionHistory => OptionHistory != null;
        public bool HasDtoHistory => DtoHistory != null;

        public bool CanRegenerateCrud => CrudHistory != null && CrudStatus == RegenerableFeatureStatus.Ready;
        public bool CanRegenerateOption => OptionHistory != null && OptionStatus == RegenerableFeatureStatus.Ready;
        public bool CanRegenerateDto => DtoHistory != null && DtoStatus == RegenerableFeatureStatus.Ready;

        public bool CanSelectEntity => CanRegenerateCrud || CanRegenerateOption || CanRegenerateDto;

        public bool HasAnyGeneratedFeature => CrudHistory != null || OptionHistory != null || DtoHistory != null;

        public bool HasAnyIssue =>
            (CrudHistory != null && CrudStatus != RegenerableFeatureStatus.Ready) ||
            (OptionHistory != null && OptionStatus != RegenerableFeatureStatus.Ready) ||
            (DtoHistory != null && DtoStatus != RegenerableFeatureStatus.Ready);

        public DateTime LastGenerationDate
        {
            get
            {
                DateTime latest = DateTime.MinValue;
                if (CrudHistory != null && CrudHistory.Date > latest)
                    latest = CrudHistory.Date;
                if (OptionHistory != null && OptionHistory.Date > latest)
                    latest = OptionHistory.Date;
                if (DtoHistory != null && DtoHistory.DateTime > latest)
                    latest = DtoHistory.DateTime;
                return latest;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
