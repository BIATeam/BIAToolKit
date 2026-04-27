namespace BIA.ToolKit.Application.Helper
{
    using BIA.ToolKit.Domain.Settings;

    /// <summary>
    /// Abstraction for loading and saving the application settings
    /// without coupling the Application layer to a specific storage backend
    /// (WPF Properties.Settings.Default, file, registry, ...).
    /// </summary>
    public interface ISettingsPersistence
    {
        /// <summary>
        /// Loads settings from the persistent store, returning a fully
        /// populated <see cref="BIATKSettings"/> (defaults applied if no
        /// previous value was persisted).
        /// </summary>
        BIATKSettings Load();

        /// <summary>
        /// Persists the given settings to the store.
        /// </summary>
        void Save(IBIATKSettings settings);
    }
}
