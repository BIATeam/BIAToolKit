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

        public static bool HasAllFeature(List<FeatureSetting> featureSettings)
        {
            return featureSettings?.Exists(x => !x.IsSelected) != true;
        }

        public static List<string> GetFoldersToExcludes(List<FeatureSetting> settings)
        {
            List<string> foldersToExcludes = settings.Where(x => !x.IsSelected).SelectMany(x => x.FoldersToExcludes).Distinct().ToList();
            return foldersToExcludes;
        }

        public static List<string> GetBiaFeatureTagToDeletes(List<FeatureSetting> settings, string prefix = null)
        {
            List<string> tags = settings
                ?.Where(x => !x.IsSelected && x.Tags?.Any() == true)
                .SelectMany(x => x.Tags.Select(tag => $"{prefix}{tag}"))
                .Distinct().ToList();
            return tags;
        }

        public static List<string> GetAllBiaFeatureTag(List<FeatureSetting> settings, string prefix = null)
        {
            List<string> tags = settings
                ?.Where(x => x.Tags?.Any() == true)
                .SelectMany(x => x.Tags.Select(tag => $"{prefix}{tag}"))
                .Distinct().ToList();
            return tags;
        }

        public static List<string> GetFileToExcludes(VersionAndOption versionAndOption, List<FeatureSetting> settings)
        {
            List<string> tags = GetBiaFeatureTagToDeletes(settings, BiaFeatureTag.ItemGroupTag);

            List<string> filesToExcludes = new List<string>();

            string csprojFile = (FileHelper.GetFilesFromPathWithExtension(versionAndOption.WorkTemplate.VersionFolderPath, $"*{FileExtensions.DotNetProject}")).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(csprojFile))
            {
                XDocument document = XDocument.Load(csprojFile);
                XNamespace ns = document.Root.Name.Namespace;

                XElement itemGroup = document.Descendants(ns + "ItemGroup")
                                        .FirstOrDefault(x => tags.Contains((string)x.Attribute("Label")));

                if (itemGroup != null)
                {
                    List<string> compileRemoveItems = itemGroup.Elements(ns + "Compile")
                                                      .Where(x => x.Attribute("Remove") != null)
                                                      .Select(x => x.Attribute("Remove").Value)
                                                      .ToList();

                    foreach (string item in compileRemoveItems)
                    {
                        string newPattern;
                        if (item.Contains("**\\*"))
                        {
                            newPattern = ".*" + Regex.Escape(item.Replace("**\\*", "").Replace(".cs", "").Replace("*", "")) + ".*\\.cs$";
                        }
                        else
                        {
                            newPattern = "^" + Regex.Escape(item.Replace("**\\", "").Replace(".cs", "")) + "\\.cs$";
                        }

                        if (!filesToExcludes.Contains(newPattern))
                        {
                            filesToExcludes.Add(newPattern);
                        }
                    }
                }
            }

            return filesToExcludes;
        }

        /// <summary>
        /// Gets all.
        /// </summary>
        /// <returns></returns>
        public static List<FeatureSetting> GetAll()
        {
            return new List<FeatureSetting>()
            {
                new FeatureSetting()
                {
                    Id = 1,
                    DisplayName = "FrontEnd",
                    Description = "FrontEnd desc",
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
                    Description = "BackToBackAuth desc",
                    Tags = new List<string>() {"BIA_SERVICE_API" },
                    IsSelected = true,

                },
                new FeatureSetting()
                {
                    Id = 3,
                    DisplayName = "DeployDb",
                    Description = "DeployDb desc",
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
                    Description = "WorkerService desc",
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
