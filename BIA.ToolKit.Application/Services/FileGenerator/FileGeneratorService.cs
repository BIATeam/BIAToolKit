namespace BIA.ToolKit.Application.Services.FileGenerator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator.Versions;
    using BIA.ToolKit.Application.Templates;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using Microsoft.Extensions.Logging;
    using Mono.TextTemplating;
    using Newtonsoft.Json;

    public class FileGeneratorService
    {
        private readonly FileGeneratorVersionFactory fileGeneratorFactory;
        private IFileGeneratorVersion fileGenerator;
        private readonly TemplateGenerator templateGenerator;
        private readonly IConsoleWriter consoleWriter;
        private readonly List<Manifest> manifests = [];
        private Manifest currentManifest;

        public FileGeneratorService(IConsoleWriter consoleWriter, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            this.consoleWriter = consoleWriter;
            fileGeneratorFactory = new FileGeneratorVersionFactory(this, consoleWriter);
            templateGenerator = new TemplateGenerator();
            templateGenerator.Refs.Add(typeof(Manifest).Assembly.Location);
            LoadManifests();
        }

        public bool Init(Version version)
        {
            fileGenerator = fileGeneratorFactory.GetBiaFrameworkFileGenerator(version);
            if (fileGenerator is null) 
                return false;

            var regex = new Regex(@"^[^_]+_(.+)$");
            var match = regex.Match(fileGenerator.GetType().Name);
            if (match.Success)
            {
                var versionManifest = match.Groups[1].Value.Replace("_", ".");
                currentManifest = manifests.FirstOrDefault(m => m.Version.ToString() == versionManifest);
            }
            if (currentManifest is null)
                return false;
            
            return true;
        }

        public async Task GenerateDto(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            try
            {
                consoleWriter.AddMessageLine($" === GENERATE DTO ===", color: "lightblue");
                await fileGenerator.GenerateDto(project, entityInfo, domainName, mappingEntityProperties);
                consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Fail to generate DTO : {ex}", color: "red");
            }
        }

        public async Task GenerateFromTemplateWithT4(string templatePath, object model, string outputPath)
        {
            try
            {
                consoleWriter.AddMessageLine($"Generating file {Path.GetFileName(outputPath)} ...");
                consoleWriter.AddMessageLine($"Using template file {templatePath}", color: "darkgray");

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

                consoleWriter.AddMessageLine($"Success !", "lightgreen");
            }
            catch(Exception ex)
            {
                consoleWriter.AddMessageLine($"Fail to generate : {ex}", color: "red");
            }
        }

        private void LoadManifests()
        {
            var manifestsFiles = Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*manifest.json", SearchOption.AllDirectories).ToList();
            manifestsFiles.ForEach(m => manifests.Add(JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(m))));
        }
    }
}
