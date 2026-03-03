namespace BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures
{
    public enum RegenerateFeatureType
    {
        Crud,
        Option,
        Dto,
    }

    public static class RegenerateEntitySelectionRule
    {
        public static bool CanSelectEntity(RegenerableEntity entity)
        {
            return entity?.CanSelectEntity == true;
        }

        public static bool CanRegenerateCrud(RegenerableEntity entity)
        {
            return entity?.CanRegenerateCrud == true;
        }

        public static bool CanRegenerateOption(RegenerableEntity entity)
        {
            return entity?.CanRegenerateOption == true;
        }

        public static bool CanRegenerateDto(RegenerableEntity entity)
        {
            return entity?.CanRegenerateDto == true;
        }

        public static string GetBlockageReason(RegenerableEntity entity, RegenerateFeatureType featureType)
        {
            if (entity == null)
                return string.Empty;

            return featureType switch
            {
                RegenerateFeatureType.Crud => entity.CrudHistory == null
                    ? "No CRUD generation history found."
                    : entity.CrudStatus == RegenerableFeatureStatus.Missing
                        ? $"DTO file not found: {entity.CrudHistory.Mapping?.Dto}"
                        : entity.CrudStatus == RegenerableFeatureStatus.Error
                            ? "Error reading CRUD generation history."
                            : string.Empty,
                RegenerateFeatureType.Option => entity.OptionHistory == null
                    ? "No Option generation history found."
                    : entity.OptionStatus == RegenerableFeatureStatus.Missing
                        ? $"Entity file not found: {entity.OptionHistory.Mapping?.Entity}"
                        : entity.OptionStatus == RegenerableFeatureStatus.Error
                            ? "Error reading Option generation history."
                            : string.Empty,
                RegenerateFeatureType.Dto => entity.DtoHistory == null
                    ? "No DTO generation history found."
                    : entity.DtoStatus == RegenerableFeatureStatus.Missing
                        ? $"DTO file not found for entity: {entity.EntityNameSingular}Dto.cs"
                        : entity.DtoStatus == RegenerableFeatureStatus.Error
                            ? "Error reading DTO generation history."
                            : string.Empty,
                _ => string.Empty,
            };
        }
    }
}
