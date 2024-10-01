namespace BIA.ToolKit.Application.Services.FileGenerator
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
        private const string TemplateKey_Dto = "DtoTemplate.cshtml";
        private readonly RazorLightEngine razorLightEngine;
        private readonly IConsoleWriter consoleWriter;

        public FileGeneratorService(IConsoleWriter consoleWriter)
        {
            razorLightEngine = new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(typeof(FileGeneratorService).Assembly, EmbeddedResourcesNamespace)
                .UseMemoryCachingProvider()
                .Build();
            this.consoleWriter = consoleWriter;
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
                Properties = mappingEntityProperties.Select(x => new PropertyModel() 
                {
                    Name = x.MappingName,
                    Type = x.MappingType
                }).ToList()
            };

            consoleWriter.AddMessageLine($"Generate DTO file...");

            var content = await GenerateFromTemplate(TemplateKey_Dto, model);
            var destPath = Path.Combine(
                project.Folder, 
                Constants.FolderDotNet, 
                string.Join(".", project.CompanyName, project.Name, "Domain", "Dto"),
                model.DomainName, 
                $"{model.DtoName}.cs");

            await GenerateFile(content, destPath);

            consoleWriter.AddMessageLine($"DTO file successfully generated !");
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
                content = await razorLightEngine.CompileRenderAsync(TemplateKey_Dto, model);
                content = content.Remove(0, 2);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Fail to generate from template {templateKey} : {ex.Message}", color: "red");
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
                consoleWriter.AddMessageLine($"Fail to generate file : {ex.Message}", color: "red");
            }
        }
    }
}
