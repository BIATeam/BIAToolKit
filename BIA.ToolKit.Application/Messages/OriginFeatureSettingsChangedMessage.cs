using System.Collections.Generic;
using BIA.ToolKit.Domain.Model;

namespace BIA.ToolKit.Application.Messages
{
    /// <summary>
    /// Message sent when origin feature settings are changed
    /// </summary>
    public class OriginFeatureSettingsChangedMessage
    {
        public List<FeatureSetting> FeatureSettings { get; }

        public OriginFeatureSettingsChangedMessage(List<FeatureSetting> featureSettings)
        {
            FeatureSettings = featureSettings;
        }
    }
}
