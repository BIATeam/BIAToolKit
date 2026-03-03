namespace BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures
{
    using System;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;

    public class RegenerableEntity
    {
        private bool isSelected;

        public string EntityNameSingular { get; set; }
        public string EntityNamePlural { get; set; }

        public CRUDGenerationHistory CrudHistory { get; set; }
        public OptionGenerationHistory OptionHistory { get; set; }
        public DtoGeneration DtoHistory { get; set; }

        public RegenerableFeatureStatus CrudStatus { get; set; }
        public RegenerableFeatureStatus OptionStatus { get; set; }
        public RegenerableFeatureStatus DtoStatus { get; set; }

        public bool IsSelected
        {
            get => isSelected;
            set => isSelected = value;
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
    }
}
