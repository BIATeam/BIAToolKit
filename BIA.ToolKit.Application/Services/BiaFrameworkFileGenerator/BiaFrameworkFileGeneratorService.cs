namespace BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using RazorLight;

    public class BiaFrameworkFileGeneratorService
    {
        private readonly IConsoleWriter consoleWriter;
        private readonly BiaFrameworkFileGeneratorFactory biaFrameworkFileGeneratorFactory;
        private RazorLightEngine razorLightEngine;
        private IBiaFrameworkFileGenerator biaFrameworkFileGenerator;

        public BiaFrameworkFileGeneratorService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
            biaFrameworkFileGeneratorFactory = new BiaFrameworkFileGeneratorFactory(this, consoleWriter);
        }

        public bool Init(Version version)
        {
            biaFrameworkFileGenerator = biaFrameworkFileGeneratorFactory.GetBiaFrameworkFileGenerator(version);
            if (biaFrameworkFileGenerator is null)
                return false;

            razorLightEngine = new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(biaFrameworkFileGenerator.GetType().Assembly, biaFrameworkFileGenerator.TemplatesNamespace)
                .UseMemoryCachingProvider()
                .Build();

            return true;
        }

        public async Task GenerateDto(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            await biaFrameworkFileGenerator.GenerateDto(project, entityInfo, domainName, mappingEntityProperties);
        }

        public async Task<string> GenerateFromTemplate(string templateKey, object model)
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
                consoleWriter.AddMessageLine($"ERROR: Fail to generate from template {templateKey} : {ex.Message}", "red");
            }

            return content;
        }

        public async Task GenerateFile(string content, string path)
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
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"ERROR: Fail to generate file : {ex.Message}", color: "red");
            }
        }
    }
}
