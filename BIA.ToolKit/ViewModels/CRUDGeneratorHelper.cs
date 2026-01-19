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
    /// Helper class to manage CRUD Generator history and settings
    /// Extracted from CRUDGeneratorUC.xaml.cs to follow DRY and SRP principles
    /// </summary>
    public class CRUDGeneratorHelper
    {
        private readonly CRUDSettings settings;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly Project currentProject;

        private string crudHistoryFileName;

        public CRUDGeneratorHelper(
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
        /// Returns: (backSettings, frontSettings, featureNames, history, useFileGenerator)
        /// </summary>
        public (List<FeatureGenerationSettings> backSettings, 
                List<FeatureGenerationSettings> frontSettings, 
                List<string> featureNames,
                CRUDGeneration history,
                bool useFileGenerator) 
            InitializeSettings(List<ZipFeatureType> zipFeatureTypeList)
        {
            var backSettingsList = new List<FeatureGenerationSettings>();
            var frontSettingsList = new List<FeatureGenerationSettings>();
            var featureNames = new List<string>();

            // Get files/folders name
            string dotnetBiaFolderPath = Path.Combine(currentProject.Folder, Constants.FolderDotNet, Constants.FolderBia);
            string backSettingsFileName = Path.Combine(currentProject.Folder, Constants.FolderDotNet, settings.GenerationSettingsFileName);
            crudHistoryFileName = Path.Combine(currentProject.Folder, Constants.FolderBia, settings.CrudGenerationHistoryFileName);

            // Handle old path of CRUD history file
            var oldCrudHistoryFilePath = Path.Combine(currentProject.Folder, settings.CrudGenerationHistoryFileName);
            if (File.Exists(oldCrudHistoryFilePath))
            {
                File.Move(oldCrudHistoryFilePath, crudHistoryFileName);
            }

            CRUDGeneration crudHistory = null;
            bool useFileGenerator = false;

            // Load generation history
            crudHistory = CommonTools.DeserializeJsonFile<CRUDGeneration>(crudHistoryFileName);

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                useFileGenerator = true;
                featureNames.Add("CRUD");
                return (backSettingsList, frontSettingsList, featureNames, crudHistory, useFileGenerator);
            }

            // Load BIA settings
            if (File.Exists(backSettingsFileName))
            {
                backSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(backSettingsFileName));
                
                // Handle specific version adjustments
                if (currentProject.FrameworkVersion == "3.9.0")
                {
                    var crudPlanesFeature = backSettingsList.FirstOrDefault(x => x.Feature == "crud-planes");
                    if (crudPlanesFeature != null)
                    {
                        crudPlanesFeature.Feature = "planes";
                    }
                }
            }

            foreach (var setting in backSettingsList)
            {
                var featureType = Enum.Parse<FeatureType>(setting.Type);
                if (featureType == FeatureType.Option)
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
                    setting.FeatureDomain);
                
                zipFeatureTypeList.Add(zipFeatureType);
            }

            foreach (var featureName in zipFeatureTypeList.Select(x => x.Feature).Distinct())
            {
                featureNames.Add(featureName);
            }

            return (backSettingsList, frontSettingsList, featureNames, crudHistory, useFileGenerator);
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
                
                // Handle specific version adjustments
                if (currentProject.FrameworkVersion == "3.9.0")
                {
                    var featuresToRemove = frontSettingsList.Where(x => 
                        x.Feature == "planes-full-code" || 
                        x.Feature == "aircraft-maintenance-companies");
                    frontSettingsList = frontSettingsList.Except(featuresToRemove).ToList();
                }

                foreach (var setting in frontSettingsList)
                {
                    var featureType = Enum.Parse<FeatureType>(setting.Type);
                    if (featureType == FeatureType.Option)
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
                        setting.FeatureDomain);

                    zipFeatureTypeList.Add(zipFeatureType);
                }
            }

            return frontSettingsList;
        }

        /// <summary>
        /// Load last generation history for a specific DTO
        /// </summary>
        public CRUDGenerationHistory LoadDtoHistory(
            CRUDGeneration crudHistory,
            string dtoPath)
        {
            if (crudHistory?.CRUDGenerationHistory == null)
                return null;

            return crudHistory.CRUDGenerationHistory
                .FirstOrDefault(h => h.Mapping.Dto == dtoPath);
        }

        /// <summary>
        /// Update CRUD generation history file
        /// </summary>
        public void UpdateHistory(
            CRUDGeneration crudHistory,
            CRUDGenerationHistory history)
        {
            try
            {
                // Get existing to verify if previous generation for same entity name was already done
                var genFound = crudHistory.CRUDGenerationHistory.FirstOrDefault(gen => gen.EntityNameSingular == history.EntityNameSingular);
                if (genFound != null)
                {
                    // Remove last generation to replace by new generation
                    crudHistory.CRUDGenerationHistory.Remove(genFound);
                }

                crudHistory.CRUDGenerationHistory.Add(history);

                // Save to file
                CommonTools.SerializeToJsonFile(crudHistory, crudHistoryFileName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update CRUD generation history: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete last generation history for a specific DTO
        /// </summary>
        public void DeleteHistory(CRUDGeneration crudHistory, CRUDGenerationHistory history)
        {
            if (crudHistory?.CRUDGenerationHistory == null)
                return;

            // Delete CRUDGenerationHistory
            crudHistory.CRUDGenerationHistory.Remove(history);

            // Delete in options
            foreach (var optionHistory in crudHistory.CRUDGenerationHistory)
            {
                optionHistory.OptionItems?.Remove(history.EntityNameSingular);
            }

            // Save to file
            CommonTools.SerializeToJsonFile(crudHistory, crudHistoryFileName);
        }

        /// <summary>
        /// Get list of already generated CRUDs with options (excluding current DTO)
        /// </summary>
        public List<CRUDGenerationHistory> GetGeneratedOptions(
            CRUDGeneration crudHistory,
            string currentDtoName)
        {
            if (crudHistory?.CRUDGenerationHistory == null)
                return new List<CRUDGenerationHistory>();

            return crudHistory.CRUDGenerationHistory
                .Where(h => h.Mapping.Dto != currentDtoName &&
                           h.Generation.Any(g => g.FeatureType == FeatureType.Option.ToString()))
                .ToList();
        }

        /// <summary>
        /// Get generation histories that use a specific entity as option
        /// </summary>
        public List<CRUDGenerationHistory> GetHistoriesUsingOption(
            CRUDGeneration crudHistory,
            string entityName)
        {
            if (crudHistory?.CRUDGenerationHistory == null)
                return new List<CRUDGenerationHistory>();

            return crudHistory.CRUDGenerationHistory
                .Where(h => h.OptionItems != null && h.OptionItems.Contains(entityName))
                .ToList();
        }
    }
}
