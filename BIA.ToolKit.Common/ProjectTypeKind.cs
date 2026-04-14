namespace BIA.ToolKit.Common
{
    /// <summary>
    /// High-level project type presets used by the "Normal" creation mode.
    /// </summary>
    public enum ProjectTypeKind
    {
        /// <summary>Front-end + API + Database + Worker Service.</summary>
        Complete,

        /// <summary>API + Database (no Front-end).</summary>
        ApiAndDb,

        /// <summary>Database only (no Front-end, no Worker, no Auth).</summary>
        DbOnly,
    }
}
