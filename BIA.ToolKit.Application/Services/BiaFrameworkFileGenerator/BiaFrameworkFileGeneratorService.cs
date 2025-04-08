namespace BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.TemplateGenerator._4_0_0.Templates.DotNet.DomainDto;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.Extensions.Logging;
    using Scriban;

    public class BiaFrameworkFileGeneratorService
    {
        private readonly IConsoleWriter consoleWriter;
        private readonly IServiceProvider serviceProvider;
        private readonly ILoggerFactory loggerFactory;
        private readonly BiaFrameworkFileGeneratorFactory biaFrameworkFileGeneratorFactory;
        private IBiaFrameworkFileGenerator biaFrameworkFileGenerator;

        public BiaFrameworkFileGeneratorService(IConsoleWriter consoleWriter, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            this.consoleWriter = consoleWriter;
            this.serviceProvider = serviceProvider;
            this.loggerFactory = loggerFactory;
            biaFrameworkFileGeneratorFactory = new BiaFrameworkFileGeneratorFactory(this, consoleWriter);
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
            catch(Exception ex)
            {
                consoleWriter.AddMessageLine($"ERROR: Fail to generate DTO : {ex}", color: "red");
            }
        }

        public async Task<string> GenerateFromTemplateWithRazor(Type templateType, object model)
        {
            using var htmlRenderer = new Microsoft.AspNetCore.Components.Web.HtmlRenderer(serviceProvider, loggerFactory);
            var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var dictionary = new Dictionary<string, object>
                {
                    { "Model", model }
                };

                var parameters = ParameterView.FromDictionary(dictionary);
                var output = await htmlRenderer.RenderComponentAsync(templateType, parameters);

                return output.ToHtmlString();
            });

            return html;
        }

        public static string GenerateFromTemplateWithScriban(object model)
        {
            var template = LoadTemplateFromEmbeddedResource(model);
            return template.Render(model, member => member.Name);
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

        private static Template LoadTemplateFromEmbeddedResource(object model)
        {
            var modelType = model.GetType();
            var templateResourcePath = modelType.FullName.Replace("Models", "Templates") + ".scriban";
            var modelAssembly = modelType.Assembly;
            using Stream stream = modelAssembly.GetManifestResourceStream(templateResourcePath)
                ?? throw new FileNotFoundException(templateResourcePath);
            using StreamReader reader = new(stream);
            string templateText = reader.ReadToEnd();
            return Template.Parse(templateText);
        }
    }
}
