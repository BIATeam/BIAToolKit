namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator
{
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using System.Collections.Generic;

    public class CrudNames
    {
        private readonly List<CrudGenerationSettings> BackSettingsList;
        private readonly List<CrudGenerationSettings> FrontSettingsList;
        private IEnumerable<CrudGenerationSettings> AllSettings => BackSettingsList.Concat(FrontSettingsList);
        private bool IsWebApiSelected;
        private bool IsFrontSelected;

        public string NewCrudNamePascalSingular { get; private set; }
        public string NewCrudNamePascalPlural { get; private set; }
        public string NewCrudNameCamelSingular { get; private set; }
        public string NewCrudNameCamelPlural { get; private set; }
        public string NewCrudNameKebabSingular { get; private set; }
        public string NewCrudNameKebabPlural { get; private set; }

        public CrudNames(List<CrudGenerationSettings> backSettingsList, List<CrudGenerationSettings> frontSettingsList)
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

        public void InitRenameValues(string newValueSingular, string newValuePlural, string feature, bool isWebApiSelected = true, bool isFrontSelected = true)
        {
            this.NewCrudNamePascalSingular = newValueSingular;
            this.NewCrudNamePascalPlural = newValuePlural;
            NewCrudNameCamelSingular = CommonTools.ConvertToCamelCase(NewCrudNamePascalSingular);
            NewCrudNameCamelPlural = CommonTools.ConvertToCamelCase(NewCrudNamePascalPlural);
            NewCrudNameKebabSingular = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalSingular);
            NewCrudNameKebabPlural = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalPlural);
            this.IsWebApiSelected = isWebApiSelected;
            this.IsFrontSelected = isFrontSelected;
        }

        public string ConvertPascalOldToNewCrudName(string value, string feature, FeatureType type, bool convertCamel = true)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            return convertCamel ?
                ReplaceOldToNewValue(value, GetOldFeatureNamePluralCamel(feature, type), NewCrudNamePascalPlural, GetOldFeatureNameSingularCamel(feature, type), NewCrudNamePascalSingular) :
                ReplaceOldToNewValue(value, GetOldFeatureNamePluralPascal(feature, type), NewCrudNamePascalPlural, GetOldFeatureNameSingularPascal(feature, type), NewCrudNamePascalSingular);
        }

        /// <summary>
        /// Convert value form Camel case to Kebab case
        /// </summary>
        public string ConvertCamelToKebabCrudName(string value, string feature, FeatureType type)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            return ReplaceOldToNewValue(value, GetOldFeatureNamePluralCamel(feature, type), NewCrudNameKebabPlural, GetOldFeatureNameSingularCamel(feature, type), NewCrudNameKebabSingular);
        }

        private string ReplaceOldToNewValue(string value, string oldValuePlural, string newValuePlural, string oldValueSingular, string newValueSingular)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            List<int> nbAllOccurOldValue = FindOccurences(value, oldValueSingular);
            List<int> nbOccurOldValuePlural = FindOccurences(value, oldValuePlural);
            List<int> nbOccurOldValueSingular = nbAllOccurOldValue.Except(nbOccurOldValuePlural).ToList();

            foreach (int index in nbAllOccurOldValue.OrderByDescending(i => i))
            {
                string before = value[..index];
                if (nbOccurOldValuePlural.Contains(index))
                {
                    string after = value[(index + oldValuePlural.Length)..];
                    value = $"{before}{newValuePlural}{after}";
                }
                else if (nbOccurOldValueSingular.Contains(index))
                {
                    string after = value[(index + oldValueSingular.Length)..];
                    value = $"{before}{newValueSingular}{after}";
                }
            }

            return value;
        }

        private List<int> FindOccurences(string line, string search)
        {
            int lastIndex = 0;
            List<int> indexList = new();
            for (int count = 0; line.Length > 0; count++)
            {
                int index = line.IndexOf(search);
                if (index < 0)
                    break;
                else
                {
                    indexList.Add(lastIndex + index);
                    lastIndex += index + search.Length;
                    line = line.Substring(index + search.Length);
                }
            }

            return indexList;
        }
    }

}
