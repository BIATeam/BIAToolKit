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
        RightsForOption,
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
        TeamMapperParentTeamTitle,
        PlaneModuleChildPath,
        PlaneIndexHtml,
        PlaneIndexTsCanViewChildDeclaration,
        PlaneIndexTsCanViewChildSet,
        PlaneIndexTsOnViewChild
    }

    public enum WebApiFileType
    {
        AppService,
        OptionAppService,
        Controller,
        OptionsController,
        Dto,
        Entity,
        IAppService,
        IOptionAppService,
        Mapper,
        OptionMapper,
        Partial,
        Specification
    }
}
