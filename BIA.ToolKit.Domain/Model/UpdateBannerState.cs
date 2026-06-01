namespace BIA.ToolKit.Domain.Model
{
    /// <summary>
    /// Mutually exclusive UI states of the Config-tab Update banner. The
    /// banner color, glyph, and action label all derive from this value
    /// through converters in BIA.ToolKit/Converters/.
    /// </summary>
    public enum UpdateBannerState
    {
        UpToDate,
        UpdateAvailable,
        Checking,
        NoSource,
        Failed,
    }
}
