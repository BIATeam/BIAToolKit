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
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Services.FileGenerator.ModelProviders;
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

        private readonly FileGeneratorModelProviderFactory modelProviderFactory;
        private readonly IConsoleWriter consoleWriter;
        private readonly List<Manifest> manifests = [];
        private IFileGeneratorModelProvider modelProvider;
        private VersionedTemplateGenerator templateGenerator;
        private string templatesPath;
        private Project currentProject;
        private FileGeneratorContext currentContext;
        private Manifest currentManifest;

        public bool IsInit { get; private set; }

        public FileGeneratorService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
            modelProviderFactory = new FileGeneratorModelProviderFactory(consoleWriter);

            LoadTemplatesManifests();
        }

        public void EventBroker_OnProjectChanged(Project project, UIEventBroker.TabItemModifyProjectEnum currentTabItem)
        {
            if(project is null)
            {
                IsInit = false;
                currentProject = null;
                return;
            }

            if (project.Folder != currentProject?.Folder)
            {
                Init(project);
            }
        }

        public void Init(Project project)
        {
            IsInit = false;
            currentProject = project;

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
            modelProvider = modelProviderFactory.GetFileGeneratorVersion(projectVersion);
            if (modelProvider is null)
            {
                consoleWriter.AddMessageLine($"File generator : incompatible project version {projectVersion}", "red");
                return;
            }

            // Parse file generator version number (_X_Y_Z)
            var regex = new Regex(@"(_[0-9]+(?:_[0-9]+){0,2})[^0-9]*");
            var match = regex.Match(modelProvider.GetType().Name);
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

            // Init template generator
            templateGenerator = new VersionedTemplateGenerator(fileGeneratorVersion);
            // Add reference to assembly of Manifest class to the template generator
            templateGenerator.Refs.Add(typeof(Manifest).Assembly.Location);

            IsInit = true;
        }

        /// <summary>
        /// Check if current project is compatible for generation with current file generator service for CRUD or Option feature.
        /// </summary>
        /// <returns></returns>
        public bool IsProjectCompatibleForCrudOrOptionFeature()
        {
            return Version.TryParse(currentProject?.FrameworkVersion, out Version projectVersion) && projectVersion >= new Version(5, 0);
        }

        public async Task GenerateDtoAsync(FileGeneratorDtoContext dtoContext)
        {
            try
            {
                consoleWriter.AddMessageLine($"=== GENERATE DTO ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                var templateModel = modelProvider.GetDtoTemplateModel(dtoContext);
                var dtoFeature = GetCurrentManifestFeature("DTO");

                currentContext = dtoContext;

                await GenerateTemplatesFromManifestFeatureAsync(dtoFeature, templateModel);
                consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"DTO generation failed : {ex}", color: "red");
            }
        }

        public async Task GenerateOptionAsync(FileGeneratorOptionContext optionContext)
        {
            try
            {
                consoleWriter.AddMessageLine($"=== GENERATE OPTION ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                var templateModel = modelProvider.GetOptionTemplateModel(optionContext);
                var optionFeature = GetCurrentManifestFeature("Option");

                currentContext = optionContext;

                await GenerateTemplatesFromManifestFeatureAsync(optionFeature, templateModel);

                consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Option generation failed : {ex}", color: "red");
            }
        }

        public async Task GenerateCRUDAsync(FileGeneratorCrudContext crudContext)
        {
            try
            {
                consoleWriter.AddMessageLine($"=== GENERATE CRUD ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                if (crudContext.GenerateFront && crudContext.HasParent)
                {
                    crudContext.ComputeAngularParentLocation(currentProject.Folder);
                }

                var templateModel = modelProvider.GetCrudTemplateModel(crudContext);
                var crudFeature = GetCurrentManifestFeature("CRUD");

                currentContext = crudContext;

                await GenerateTemplatesFromManifestFeatureAsync(crudFeature, templateModel);

                consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"CRUD generation failed : {ex}", color: "red");
            }
        }

        public Manifest.Feature GetCurrentManifestFeature(string featureName)
        {
            return currentManifest.Features.SingleOrDefault(f => f.Name == featureName)
                    ?? throw new KeyNotFoundException($"no {featureName} feature for template manifest {currentManifest.Version}");
        }

        private void LoadTemplatesManifests()
        {
            var manifestsFiles = Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "manifest.json", System.IO.SearchOption.AllDirectories).ToList();
            manifestsFiles.ForEach(m => manifests.Add(JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(m))));
        }

        private async Task GenerateTemplatesFromManifestFeatureAsync(Manifest.Feature manifestFeature, object model)
        {
            if (currentContext.GenerateFront)
            {
                await GenerateAngularTemplates(manifestFeature.AngularTemplates, model);
            }

            if (currentContext.GenerateBack)
            {
                await GenerateDotNetTemplatesAsync(manifestFeature.DotNetTemplates, model);
            }
        }

        private async Task GenerateDotNetTemplatesAsync(IEnumerable<Manifest.Feature.Template> templates, object model)
        {
            foreach (var template in templates)
            {
                var templatePath = Path.Combine(templatesPath, Constants.FolderDotNet, template.InputPath);
                await GenerateFromTemplateAsync(template, templatePath, model, GetDotNetTemplateOutputPath(template.OutputPath, currentContext, currentProject.Folder));
            }
        }

        public static string GetDotNetTemplateOutputPath(string templateOutputPath, FileGeneratorContext context, string projectFolder)
        {
            return Path.Combine(
                Path.Combine(projectFolder, Constants.FolderDotNet), 
                templateOutputPath
                    .Replace("{Project}", $"{context.CompanyName}.{context.ProjectName}")
                    .Replace("{Domain}", context.DomainName)
                    .Replace("{Entity}", context.EntityName)
                    .Replace("{EntityPlural}", context.EntityNamePlural)
            );
        }

        private async Task GenerateAngularTemplates(IEnumerable<Manifest.Feature.Template> templates, object model)
        {
            foreach (var template in templates)
            {
                var templatePath = Path.Combine(templatesPath, Constants.FolderAngular, template.InputPath);
                await GenerateFromTemplateAsync(template, templatePath, model, GetAngularTemplateOutputPath(template.OutputPath, currentContext, currentProject.Folder));
            }
        }

        public static string GetAngularTemplateOutputPath(string templateOutputPath, FileGeneratorContext context, string projectFolder)
        {
            var outputPath = Path.Combine(
                Path.Combine(projectFolder, context.AngularFront),
                templateOutputPath
                    .Replace("{Entity}", context.EntityName.ToKebabCase())
                    .Replace("{EntityPlural}", context.EntityNamePlural.ToKebabCase())
                    .Replace("{Parent}", context.ParentName.ToKebabCase())
                    .Replace("{ParentPlural}", context.ParentNamePlural.ToKebabCase())
                    .Replace("{ParentRelativePath}", context.AngularParentFolderRelativePath)
                    .Replace("{ParentChildrenRelativePath}", context.AngularParentChildrenFolderRelativePath)
                    .Replace(@"\\", @"\"));

            return outputPath;
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
            var insertionMarkup = GetInsertionMarkup(template, currentContext);

            var outputContent = (await File.ReadAllLinesAsync(outputPath)).ToList();
            var insertionMarkupBegin = AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupBeginPattern, insertionMarkup), outputPath);
            var insertionMarkupEnd = AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupEndPattern, insertionMarkup), outputPath);
            if (!outputContent.Any(line => line.Trim().Equals(insertionMarkupBegin)) || !outputContent.Any(line => line.Trim().Equals(insertionMarkupEnd)))
            {
                throw new Exception($"Unable to find insertion markup {insertionMarkup} into {relativeOutputPath}");
            }

            (var partialInsertionMarkupBegin, var partialInsertionMarkupEnd) = GetPartialInsertionMarkups(currentContext, template, outputPath);
            // Partial content already exists
            if (outputContent.Any(line => line.Trim().Equals(partialInsertionMarkupBegin)) && outputContent.Any(line => line.Trim().Equals(partialInsertionMarkupEnd)))
            {
                var indexBegin = outputContent.FindIndex(line => line.Trim().Equals(partialInsertionMarkupBegin));
                var indexEnd = outputContent.FindIndex(line => line.Trim().Equals(partialInsertionMarkupEnd));
                // Replace previous generated content by new one
                outputContent.RemoveRange(indexBegin, indexEnd - indexBegin + 1);
                outputContent.InsertRange(indexBegin, generatedTemplateContent);
            }
            else
            {
                var indexBegin = outputContent.FindIndex(line => line.Contains(insertionMarkupEnd));
                outputContent.InsertRange(indexBegin, generatedTemplateContent);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            await File.WriteAllLinesAsync(outputPath, outputContent);
        }

        public static (string partialInsertionMarkupBegin, string partialInsertionMarkupEnd) GetPartialInsertionMarkups(FileGeneratorContext context, Manifest.Feature.Template template, string outputPath)
        {
            var partialInsertionMarkup = GetInsertionMarkup(template, context);

            return (
                AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupPartialBeginPattern, partialInsertionMarkup, context.EntityName), outputPath),
                AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupPartialEndPattern, partialInsertionMarkup, context.EntityName), outputPath)
                );
        }

        private static string GetInsertionMarkup(Manifest.Feature.Template template, FileGeneratorContext context)
        {
            return template.PartialInsertionMarkup.Replace("{Parent}", context.ParentName);
        }

        private static string AdaptBiaToolKitMarkup(string markup, string outputPath)
        {
            if (outputPath.EndsWith(".html"))
            {
                return markup.Replace("//", "<!--") + " -->";
            }

            return markup;
        }
    }
}
