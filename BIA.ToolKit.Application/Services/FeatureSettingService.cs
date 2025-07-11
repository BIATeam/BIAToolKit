﻿namespace BIA.ToolKit.Application.Services
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// FeatureSetting Service.
    /// </summary>
    public class FeatureSettingService
    {
        public const string fileName = ".bia\\BiaToolKit_FeatureSetting.json";

        /// <summary>
        /// Gets the specified project path.
        /// </summary>
        /// <param name="projectPath">The project path.</param>
        /// <returns>List of <see cref="FeatureSetting"/></returns>
        public List<FeatureSetting> Get(string projectPath)
        {
            List<FeatureSetting> featureSettings = null;

            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                string jsonFile = Path.Combine(projectPath, fileName);

                if (File.Exists(jsonFile))
                {
                    featureSettings = CommonTools.DeserializeJsonFile<List<FeatureSetting>>(jsonFile);
                }
            }

            return featureSettings;
        }

        public bool HasAllFeature(List<FeatureSetting> featureSettings)
        {
            return featureSettings?.Exists(x => !x.IsSelected) != true;
        }

        public List<string> GetFoldersToExcludes(List<FeatureSetting> settings)
        {
            List<string> foldersToExcludes = settings.Where(x => !x.IsSelected && x.FoldersToExcludes?.Any() == true).SelectMany(x => x.FoldersToExcludes).Distinct().ToList();
            return foldersToExcludes;
        }

        public List<string> GetBiaFeatureTagToDeletes(List<FeatureSetting> settings, string prefix = null)
        {
            List<string> tags = settings
                ?.Where(x => !x.IsSelected && x.Tags?.Any() == true)
                .OrderBy(x => x.Id)
                .SelectMany(x => x.Tags.Select(tag => $"{prefix}{tag}"))
                .Distinct().ToList();
            return tags;
        }

        public List<string> GetAllBiaFeatureTag(List<FeatureSetting> settings, string prefix = null)
        {
            List<string> tags = settings
                ?.Where(x => x.Tags?.Any() == true).OrderBy(x => x.Id)
                .SelectMany(x => x.Tags.Select(tag => $"{prefix}{tag}"))
                .Distinct().ToList();
            return tags;
        }
    }
}
