﻿namespace BIA.ToolKit.Application.Services.FileGenerator
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
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualBasic.FileIO;
    using Mono.TextTemplating;
    using Newtonsoft.Json;

    public class FileGeneratorService
    {
        const string BiaToolKitMarkupBeginPattern = "// BIAToolKit - Begin {0}";
        const string BiaToolKitMarkupEndPattern = "// BIAToolKit - End {0}";
        const string BiaToolKitMarkupPartialBeginPattern = "// BIAToolKit - Begin Partial {0} {1}";
        const string BiaToolKitMarkupPartialEndPattern = "// BIAToolKit - End Partial {0} {1}";

        private readonly FileGeneratorVersionFactory fileGeneratorFactory;
        private IFileGeneratorVersion fileGenerator;
        private readonly TemplateGenerator templateGenerator;
        private readonly IConsoleWriter consoleWriter;
        private readonly List<Manifest> manifests = [];
        private Project currentProject;
        private Manifest currentManifest;
        private string templatesPath;
        private string currentDomain;
        private string currentEntityName;
        private string currentEntityNamePlural;
        private string currentAngularFront;
        private string currentParentName;
        private string currentParentNamePlural;
        private bool currentGenerateFront = true;
        private bool currentGenerateBack = true;
        public Manifest.Feature CurrentFeature { get; private set; }

        public bool IsInit { get; private set; }

        public FileGeneratorService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
            fileGeneratorFactory = new FileGeneratorVersionFactory(consoleWriter);
            templateGenerator = new TemplateGenerator();
            templateGenerator.Refs.Add(typeof(Manifest).Assembly.Location);
            LoadTemplatesManifests();
        }

        public void EventBroker_OnProjectChanged(Project project, UIEventBroker.TabItemModifyProjectEnum currentTabItem)
        {
            if(project is null)
            {
                IsInit = false;
                return;
            }

            if (project != currentProject)
            {
                Init(project);
            }
        }

        public void Init(Project project)
        {
            IsInit = false;

            // Parse version of project
            if (!Version.TryParse(project.FrameworkVersion, out Version projectVersion))
            {
                consoleWriter.AddMessageLine($"File generator : invalid project version", "red");
                return;
            }

            // Stop init for projects under version 4.0.0
            if(projectVersion < new Version(4, 0))
            {
                return;
            }

            // Get compatible file generator version
            currentProject = project;
            fileGenerator = fileGeneratorFactory.GetFileGeneratorVersion(projectVersion);
            if (fileGenerator is null)
            {
                consoleWriter.AddMessageLine($"File generator : incompatible project version {projectVersion}", "red");
                return;
            }

            // Parse file generator version number (_X_Y_Z)
            var regex = new Regex(@"^[^_]+(.+)$");
            var match = regex.Match(fileGenerator.GetType().Name);
            if (!match.Success)
            {
                consoleWriter.AddMessageLine($"File generator : invalid file generator version", "red");
                return;
            }
            var fileGeneratorVersion = match.Groups[1].Value;

            // Search templates path based on file generator version (_X_Y_Z)
            templatesPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileGeneratorVersion, "Templates");
            if (!Directory.Exists(templatesPath))
            {
                consoleWriter.AddMessageLine($"File generator : no templates found for version {fileGeneratorVersion}", "red");
                return;
            }

            // Search template manifest based on file generator version transofmred (_X_Y_Z -> X.Y.Z)
            var manifestVersion = fileGeneratorVersion.Replace("_", ".")[1..fileGeneratorVersion.Length];
            currentManifest = manifests.FirstOrDefault(m => m.Version.ToString() == manifestVersion);
            if (currentManifest is null)
            {
                consoleWriter.AddMessageLine($"File generator : no manifest for version {manifestVersion}", "red");
                return;
            }

            IsInit = true;
        }

        /// <summary>
        /// Check if current project is compatible for generation with current file generator service.
        /// </summary>
        /// <returns></returns>
        public bool IsProjectCompatible()
        {
            return Version.TryParse(currentProject.FrameworkVersion, out Version projectVersion) && projectVersion >= new Version(5, 0);
        }

        public async Task GenerateDtoAsync(EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties, string ancestorTeam = null)
        {
            try
            {
                consoleWriter.AddMessageLine($" === GENERATE DTO ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                var templateModel = fileGenerator.GetDtoTemplateModel(currentProject, entityInfo, domainName, mappingEntityProperties, ancestorTeam);
                var dtoFeature = currentManifest.Features.SingleOrDefault(f => f.Name == "DTO")
                    ?? throw new KeyNotFoundException($"no DTO feature for template manifest {currentManifest.Version}");

                currentDomain = domainName;
                currentEntityName = entityInfo.Name;
                currentEntityNamePlural = string.Empty;
                currentParentName = string.Empty;
                currentParentNamePlural = string.Empty;
                CurrentFeature = dtoFeature;

                await GenerateTemplatesFromManifestFeatureAsync(dtoFeature, templateModel);
                consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"DTO generation failed : {ex}", color: "red");
            }
        }

        public async Task GenerateOptionAsync(EntityInfo entityInfo, string entityNamePlural, string domainName, string displayName, string angularFront)
        {
            try
            {
                consoleWriter.AddMessageLine($" === GENERATE OPTION ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                var templateModel = fileGenerator.GetOptionTemplateModel(entityInfo, entityNamePlural, domainName, displayName);
                var optionFeature = currentManifest.Features.SingleOrDefault(f => f.Name == "Option")
                    ?? throw new KeyNotFoundException($"no Option feature for template manifest {currentManifest.Version}");

                currentDomain = domainName;
                currentEntityName = entityInfo.Name;
                currentEntityNamePlural = entityNamePlural;
                currentAngularFront = angularFront;
                currentParentName = string.Empty;
                currentParentNamePlural = string.Empty;
                currentGenerateBack = false;
                CurrentFeature = optionFeature;

                await GenerateTemplatesFromManifestFeatureAsync(optionFeature, templateModel);

                consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Option generation failed : {ex}", color: "red");
            }
        }

        public async Task GenerateCRUDAsync(EntityInfo entityInfo, string entityNamePlural, string domainName, string displayItemName, string angularFront, bool generateBack = true, bool generatedFront = true, bool isTeam = false, List<string> optionItems = null, bool hasParent = false, string parentName = null, string parentNamePlural = null)
        {
            try
            {
                consoleWriter.AddMessageLine($" === GENERATE CRUD ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                var templateModel = fileGenerator.GetCrudTemplateModel(entityInfo, entityNamePlural, domainName, displayItemName, isTeam, optionItems, hasParent, parentName, parentNamePlural);
                var optionFeature = currentManifest.Features.SingleOrDefault(f => f.Name == "CRUD")
                    ?? throw new KeyNotFoundException($"no CRUD feature for template manifest {currentManifest.Version}");

                currentDomain = domainName;
                currentEntityName = entityInfo.Name.Replace("dto", string.Empty, StringComparison.InvariantCultureIgnoreCase);
                currentEntityNamePlural = entityNamePlural;
                currentParentName = parentName;
                currentParentNamePlural = parentNamePlural;
                currentAngularFront = angularFront;
                currentGenerateBack = generateBack;
                currentGenerateFront = generatedFront;
                CurrentFeature = optionFeature;

                await GenerateTemplatesFromManifestFeatureAsync(optionFeature, templateModel);

                consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"CRUD generation failed : {ex}", color: "red");
            }
        }

        private void LoadTemplatesManifests()
        {
            var manifestsFiles = Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "manifest.json", System.IO.SearchOption.AllDirectories).ToList();
            manifestsFiles.ForEach(m => manifests.Add(JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(m))));
        }

        private async Task GenerateTemplatesFromManifestFeatureAsync(Manifest.Feature manifestFeature, object model)
        {
            if (currentGenerateFront)
            {
                await GenerateAngularTemplates(manifestFeature.AngularTemplates, model);
            }

            if (currentGenerateBack)
            {
                await GenerateDotNetTemplatesAsync(manifestFeature.DotNetTemplates, model);
            }
        }

        private async Task GenerateDotNetTemplatesAsync(IEnumerable<Manifest.Feature.Template> templates, object model)
        {
            foreach (var template in templates)
            {
                var templatePath = Path.Combine(templatesPath, Constants.FolderDotNet, template.InputPath);
                await GenerateFromTemplateAsync(template, templatePath, model, GetDotNetTemplateOutputPath(template.OutputPath, currentProject, currentDomain, currentEntityName, currentEntityNamePlural));
            }
        }

        public static string GetDotNetTemplateOutputPath(string templateOutputPath, Project project, string domainName, string entityName, string entityNamePlural)
        {
            var projectName = $"{project.CompanyName}.{project.Name}";
            var dotNetProjectPath = Path.Combine(project.Folder, Constants.FolderDotNet);
            return Path.Combine(
                dotNetProjectPath, 
                templateOutputPath
                    .Replace("{Project}", projectName)
                    .Replace("{Domain}", domainName)
                    .Replace("{Entity}", entityName)
                    .Replace("{EntityPlural}", entityNamePlural)
            );
        }

        private async Task GenerateAngularTemplates(IEnumerable<Manifest.Feature.Template> templates, object model)
        {
            foreach (var template in templates)
            {
                var templatePath = Path.Combine(templatesPath, currentAngularFront, template.InputPath);
                await GenerateFromTemplateAsync(template, templatePath, model, GetAngularTemplateOutputPath(template.OutputPath, currentProject, currentEntityName, currentEntityNamePlural, currentParentName, currentParentNamePlural));
            }
        }

        public static string GetAngularTemplateOutputPath(string templateOutputPath, Project project, string entityName, string entityNamePlural, string parentName, string parentNamePlural)
        {
            var angularProjectPath = Path.Combine(project.Folder, Constants.FolderAngular);
            var parentRelativePathSearchRootFolder = Path.Combine(angularProjectPath, @"src\app\features\");

            var parentRelativePath = Directory.EnumerateDirectories(parentRelativePathSearchRootFolder, parentNamePlural.ToKebabCase(), System.IO.SearchOption.AllDirectories).SingleOrDefault();
            parentRelativePath = !string.IsNullOrWhiteSpace(parentRelativePath) ? parentRelativePath.Replace(parentRelativePathSearchRootFolder, string.Empty) : string.Empty;
            var parentChildrenRelativePath = !string.IsNullOrWhiteSpace(parentRelativePath) ? Path.Combine(parentRelativePath, "children") : string.Empty;

            return Path.Combine(
                angularProjectPath, 
                templateOutputPath
                    .Replace("{Entity}", entityName.ToKebabCase())
                    .Replace("{EntityPlural}", entityNamePlural.ToKebabCase())
                    .Replace("{ParentRelativePath}", parentRelativePath)
                    .Replace("{ParentChildrenRelativePath}", parentChildrenRelativePath)
                    .Replace("{Parent}", parentName.ToKebabCase())
                    .Replace(@"\\\\", @"\\")
            );
        }

        private async Task GenerateFromTemplateAsync(Manifest.Feature.Template template, string templatePath, object model, string outputPath)
        {
            try
            {
                var relativeOutputPath = outputPath.Replace(currentProject.Folder, string.Empty);
                consoleWriter.AddMessageLine($"Generating {(template.IsPartial ? "partial content into" : "file")} {relativeOutputPath} ...");
                var relativeTemplatePath = templatePath.Replace(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), string.Empty);
                consoleWriter.AddMessageLine($"Using template file {relativeTemplatePath}", color: "darkgray");

                var generationTemplatePath = Path.GetTempFileName();
                var templateContent = await File.ReadAllTextAsync(templatePath);
                // Replace the load of assembly based on $(TargetPath) from template content to avoid generation errors
                templateContent = templateContent.Replace("<#@ assembly name=\"$(TargetPath)\" #>", "<#@ #>");
                await File.WriteAllTextAsync(generationTemplatePath, templateContent);

                // Inject Model parameter for template generation
                templateGenerator.ClearSession();
                var templateGeneratorSession = templateGenerator.GetOrCreateSession();
                templateGeneratorSession.Add("Model", model);

                // Generate content from template into temp file
                var generatedTemplatePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(outputPath));
                var success = await templateGenerator.ProcessTemplateAsync(generationTemplatePath, generatedTemplatePath);
                File.Delete(generationTemplatePath);
                if (!success)
                {
                    throw new Exception(JsonConvert.SerializeObject(templateGenerator.Errors));
                }

                // Check if generated content has any line
                var generatedTemplateContent = (await File.ReadAllLinesAsync(generatedTemplatePath)).ToList();
                File.Delete(generatedTemplatePath);
                if (generatedTemplateContent.Count == 0)
                {
                    consoleWriter.AddMessageLine("Ignored : generated content is empty", "orange");
                    return;
                }

                if (template.IsPartial)
                {
                    await WritePartialContentAsync(template, outputPath, relativeOutputPath, generatedTemplateContent);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    await File.WriteAllLinesAsync(outputPath, generatedTemplateContent);
                }

                consoleWriter.AddMessageLine($"Success !", "lightgreen");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Generate from template failed : {ex}", color: "red");
            }
        }

        private async Task WritePartialContentAsync(Manifest.Feature.Template template, string outputPath, string relativeOutputPath, List<string> generatedTemplateContent)
        {
            var partialInsertionMarkup = template.PartialInsertionMarkup
                .Replace("{Parent}", currentParentName);

            var outputContent = (await File.ReadAllLinesAsync(outputPath)).ToList();
            var biaToolKitMarkupBegin = string.Format(BiaToolKitMarkupBeginPattern, partialInsertionMarkup);
            var biaToolKitMarkupEnd = string.Format(BiaToolKitMarkupEndPattern, partialInsertionMarkup);
            if (!outputContent.Any(line => line.Trim().Equals(biaToolKitMarkupBegin)) || !outputContent.Any(line => line.Trim().Equals(biaToolKitMarkupEnd)))
            {
                throw new Exception($"Unable to find insertion markup {partialInsertionMarkup} into {relativeOutputPath}");
            }

            var biaToolKitMarkupPartialBeginPattern = string.Format(BiaToolKitMarkupPartialBeginPattern, partialInsertionMarkup, currentEntityName);
            var biaToolKitMarkupPartialEndPattern = string.Format(BiaToolKitMarkupPartialEndPattern, partialInsertionMarkup, currentEntityName);
            // Partial content already exists
            if (outputContent.Any(line => line.Trim().Equals(biaToolKitMarkupPartialBeginPattern)) && outputContent.Any(line => line.Trim().Equals(biaToolKitMarkupPartialEndPattern)))
            {
                var indexBegin = outputContent.FindIndex(line => line.Trim().Equals(biaToolKitMarkupPartialBeginPattern));
                var indexEnd = outputContent.FindIndex(line => line.Trim().Equals(biaToolKitMarkupPartialEndPattern));
                // Replace previous generated content by new one
                outputContent.RemoveRange(indexBegin, indexEnd - indexBegin + 1);
                outputContent.InsertRange(indexBegin, generatedTemplateContent);
            }
            else
            {
                var indexBegin = outputContent.FindIndex(line => line.Contains(biaToolKitMarkupEnd));
                outputContent.InsertRange(indexBegin, generatedTemplateContent);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            await File.WriteAllLinesAsync(outputPath, outputContent);
        }
    }
}
