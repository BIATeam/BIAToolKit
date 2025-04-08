namespace BIA.ToolKit.Application.Services.FileGenerator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator.Versions;
    using BIA.ToolKit.Application.TemplateGenerator;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using Microsoft.Extensions.Logging;
    using Mono.TextTemplating;
    using Newtonsoft.Json;

    public class FileGeneratorService
    {
        private readonly FileGeneratorVersionFactory biaFrameworkFileGeneratorFactory;
        private IFileGeneratorVersion biaFrameworkFileGenerator;
        private readonly TemplateGenerator templateGenerator;
        private readonly IConsoleWriter consoleWriter;

        public FileGeneratorService(IConsoleWriter consoleWriter, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            this.consoleWriter = consoleWriter;
            biaFrameworkFileGeneratorFactory = new FileGeneratorVersionFactory(this, consoleWriter);
            templateGenerator = new TemplateGenerator();
            templateGenerator.Refs.Add(typeof(TemplateGeneratorManifest).Assembly.Location);
        }

        public bool Init(Version version)
        {
            biaFrameworkFileGenerator = biaFrameworkFileGeneratorFactory.GetBiaFrameworkFileGenerator(version);
            return biaFrameworkFileGenerator is not null;
        }

        public async Task GenerateDto(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            try
            {
                await biaFrameworkFileGenerator.GenerateDto(project, entityInfo, domainName, mappingEntityProperties);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"ERROR: Fail to generate DTO : {ex}", color: "red");
            }
        }

        public async Task GenerateFromTemplateWithT4(string templatePath, object model, string outputPath)
        {
            var tempTemplatePath = Path.GetTempFileName();
            var templateContent = await File.ReadAllTextAsync(templatePath);
            templateContent = templateContent.Replace("<#@ assembly name=\"$(TargetPath)\" #>", "<#@ #>");
            await File.WriteAllTextAsync(tempTemplatePath, templateContent);

            templateGenerator.ClearSession();
            var templateGeneratorSession = templateGenerator.GetOrCreateSession();
            templateGeneratorSession.Add("Model", model);
            var success = await templateGenerator.ProcessTemplateAsync(tempTemplatePath, outputPath);
            File.Delete(tempTemplatePath);
            if (!success)
            {
                throw new Exception(JsonConvert.SerializeObject(templateGenerator.Errors));
            }
        }
    }
}
