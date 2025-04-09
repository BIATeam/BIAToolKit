namespace BIA.ToolKit.Application.Services.FileGenerator.Versions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;

    internal class FileGeneratorVersion_5_0_0(IConsoleWriter consoleWriter) : FileGeneratorVersion_4_0_0(consoleWriter), IFileGeneratorVersion
    {
        public new List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions =>
        [
            new("5.*"),
        ];

        protected override Templates._4_0_0.Models.EntityDtoModel CreateDtoEntityModel()
        {
            return new Templates._5_0_0.Models.EntityDtoModel();
        }

        public new object GetDtoTemplateModel(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            var model = base.GetDtoTemplateModel(project, entityInfo, domainName, mappingEntityProperties) as Templates._5_0_0.Models.EntityDtoModel;
            // Map additionnal properties of your model 
            return model;
        }
    }
}
