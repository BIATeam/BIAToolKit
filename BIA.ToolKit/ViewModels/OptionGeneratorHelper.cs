namespace BIA.ToolKit.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;

    /// <summary>
    /// Helper class to manage Option Generator history and settings
    /// Extracted from OptionGeneratorUC.xaml.cs to follow DRY and SRP principles
    /// </summary>
    public class OptionGeneratorHelper
    {
        private readonly CRUDSettings settings;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly Project currentProject;

        private string optionHistoryFileName;

        public OptionGeneratorHelper(
            CRUDSettings settings,
            FileGeneratorService fileGeneratorService,
            Project currentProject)
        {
            this.settings = settings;
            this.fileGeneratorService = fileGeneratorService;
            this.currentProject = currentProject;
        }

        /// <summary>
        /// Initialize generation settings and load history
        /// </summary>
        public (List<FeatureGenerationSettings> backSettings, List<FeatureGenerationSettings> frontSettings, OptionGeneration history) 
            InitializeSettings(List<ZipFeatureType> zipFeatureTypeList)
        {
            var backSettingsList = new List<FeatureGenerationSettings>();
            var frontSettingsList = new List<FeatureGenerationSettings>();

            // Get files/folders name
            string dotnetBiaFolderPath = Path.Combine(currentProject.Folder, Constants.FolderDotNet, Constants.FolderBia);
            string backSettingsFileName = Path.Combine(currentProject.Folder, Constants.FolderDotNet, settings.GenerationSettingsFileName);
            optionHistoryFileName = Path.Combine(currentProject.Folder, Constants.FolderBia, settings.OptionGenerationHistoryFileName);

            OptionGeneration optionHistory = null;

            // Load generation history
            optionHistory = CommonTools.DeserializeJsonFile<OptionGeneration>(optionHistoryFileName);

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                return (backSettingsList, frontSettingsList, optionHistory);
            }

            // Load BIA settings
            if (File.Exists(backSettingsFileName))
            {
                backSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(backSettingsFileName));
                foreach (var setting in backSettingsList)
                {
                    var featureType = Enum.Parse<FeatureType>(setting.Type);
                    if (featureType != FeatureType.Option)
                        continue;

                    var zipFeatureType = new ZipFeatureType(
                        featureType,
                        GenerationType.WebApi,
                        setting.ZipName,
                        dotnetBiaFolderPath,
                        setting.Feature,
                        setting.Parents,
                        setting.NeedParent,
                        setting.AdaptPaths,
                        setting.FeatureDomain)
                    {
                        IsChecked = true
                    };
                    zipFeatureTypeList.Add(zipFeatureType);
                }
            }

            return (backSettingsList, frontSettingsList, optionHistory);
        }

        /// <summary>
        /// Load front generation settings for a specific BIA front folder
        /// </summary>
        public List<FeatureGenerationSettings> LoadFrontSettings(
            string biaFront, 
            List<ZipFeatureType> zipFeatureTypeList)
        {
            var frontSettingsList = new List<FeatureGenerationSettings>();

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                return frontSettingsList;
            }

            // Remove existing front settings
            zipFeatureTypeList.RemoveAll(x => x.GenerationType == GenerationType.Front);

            string angularBiaFolderPath = Path.Combine(currentProject.Folder, biaFront, Constants.FolderBia);
            string frontSettingsFileName = Path.Combine(currentProject.Folder, biaFront, settings.GenerationSettingsFileName);

            if (File.Exists(frontSettingsFileName))
            {
                frontSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(frontSettingsFileName));
                foreach (var setting in frontSettingsList)
                {
                    var featureType = Enum.Parse<FeatureType>(setting.Type);
                    if (featureType != FeatureType.Option)
                        continue;

                    var zipFeatureType = new ZipFeatureType(
                        featureType,
                        GenerationType.Front,
                        setting.ZipName,
                        angularBiaFolderPath,
                        setting.Feature,
                        setting.Parents,
                        setting.NeedParent,
                        setting.AdaptPaths,
                        setting.FeatureDomain)
                    {
                        IsChecked = true
                    };

                    zipFeatureTypeList.Add(zipFeatureType);
                }
            }

            return frontSettingsList;
        }

        /// <summary>
        /// Load last generation history for a specific entity
        /// </summary>
        public OptionGenerationHistory LoadEntityHistory(
            OptionGeneration optionHistory,
            string entityPath)
        {
            if (optionHistory?.OptionGenerationHistory == null)
                return null;

            return optionHistory.OptionGenerationHistory
                .FirstOrDefault(h => h.Mapping.Entity == entityPath);
        }

        /// <summary>
        /// Update option generation history file
        /// </summary>
        public void UpdateHistory(
            OptionGeneration optionHistory,
            OptionGenerationHistory history)
        {
            try
            {
                // Get existing to verify if previous generation for same entity name was already done
                var genFound = optionHistory.OptionGenerationHistory.FirstOrDefault(gen => gen.EntityNameSingular == history.EntityNameSingular);
                if (genFound != null)
                {
                    // Remove last generation to replace by new generation
                    optionHistory.OptionGenerationHistory.Remove(genFound);
                }

                optionHistory.OptionGenerationHistory.Add(history);

                // Save to file
                CommonTools.SerializeToJsonFile(optionHistory, optionHistoryFileName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update option generation history: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete last generation history for a specific entity
        /// </summary>
        public void DeleteHistory(OptionGeneration optionHistory, OptionGenerationHistory history)
        {
            if (optionHistory?.OptionGenerationHistory == null)
                return;

            optionHistory.OptionGenerationHistory.Remove(history);
            CommonTools.SerializeToJsonFile(optionHistory, optionHistoryFileName);
        }

        /// <summary>
        /// Get list of already generated options (excluding current entity)
        /// </summary>
        public List<OptionGenerationHistory> GetGeneratedOptions(
            OptionGeneration optionHistory,
            string currentEntityName)
        {
            if (optionHistory?.OptionGenerationHistory == null)
                return new List<OptionGenerationHistory>();

            return optionHistory.OptionGenerationHistory
                .Where(h => h.Mapping.Entity != currentEntityName &&
                           h.Generation.Any(g => g.FeatureType == FeatureType.Option.ToString()))
                .ToList();
        }
    }
}
