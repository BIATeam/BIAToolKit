namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator
{
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using System.Collections.Generic;

    public class CrudNames
    {
        private List<CrudGenerationSettings> BackSettingsList;
        private List<CrudGenerationSettings> FrontSettingsList;
        private bool IsWebApiSelected;
        private bool IsFrontSelected;

        public string? NewCrudNamePascalSingular { get; private set; }
        public string? NewCrudNamePascalPlural { get; private set; }
        public string? NewCrudNameCamelSingular { get; private set; }
        public string? NewCrudNameCamelPlural { get; private set; }
        public string? NewCrudNameKebabSingular { get; private set; }
        public string? NewCrudNameKebabPlural { get; private set; }

        public string OldCrudNamePascalSingular { get; private set; } = "Plane";
        public string OldCrudNamePascalPlural { get; private set; } = "Planes";
        public string OldCrudNameCamelSingular { get; private set; } = "plane";
        public string OldCrudNameCamelPlural { get; private set; } = "planes";

        public string OldOptionNamePascalSingular { get; private set; } = "Airport";
        public string OldOptionNamePascalPlural { get; private set; } = "Airports";
        public string OldOptionNameCamelSingular { get; private set; } = "airport";
        public string OldOptionNameCamelPlural { get; private set; } = "airports";

        public string OldTeamNamePascalSingular { get; private set; } = "MaintenanceTeam";
        public string OldTeamNamePascalPlural { get; private set; } = "MaintenanceTeams";
        public string OldTeamNameCamelSingular { get; private set; } = "maintenanceTeam";
        public string OldTeamNameCamelPlural { get; private set; } = "maintenanceTeams";
        public string OldTeamNameKebabSingular { get; private set; } = "maintenance-team";
        public string OldTeamNameKebabPlural { get; private set; } = "maintenance-teams";

        public CrudNames(List<CrudGenerationSettings> backSettingsList, List<CrudGenerationSettings> frontSettingsList)
        {
            this.BackSettingsList = backSettingsList;
            this.FrontSettingsList = frontSettingsList;
        }

        public void InitRenameValues(string newValueSingular, string newValuePlural, bool isWebApiSelected = true, bool isFrontSelected = true)
        {
            this.NewCrudNamePascalSingular = newValueSingular;
            this.NewCrudNamePascalPlural = newValuePlural;
            this.IsWebApiSelected = isWebApiSelected;
            this.IsFrontSelected = isFrontSelected;

            (string? crudNameSingular, string? crudNamePlural) = GetSingularPlurialNames(FeatureType.CRUD);
            (string? optionNameSingular, string? optionNamePlural) = GetSingularPlurialNames(FeatureType.Option);
            (string? teamNameSingular, string? teamNamePlural) = GetSingularPlurialNames(FeatureType.Team);

            NewCrudNameCamelSingular = CommonTools.ConvertToCamelCase(NewCrudNamePascalSingular);
            NewCrudNameCamelPlural = CommonTools.ConvertToCamelCase(NewCrudNamePascalPlural);
            NewCrudNameKebabSingular = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalSingular);
            NewCrudNameKebabPlural = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalPlural);

            // Get Pascal case value
            OldCrudNamePascalSingular = string.IsNullOrWhiteSpace(crudNameSingular) ? OldCrudNamePascalSingular : crudNameSingular;
            OldCrudNamePascalPlural = string.IsNullOrWhiteSpace(crudNamePlural) ? OldCrudNamePascalPlural : crudNamePlural;
            OldOptionNamePascalSingular = string.IsNullOrWhiteSpace(optionNameSingular) ? OldOptionNamePascalSingular : optionNameSingular;
            OldOptionNamePascalPlural = string.IsNullOrWhiteSpace(optionNamePlural) ? OldOptionNamePascalPlural : optionNamePlural;
            OldTeamNamePascalSingular = string.IsNullOrWhiteSpace(teamNameSingular) ? OldTeamNamePascalSingular : teamNameSingular;
            OldTeamNamePascalPlural = string.IsNullOrWhiteSpace(teamNamePlural) ? OldTeamNamePascalPlural : teamNamePlural;

            // Convert value to Camel case
            OldCrudNameCamelSingular = CommonTools.ConvertToCamelCase(OldCrudNamePascalSingular);
            OldCrudNameCamelPlural = CommonTools.ConvertToCamelCase(OldCrudNamePascalPlural);
            OldOptionNameCamelSingular = CommonTools.ConvertToCamelCase(OldOptionNamePascalSingular);
            OldOptionNameCamelPlural = CommonTools.ConvertToCamelCase(OldOptionNamePascalPlural);
            OldTeamNameCamelSingular = CommonTools.ConvertToCamelCase(OldTeamNamePascalSingular);
            OldTeamNameCamelPlural = CommonTools.ConvertToCamelCase(OldTeamNamePascalPlural);

            //Convert value to Kebab case
            OldTeamNameKebabSingular = CommonTools.ConvertPascalToKebabCase(OldTeamNamePascalSingular);
            OldTeamNameKebabPlural = CommonTools.ConvertPascalToKebabCase(OldTeamNamePascalPlural);
        }

        private (string? singular, string? plurial) GetSingularPlurialNames(FeatureType type)
        {
            CrudGenerationSettings? settings = null;

            if (this.IsWebApiSelected)
                settings = this.BackSettingsList.FirstOrDefault(x => x.Type == type.ToString());
            else if (this.IsFrontSelected)
                settings = this.FrontSettingsList.FirstOrDefault(x => x.Type == type.ToString());

            return (settings?.FeatureName, settings?.FeatureNamePlural);
        }

        public string ConvertPascalOldToNewCrudName(string value, FeatureType type, bool convertCamel = true)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            switch (type)
            {
                //case FeatureType.WebApi:
                case FeatureType.CRUD:
                    value = ReplaceOldToNewValue(value, OldCrudNamePascalPlural, NewCrudNamePascalPlural, OldCrudNamePascalSingular, NewCrudNamePascalSingular);
                    if (convertCamel)
                    {
                        value = ReplaceOldToNewValue(value, OldCrudNameCamelPlural, NewCrudNameCamelPlural, OldCrudNameCamelSingular, NewCrudNameCamelSingular);
                    }
                    break;
                case FeatureType.Option:
                    value = ReplaceOldToNewValue(value, OldOptionNamePascalPlural, NewCrudNamePascalPlural, OldOptionNamePascalSingular, NewCrudNamePascalSingular);
                    if (convertCamel)
                    {
                        value = ReplaceOldToNewValue(value, OldOptionNameCamelPlural, NewCrudNameCamelPlural, OldOptionNameCamelSingular, NewCrudNameCamelSingular);
                    }
                    break;
                case FeatureType.Team:
                    value = ReplaceOldToNewValue(value, OldTeamNamePascalPlural, NewCrudNamePascalPlural, OldTeamNamePascalSingular, NewCrudNamePascalSingular);
                    if (convertCamel)
                    {
                        value = ReplaceOldToNewValue(value, OldTeamNameCamelPlural, NewCrudNameCamelPlural, OldTeamNameCamelSingular, NewCrudNameCamelSingular);
                    }
                    break;
            }

            return value;
        }

        /// <summary>
        /// Convert value form Camel case to Kebab case
        /// </summary>
        public string ConvertCamelToKebabCrudName(string value, FeatureType type)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            switch (type)
            {
                case FeatureType.CRUD:
                    value = ReplaceOldToNewValue(value, OldCrudNameCamelPlural, NewCrudNameKebabPlural, OldCrudNameCamelSingular, NewCrudNameKebabSingular);
                    break;
                case FeatureType.Option:
                    value = ReplaceOldToNewValue(value, OldOptionNameCamelPlural, NewCrudNameKebabPlural, OldOptionNameCamelSingular, NewCrudNameKebabSingular);
                    break;
                case FeatureType.Team:
                    value = ReplaceOldToNewValue(value, OldTeamNameCamelPlural, NewCrudNameKebabPlural, OldTeamNameCamelSingular, NewCrudNameKebabSingular);
                    break;
            }

            return value;
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
