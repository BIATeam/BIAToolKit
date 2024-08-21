namespace BIA.ToolKit.Application.Services
{
    using System.IO;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// FeatureSetting Service.
    /// </summary>
    public class FeatureSettingService
    {
        private const string fileName = "BiaToolKit_FeatureSetting.json";

        /// <summary>
        /// Saves the specified feature setting.
        /// </summary>
        /// <param name="featureSetting">The feature setting.</param>
        /// <param name="projectPath">The project path.</param>
        public void Save(FeatureSetting featureSetting, string projectPath)
        {
            string jsonFile = Path.Combine(projectPath, fileName);

            featureSetting ??= new FeatureSetting();

            if (!featureSetting.HasAllFeature)
            {
                CommonTools.SerializeToJsonFile(featureSetting, jsonFile);
            }
            else
            {
                File.Delete(jsonFile);
            }
        }

        /// <summary>
        /// Gets the specified project path.
        /// </summary>
        /// <param name="projectPath">The project path.</param>
        /// <returns><see cref="FeatureSetting"/></returns>
        public FeatureSetting Get(string projectPath)
        {
            string jsonFile = Path.Combine(projectPath, fileName);

            FeatureSetting featureSetting = CommonTools.DeserializeJsonFile<FeatureSetting>(jsonFile);

            featureSetting ??= new FeatureSetting();

            return featureSetting;
        }
    }
}
