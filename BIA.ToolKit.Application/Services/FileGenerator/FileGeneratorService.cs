namespace BIA.ToolKit.Application.Services.FileGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator.RazorModels;
    using BIA.ToolKit.Application.ViewModel;
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

        public async Task GenerateDto(EntityInfo entity, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            var model = new DtoModel
            {
                CompanyName = entity.CompagnyName,
                ProjectName = entity.ProjectName
            };

            consoleWriter.AddMessageLine($"Generate DTO file...");
            await Generate(TemplateKey_Dto, model);
        }

        private async Task Generate(string templateKey, object model)
        {
            try
            {
                var generatedContent = await razorLightEngine.CompileRenderAsync(TemplateKey_Dto, model);
                consoleWriter.AddMessageLine($"Generation finished sucessfully !");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Fail to generate file from template {templateKey} : {ex.Message}", color: "red");
            }
        }
    }
}
