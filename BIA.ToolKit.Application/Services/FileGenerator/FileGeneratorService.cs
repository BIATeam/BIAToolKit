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
    using Microsoft.VisualBasic.FileIO;
    using Mono.TextTemplating;
    using Newtonsoft.Json;

    public class FileGeneratorService
    {
        private readonly FileGeneratorVersionFactory fileGeneratorFactory;
        private IFileGeneratorVersion fileGenerator;
        private readonly TemplateGenerator templateGenerator;
        private readonly IConsoleWriter consoleWriter;
        private readonly List<Manifest> manifests = [];
        private Project currentProject;
        private Manifest currentManifest;
        private string currentTemplatePath;
        private string currentDomain;
        private string currentEntity;

        public FileGeneratorService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
            fileGeneratorFactory = new FileGeneratorVersionFactory(consoleWriter);
            templateGenerator = new TemplateGenerator();
            templateGenerator.Refs.Add(typeof(Manifest).Assembly.Location);
            LoadManifests();
        }

        public bool Init(Project project)
        {
            if (!Version.TryParse(project.FrameworkVersion, out Version projectVersion))
            {
                consoleWriter.AddMessageLine($"ERROR: invalid project version", "red");
                return false;
            }

            currentProject = project;
            fileGenerator = fileGeneratorFactory.GetBiaFrameworkFileGenerator(projectVersion);
            if (fileGenerator is null) 
                return false;

            var regex = new Regex(@"^[^_]+(.+)$");
            var match = regex.Match(fileGenerator.GetType().Name);
            if (!match.Success)
            {
                consoleWriter.AddMessageLine($"ERROR: invalid file generator version", "red");
                return false;
            }

            var fileGeneratorVersion = match.Groups[1].Value;
            var manifestVersion = fileGeneratorVersion.Replace("_", ".")[1..fileGeneratorVersion.Length];
            var currentProjectManifestJson = Path.Combine(currentProject.Folder, ".bia", "biatoolkit.templates.manifest.json");
            if (!File.Exists(currentProjectManifestJson))
            {
                currentManifest = manifests.FirstOrDefault(m => m.Version.ToString() == manifestVersion);
                if (currentManifest != null) 
                    File.WriteAllText(currentProjectManifestJson, JsonConvert.SerializeObject(currentManifest));
            }
            else
            {
                currentManifest = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(currentProjectManifestJson));
            }

            if (currentManifest is null)
            {
                consoleWriter.AddMessageLine($"ERROR: no manifest for version {manifestVersion}", "red");
                return false;
            }

            currentTemplatePath = Path.Combine(currentProject.Folder, ".bia", "Templates");
            if(!Directory.Exists(currentTemplatePath))
            {
                var templatesPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileGeneratorVersion, "Templates");
                FileSystem.CopyDirectory(templatesPath, currentTemplatePath);
            }

            return true;
        }

        public async Task GenerateDto(EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            try
            {
                consoleWriter.AddMessageLine($" === GENERATE DTO ===", color: "lightblue");
                var templateModel = fileGenerator.GetDtoTemplateModel(currentProject, entityInfo, domainName, mappingEntityProperties);
                var dtoFeature = currentManifest.Features.SingleOrDefault(f => f.Name == "DTO")
                    ?? throw new KeyNotFoundException($"No DTO feature for template manifest {currentManifest.Version}");
                currentDomain = domainName;
                currentEntity = entityInfo.Name;
                await GenerateTemplatesFromManifestFeature(dtoFeature, templateModel);
                consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Fail to generate DTO : {ex}", color: "red");
            }
        }

        private void LoadManifests()
        {
            var manifestsFiles = Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*manifest.json", System.IO.SearchOption.AllDirectories).ToList();
            manifestsFiles.ForEach(m => manifests.Add(JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(m))));
        }

        private async Task GenerateTemplatesFromManifestFeature(Manifest.Feature manifestFeature, object model)
        {
            await GenerateDotNetTemplates(manifestFeature.DotNetTemplates, model);
        }

        private async Task GenerateDotNetTemplates(IEnumerable<Manifest.Feature.Template> templates, object model)
        {
            foreach(var template in templates)
            {
                var templatePath = Path.Combine(currentTemplatePath, "DotNet", template.InputPath);
                await GenerateFromTemplate(templatePath, model, GetDotNetTemplateOutputPath(template.OutputPath, currentProject, currentDomain, currentEntity));
            }
        }

        private static string GetDotNetTemplateOutputPath(string templateOutputPath, Project project, string domainName, string entityName)
        {
            var projectName = $"{project.CompanyName}.{project.Name}";
            var dotNetProjectPath = Path.Combine(project.Folder, "DotNet");
            return Path.Combine(dotNetProjectPath, templateOutputPath.Replace("{Project}", projectName).Replace("{Domain}", domainName).Replace("{Entity}", entityName));
        }

        private async Task GenerateFromTemplate(string templatePath, object model, string outputPath)
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
    }
}
