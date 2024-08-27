namespace BIA.ToolKit.Application.Services
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Common.Helpers;
    using BIA.ToolKit.Domain.Model;
    using static BIA.ToolKit.Common.Constants;

    /// <summary>
    /// FeatureSetting Service.
    /// </summary>
    public class FeatureSettingService
    {
        private const string fileName = ".bia\\BiaToolKit_FeatureSetting.json";

        /// <summary>
        /// Saves the specified feature setting.
        /// </summary>
        /// <param name="featureSetting">The feature setting.</param>
        /// <param name="projectPath">The project path.</param>
        public void Save(List<FeatureSetting> featureSettings, string projectPath)
        {
            string jsonFile = Path.Combine(projectPath, fileName);
            CommonTools.SerializeToJsonFile(featureSettings, jsonFile);
        }

        /// <summary>
        /// Gets the specified project path.
        /// </summary>
        /// <param name="projectPath">The project path.</param>
        /// <returns>List of <see cref="FeatureSetting"/></returns>
        public List<FeatureSetting> Get(string projectPath)
        {
            List<FeatureSetting> featureSettings = null;

            string jsonFile = Path.Combine(projectPath, fileName);

            if (File.Exists(jsonFile))
            {
                featureSettings = CommonTools.DeserializeJsonFile<List<FeatureSetting>>(jsonFile);
            }

            return featureSettings;
        }

        public bool HasAllFeature(List<FeatureSetting> featureSettings)
        {
            return featureSettings?.Exists(x => !x.IsSelected) != true;
        }

        public List<string> GetFoldersToExcludes(List<FeatureSetting> settings)
        {
            List<string> foldersToExcludes = settings.Where(x => !x.IsSelected).SelectMany(x => x.FoldersToExcludes).Distinct().ToList();
            return foldersToExcludes;
        }

        public List<string> GetBiaFeatureTagToDeletes(List<FeatureSetting> settings, string prefix = null)
        {
            List<string> tags = settings
                ?.Where(x => !x.IsSelected && x.Tags?.Any() == true)
                .SelectMany(x => x.Tags.Select(tag => $"{prefix}{tag}"))
                .Distinct().ToList();
            return tags;
        }

        public List<string> GetAllBiaFeatureTag(List<FeatureSetting> settings, string prefix = null)
        {
            List<string> tags = settings
                ?.Where(x => x.Tags?.Any() == true)
                .SelectMany(x => x.Tags.Select(tag => $"{prefix}{tag}"))
                .Distinct().ToList();
            return tags;
        }

        /// <summary>
        /// Gets all.
        /// </summary>
        /// <returns></returns>
        public List<FeatureSetting> GetAll()
        {
            return new List<FeatureSetting>()
            {
                new FeatureSetting()
                {
                    Id = 1,
                    DisplayName = "FrontEnd",
                    Description = "Add the Angular project. On the back end, add the features User, Member, Role, Team, Notification, Translation, Log, Audit",
                    IsSelected = true,
                    Tags = new List<string>() {"BIA_FRONT_FEATURE" },
                    FoldersToExcludes = new List<string>()
                    {
                        ".*Angular.*$"
                    }
                },
                new FeatureSetting()
                {
                    Id = 2,
                    DisplayName = "BackToBackAuth",
                    Description = "Add an authentication system for backend to backend exchange",
                    Tags = new List<string>() {"BIA_SERVICE_API" },
                    IsSelected = true,

                },
                new FeatureSetting()
                {
                    Id = 3,
                    DisplayName = "DeployDb",
                    Description = "Add the project allowing the deployment of the database",
                    IsSelected = true,
                    FoldersToExcludes = new List<string>()
                    {
                        ".*DeployDB$"
                    }
                },
                new FeatureSetting()
                {
                    Id = 4,
                    DisplayName = "WorkerService",
                    Description = "Add the WorkerService project with Hangfire",
                    IsSelected = true,
                    FoldersToExcludes = new List<string>()
                    {
                        ".*WorkerService$"
                    }
                },
            };
        }
    }
}
