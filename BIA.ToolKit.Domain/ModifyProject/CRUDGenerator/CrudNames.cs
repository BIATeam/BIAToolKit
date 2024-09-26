namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator
{
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using System.Collections.Generic;

    public class CrudNames
    {
        private readonly List<FeatureGenerationSettings> BackSettingsList;
        private readonly List<FeatureGenerationSettings> FrontSettingsList;
        private IEnumerable<FeatureGenerationSettings> AllSettings => BackSettingsList.Concat(FrontSettingsList);

        public string NewCrudNamePascalSingular { get; private set; }
        public string NewCrudNamePascalPlural { get; private set; }
        public string NewCrudNameCamelSingular { get; private set; }
        public string NewCrudNameCamelPlural { get; private set; }
        public string NewCrudNameKebabSingular { get; private set; }
        public string NewCrudNameKebabPlural { get; private set; }

        public CrudNames(List<FeatureGenerationSettings> backSettingsList, List<FeatureGenerationSettings> frontSettingsList)
        {
            this.BackSettingsList = backSettingsList;
            this.FrontSettingsList = frontSettingsList;
        }

        public string GetOldFeatureNameSingularPascal(string feature, FeatureType featureType) => AllSettings.First(x => x.Feature == feature && x.Type == featureType.ToString()).FeatureName;

        public string GetOldFeatureNamePluralPascal(string feature, FeatureType featureType) => AllSettings.First(x => x.Feature == feature && x.Type == featureType.ToString()).FeatureNamePlural;

        public string GetOldFeatureNameSingularCamel(string feature, FeatureType featureType) => CommonTools.ConvertToCamelCase(GetOldFeatureNameSingularPascal(feature, featureType));

        public string GetOldFeatureNamePluralCamel(string feature, FeatureType featureType) => CommonTools.ConvertToCamelCase(GetOldFeatureNamePluralPascal(feature, featureType));

        public string GetOldFeatureNameSingularKebab(string feature, FeatureType featureType) => CommonTools.ConvertPascalToKebabCase(GetOldFeatureNameSingularPascal(feature, featureType));

        public string GetOldFeatureNamePluralKebab(string feature, FeatureType featureType) => CommonTools.ConvertPascalToKebabCase(GetOldFeatureNamePluralPascal(feature, featureType));

        public void InitRenameValues(string newValueSingular, string newValuePlural)
        {
            this.NewCrudNamePascalSingular = newValueSingular;
            this.NewCrudNamePascalPlural = newValuePlural;
            NewCrudNameCamelSingular = CommonTools.ConvertToCamelCase(NewCrudNamePascalSingular);
            NewCrudNameCamelPlural = CommonTools.ConvertToCamelCase(NewCrudNamePascalPlural);
            NewCrudNameKebabSingular = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalSingular);
            NewCrudNameKebabPlural = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalPlural);
        }

        public string ConvertPascalOldToNewCrudName(string value, string feature, FeatureType type)
        {
            if (string.IsNullOrWhiteSpace(value)) 
                return value;

            return ReplaceOldToNewValue(value, GetOldFeatureNamePluralPascal(feature, type), NewCrudNamePascalPlural, GetOldFeatureNameSingularPascal(feature, type), NewCrudNamePascalSingular);
        }

        public string ConvertCamelOldToNewCrudName(string value, string feature, FeatureType type)
        {
            if (string.IsNullOrWhiteSpace(value)) 
                return value;

            return ReplaceOldToNewValue(value, GetOldFeatureNamePluralCamel(feature, type), NewCrudNameCamelPlural, GetOldFeatureNameSingularCamel(feature, type), NewCrudNameCamelSingular);
        }

        private static string ReplaceOldToNewValue(string value, string oldValuePlural, string newValuePlural, string oldValueSingular, string newValueSingular)
        {
            if (string.IsNullOrWhiteSpace(value)) 
                return value;

            return value.Replace(oldValuePlural, newValuePlural).Replace(oldValueSingular, newValueSingular);
        }
    }

}
