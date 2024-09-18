namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator
{
    public enum GenerationType
    {
        WebApi,
        Front,
    }

    public enum FeatureType
    {
        CRUD,
        Option,
        Team
    }

    public enum CRUDDataUpdateType
    {
        Properties,
        Block,
        Child,
        Option,
        OptionField,
        Display,
        Parent,
        NoParent,
        AncestorTeam,
        Nested,
        // Partial
        Config,
        Dependency,
        Navigation,
        Permission,
        Rights,
        Routing,
        TeamTypeId,
        TeamTypeModelBuilder,
        TeamTypeRoleModelBuilder,
        RoleModelBuilder,
        RoleId,
        TeamTypeIdConstants,
        TeamTypeRightPrefixConstants,
        AllEnvironment,
        TeamConfig,
        AuthController,
        TeamMapperUsing,
        TeamMapperParentTeamId,
        TeamMapperParentTeamTitle
    }

    public enum WebApiFileType
    {
        AppService,
        Controller,
        Dto,
        Entity,
        IAppService,
        Mapper,
        Partial
    }
}
