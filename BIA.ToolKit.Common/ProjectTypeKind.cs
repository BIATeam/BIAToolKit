namespace BIA.ToolKit.Common
{
    /// <summary>
    /// High-level project type presets used by the "Normal" creation mode.
    /// </summary>
    public enum ProjectTypeKind
    {
        /// <summary>Front-end + API + Database + Worker Service.</summary>
        Complete,

        /// <summary>API wired to an existing database — only the Database feature is checked (no deploy, no front-end, no worker, no auth).</summary>
        ApiWithExistingDb,
    }
}
