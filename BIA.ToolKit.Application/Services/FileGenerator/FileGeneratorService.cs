﻿namespace BIA.ToolKit.Application.Services.FileGenerator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator.RazorModels;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Common.Helpers;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using RazorLight;

    public class FileGeneratorService
    {
        private const string EmbeddedResourcesNamespace = "BIA.ToolKit.Application.Services.FileGenerator.RazorTemplates";
        private const string TemplateKey_Mapper = "MapperTemplate.cshtml";
        private const string TemplateKey_Dto = "DtoTemplate.cshtml";
        private const string TemplateValue_BaseKeyType = "{BaseKeyType}";
        private readonly RazorLightEngine razorLightEngine;
        private readonly IConsoleWriter consoleWriter;

        public FileGeneratorService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
            razorLightEngine = new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(typeof(FileGeneratorService).Assembly, EmbeddedResourcesNamespace)
                .UseMemoryCachingProvider()
                .Build();
        }

        public async Task GenerateDto(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            var model = new DtoModel
            {
                CompanyName = project.CompanyName,
                ProjectName = project.Name,
                NameArticle = ComputeNameArticle(entityInfo.Name),
                DomainName = domainName,
                DtoName = entityInfo.Name + "Dto",
                EntityName = entityInfo.Name,
                BaseKeyType = entityInfo.BaseKeyType,
                Properties = mappingEntityProperties.Select(x => new PropertyModel()
                {
                    MappingName = x.MappingName,
                    EntityCompositeName = x.EntityCompositeName,
                    MappingType = x.MappingType,
                    MappingDateType = x.MappingDateType,
                    IsOption = x.IsOption,
                    IsOptionCollection = x.IsOptionCollection,
                    OptionType = x.OptionType,
                    IsRequired = x.IsRequired,
                    OptionDisplayProperty = x.OptionDisplayProperty,
                    OptionIdProperty = x.OptionIdProperty,
                    OptionEntityIdPropertyComposite = x.OptionEntityIdPropertyComposite,
                    OptionRelationType = x.OptionRelationType,
                    OptionRelationPropertyComposite = x.OptionRelationPropertyComposite,
                    OptionRelationFirstIdProperty = x.OptionRelationFirstIdProperty,
                    OptionRelationSecondIdProperty = x.OptionRelationSecondIdProperty,
                }).ToList(),
                EntityNamespace = entityInfo.Namespace,
                MapperName = entityInfo.Name + "Mapper",
                IsTeamType = entityInfo.BaseType.Contains("Team")
            };

            if(string.IsNullOrWhiteSpace(model.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{TemplateValue_BaseKeyType}' by corresponding type value inside the DTO and mapper after generation.", "orange");
                model.BaseKeyType = TemplateValue_BaseKeyType;
            }

            consoleWriter.AddMessageLine($"Generating DTO...");
            var dtoContent = await GenerateFromTemplate(TemplateKey_Dto, model);
            var dtoDestPath = Path.Combine(
                project.Folder, 
                Constants.FolderDotNet, 
                string.Join(".", project.CompanyName, project.Name, "Domain", "Dto"),
                model.DomainName, 
                $"{model.DtoName}.cs");
            await GenerateFile(dtoContent, dtoDestPath);
            consoleWriter.AddMessageLine($"DTO successfully generated !", "green");

            consoleWriter.AddMessageLine($"Generating mapper...");
            var mapperContent = await GenerateFromTemplate(TemplateKey_Mapper, model);
            var mapperDestPath = Path.Combine(
                Path.GetDirectoryName(entityInfo.Path),
                $"{model.MapperName}.cs");
            await GenerateFile(mapperContent, mapperDestPath);
            consoleWriter.AddMessageLine($"Mapper successfully generated !", "green");
        }

        private static string ComputeNameArticle(string name)
        {
            const string An = "an";
            const string A = "a";

            var lowerName = name.ToLower();
            return
                lowerName.StartsWith("a") ||
                lowerName.StartsWith("e") ||
                lowerName.StartsWith("i") ||
                lowerName.StartsWith("o") ||
                lowerName.StartsWith("u") ?
                An : A;
        }

        private async Task<string> GenerateFromTemplate(string templateKey, object model)
        {
            string content = null;

            try
            {
                content = await razorLightEngine.CompileRenderAsync(templateKey, model);

                //Remove \r\n from generated content to avoid empty first line
                content = content.Remove(0, 2);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"ERROR: Fail to generate from template {templateKey} : {ex.Message}", color: "red");
            }

            return content;
        }

        private async Task GenerateFile(string content, string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    throw new InvalidOperationException("Content to generate is empty");

                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }

                await File.WriteAllTextAsync(path, content);
            }
            catch(Exception ex)
            {
                consoleWriter.AddMessageLine($"ERROR: Fail to generate file : {ex.Message}", color: "red");
            }
        }
    }
}
