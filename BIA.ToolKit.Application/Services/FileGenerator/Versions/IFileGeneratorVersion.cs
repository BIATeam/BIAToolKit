﻿namespace BIA.ToolKit.Application.Services.FileGenerator.Versions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;

    internal interface IFileGeneratorVersion
    {
        List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions { get; }
        object GetDtoTemplateModel(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties);
        object GetOptionTemplateModel(EntityInfo entityInfo, string entityNamePlural, string domainName, string displayName);
        object GetCrudTemplateModel(EntityInfo entityInfo, string entityNamePlural, string domainName, string displayItemName, bool isTeam = false, List<string> optionItems = null, bool hasParent = false, string parentName = null, string parentNamePlural = null);
    }
}
