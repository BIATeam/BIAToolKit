namespace BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures
{
    public enum RegenerableFeatureStatus
    {
        Ready = 0,
        Missing = 1,
        Error = 2,

        /// <summary>CRUD is blocked because no corresponding DTO history entry was found.</summary>
        BlockedNoDtoHistory = 3,

        /// <summary>CRUD or DTO is blocked because the parent entity is not present in the generation history.</summary>
        BlockedParentNotMigrated = 4,
    }
}
