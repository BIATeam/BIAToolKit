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
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.Extensions.Logging;

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
            await biaFrameworkFileGenerator.GenerateDto(project, entityInfo, domainName, mappingEntityProperties);
        }

        public async Task<string> GenerateFromTemplate(Type templateType, object model)
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
