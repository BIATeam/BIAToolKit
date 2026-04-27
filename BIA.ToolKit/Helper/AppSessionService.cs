namespace BIA.ToolKit.Helper
{
    using CommunityToolkit.Mvvm.ComponentModel;

    /// <summary>
    /// App-wide, in-memory session state shared across view models.
    /// Non-persisted on purpose (DEV mode must not survive a restart).
    /// </summary>
    public partial class AppSessionService : ObservableObject
    {
        [ObservableProperty]
        private bool isDeveloperMode;
    }
}
