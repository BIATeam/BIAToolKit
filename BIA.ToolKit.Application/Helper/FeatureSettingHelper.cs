namespace BIA.ToolKit.Application.Helper
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// FeatureSetting Service.
    /// </summary>
    public static class FeatureSettingHelper
    {
        public const string fileName = ".bia\\BiaToolKit_FeatureSetting.json";

        /// <summary>
        /// Gets the specified project path.
        /// </summary>
        /// <param name="projectPath">The project path.</param>
        /// <returns>List of <see cref="FeatureSetting"/></returns>
        public static List<FeatureSetting> Get(string projectPath)
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

        public static bool HasAllFeature(this List<FeatureSetting> settings)
        {
            return settings.Exists(x => !x.IsSelected) != true;
        }

        public static List<string> GetFoldersToExcludes(this IEnumerable<FeatureSetting> settings)
        {
            return settings
                .FilterNotSelectedFeatures()
                .Where(x => x.FoldersToExcludes.Count != 0)
                .SelectMany(x => x.FoldersToExcludes)
                .Distinct()
                .ToList();
        }

        public static List<string> GetBiaFeatureTagToDeletes(this IEnumerable<FeatureSetting> settings, string prefix = null)
        {
            return settings
                .FilterNotSelectedFeatures()
                .Where(x => x.Tags.Count != 0)
                .OrderBy(x => x.Id)
                .Distinct()
                .SelectMany(x => x.Tags.Select(tag => $"{prefix}{tag}"))
                .ToList();
        }

        public static List<string> GetAllBiaFeatureTag(this IEnumerable<FeatureSetting> settings, string prefix = null)
        {
            return settings
                .Where(x => x.Tags.Count != 0)
                .OrderBy(x => x.Id)
                .Distinct()
                .SelectMany(x => x.Tags.Select(tag => $"{prefix}{tag}"))
                .ToList();
        }

        public static List<FeatureSetting> FilterNotSelectedFeatures(this IEnumerable<FeatureSetting> settings)
        {
            var result = new List<FeatureSetting>();
            foreach (var feature in settings.Where(x => !x.IsSelected))
            {
                result.Add(feature);
                if (feature.DisabledFeatures.Count != 0)
                {
                    result.AddRange(settings.Where(s => feature.DisabledFeatures.Contains(s.Id)));
                }
            }

            return result;
        }
    }
}
