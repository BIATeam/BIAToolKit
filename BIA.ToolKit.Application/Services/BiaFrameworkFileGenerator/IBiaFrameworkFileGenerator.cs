﻿namespace BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;

    public interface BiaFrameworkFileGenerator_4_0_0
    {
        Version BiaFrameworkVersion { get; }
        string TemplatesNamespace { get; }
        Task GenerateDto(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties);
    }
}
