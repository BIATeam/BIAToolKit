namespace BIA.ToolKit.ViewModel.Messaging.Messages
{
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.ModifyProject;
    using System.Collections.Generic;

    /// <summary>
    /// Published when the origin feature settings have been updated, so the target VersionAndOption can refresh.
    /// </summary>
    public class OriginFeatureSettingsChangedMessage : IMessage
    {
        public required IReadOnlyList<FeatureSetting> FeatureSettings { get; set; }
    }
}
