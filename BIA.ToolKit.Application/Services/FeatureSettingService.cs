namespace BIA.ToolKit.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// Centralizes loading and merging of feature settings from template versions
    /// and project folders. This logic was previously embedded in
    /// <c>VersionAndOptionUserControl</c> and is now shared across creation,
    /// migration, and feature-regeneration flows.
    /// </summary>
    public class FeatureSettingService(SettingsService settingsService, IConsoleWriter consoleWriter)
    {
        private readonly SettingsService settingsService = settingsService;
        private readonly IConsoleWriter consoleWriter = consoleWriter;

        /// <summary>
        /// Loads feature settings from the template version folder and the project path,
        /// then merges them: the template provides the full feature list while the
        /// project overrides <see cref="FeatureSetting.IsSelected"/> for matching Ids.
        /// </summary>
        /// <param name="templateVersionFolderPath">
        /// Path to the template version folder (<see cref="WorkRepository.VersionFolderPath"/>).
        /// May be <c>null</c> when no template is selected yet.
        /// </param>
        /// <param name="projectPath">
        /// Path to the current project root. May be <c>null</c> during project creation.
        /// </param>
        /// <returns>Merged list of <see cref="FeatureSetting"/>, or an empty list when
        /// no template features exist.</returns>
        public static List<FeatureSetting> LoadAndMergeFeatureSettings(string templateVersionFolderPath, string projectPath)
        {
            List<FeatureSetting> featureSettings = FeatureSettingHelper.Get(templateVersionFolderPath);
            List<FeatureSetting> projectFeatureSettings = FeatureSettingHelper.Get(projectPath);

            if (featureSettings?.Count > 0 && projectFeatureSettings?.Count > 0)
            {
                foreach (FeatureSetting featureSetting in featureSettings)
                {
                    FeatureSetting projectFeatureSetting = projectFeatureSettings.Find(x => x.Id == featureSetting.Id);

                    if (projectFeatureSetting != null)
                    {
                        featureSetting.IsSelected = projectFeatureSetting.IsSelected;
                    }
                }
            }

            return featureSettings ?? [];
        }

        /// <summary>
        /// Reads the <c>ProjectGeneration</c> JSON from the project's <c>.bia</c> folder
        /// and applies its tags/folders to refine <see cref="FeatureSetting.IsSelected"/>
        /// on the supplied feature settings.
        /// </summary>
        /// <param name="projectPath">Path to the project root.</param>
        /// <param name="featureSettings">The already-merged feature settings to refine.</param>
        /// <param name="originFeatureSettings">
        /// Optional origin feature settings used in migration scenarios to determine
        /// whether a new feature (absent from the origin) should stay selected.
        /// </param>
        public void ApplyProjectGenerationSettings(
            string projectPath,
            List<FeatureSetting> featureSettings,
            List<FeatureSetting> originFeatureSettings = null)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return;

            string projectGenerationFile = Path.Combine(projectPath, Constants.FolderBia, settingsService.ReadSetting("ProjectGeneration"));
            if (!File.Exists(projectGenerationFile))
                return;

            try
            {
                VersionAndOptionDto dto = CommonTools.DeserializeJsonFile<VersionAndOptionDto>(projectGenerationFile);
                ApplyFeaturesSelection(featureSettings, dto.Tags ?? [], dto.Folders ?? [], originFeatureSettings);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error when reading {projectGenerationFile} : {ex.Message}", "red");
            }
        }

        /// <summary>
        /// Pure algorithm that applies tag-based and folder-based matching to determine
        /// <see cref="FeatureSetting.IsSelected"/> for each feature. Extracted from
        /// <c>VersionAndOptionViewModel.SetFeaturesSelection</c>.
        /// </summary>
        public static void ApplyFeaturesSelection(
            List<FeatureSetting> featureSettings,
            List<string> projectGenerationTags,
            List<string> projectGenerationExcludedFolders,
            List<FeatureSetting> originFeatureSettings)
        {
            foreach (FeatureSetting feature in featureSettings)
            {
                bool isFeatureTagUsedInProjectGeneration = projectGenerationTags.Any(feature.Tags.Contains);
                bool isFeatureExcludedFoldersInProjectGeneration = projectGenerationExcludedFolders.Any(feature.FoldersToExcludes.Contains);
                bool isSelected = isFeatureTagUsedInProjectGeneration || isFeatureExcludedFoldersInProjectGeneration;

                if (!isSelected && originFeatureSettings is not null)
                {
                    FeatureSetting originFeature = originFeatureSettings.FirstOrDefault(x => x.Id == feature.Id);
                    isSelected = originFeature is null && feature.IsSelected;
                }

                feature.IsSelected = isSelected;
            }
        }
    }
}
