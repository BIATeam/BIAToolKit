namespace BIA.ToolKit.Application.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.Settings;

    // --- Project & Solution ---

    public sealed record ProjectChangedMessage(Project Project);

    public sealed record SolutionClassesParsedMessage;

    // --- Settings ---

    public sealed record SettingsUpdatedMessage(IBIATKSettings Settings);

    // --- Repository ---

    public sealed record RepositoriesUpdatedMessage;

    // --- Features ---

    public sealed record OriginFeatureSettingsChangedMessage(List<FeatureSetting> FeatureSettings);

    // --- Update ---

    public sealed record NewVersionAvailableMessage;

    // --- UI coordination ---

    public sealed record ExecuteActionWithWaiterMessage(Func<CancellationToken, Task> Action);

    public sealed record NavigateToConfigTabMessage;
}
