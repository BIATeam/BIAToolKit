namespace BIA.ToolKit.Application.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.ViewModel;
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

    public sealed record RepositoryChangedMessage(RepositoryViewModel OldRepository, RepositoryViewModel NewRepository);

    public sealed record RepositoryDeletedMessage(RepositoryViewModel Repository);

    public sealed record RepositoryAddedMessage(RepositoryViewModel Repository);

    public sealed record RepositoryVersionXYZChangedMessage(RepositoryViewModel Repository);

    public sealed record RepositoryReleaseDataUpdatedMessage(RepositoryViewModel Repository);

    public sealed record OpenRepositoryFormMessage(RepositoryViewModel Repository, RepositoryFormMode Mode);

    // --- Features ---

    public sealed record OriginFeatureSettingsChangedMessage(List<FeatureSetting> FeatureSettings);

    // --- Update ---

    public sealed record NewVersionAvailableMessage;

    // --- UI coordination ---

    public sealed record ExecuteActionWithWaiterMessage(Func<Task> Action);

    public sealed record NavigateToConfigTabMessage;

    // --- Enums (moved from UIEventBroker) ---

    public enum RepositoryFormMode
    {
        Create,
        Edit
    }
}
