namespace BIA.ToolKit.Application.Services.FileGenerator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json.Serialization;
    using System.Text.Json;
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
    using Newtonsoft.Json.Converters;
    using System.Diagnostics;
    using System.Threading;
    using static BIA.ToolKit.Application.Templates.Manifest.Feature;

    public class FileGeneratorService
    {
        const string BiaToolKitMarkupBeginPattern = "// BIAToolKit - Begin {0}";
        const string BiaToolKitMarkupEndPattern = "// BIAToolKit - End {0}";
        const string BiaToolKitMarkupPartialBeginPattern = "// BIAToolKit - Begin Partial {0} {1}";
        const string BiaToolKitMarkupPartialEndPattern = "// BIAToolKit - End Partial {0} {1}";

        private readonly FileGeneratorModelProviderFactory _modelProviderFactory;
        private readonly IConsoleWriter _consoleWriter;
        private readonly List<Manifest> _manifests = [];
        private IFileGeneratorModelProvider _modelProvider;
        private string _modelProviderVersion;
        private string _templatesPath;
        private Project _currentProject;
        private FileGeneratorContext _currentContext;
        private Manifest _currentManifest;
        private string _prettierAngularProjectPath;
        private bool _fromUnitTest;

        public bool IsInit { get; private set; }

        public FileGeneratorService(IConsoleWriter consoleWriter)
        {
            this._consoleWriter = consoleWriter;
            _modelProviderFactory = new FileGeneratorModelProviderFactory(consoleWriter);

            LoadTemplatesManifests();
        }

        public async Task Init(Project project, bool fromUnitTest = false)
        {
            if (project is null)
            {
                IsInit = false;
                _currentProject = null;
                return;
            }

            IsInit = false;
            _currentProject = project;
            _fromUnitTest = fromUnitTest;

            try
            {
                await Task.Run(() =>
                {
                    // Parse version of project
                    ParseProjectVersion(project, out Version projectVersion);
                    // Stop init for projects under version 4.0.0
                    if (projectVersion < new Version(4, 0))
                    {
                        return;
                    }

                    // Load compatible model provider
                    LoadModelProvider(projectVersion);
                    // Parse file generator version number (_X_Y_Z)
                    _modelProviderVersion = ParseModelProviderVersion();
                    // Search templates path based on file generator version (_X_Y_Z)
                    FindTemplatesPath(_modelProviderVersion);
                    // Search template manifest based on file generator version transofmred (_X_Y_Z -> X.Y.Z)
                    SetCurrentManifest(_modelProviderVersion);

                    IsInit = true;
                });
            }
            catch(Exception ex)
            {
                _consoleWriter.AddMessageLine($"File generator : {ex.Message}", "red");
            }
        }

        private void SetCurrentManifest(string modelProviderVersion)
        {
            var manifestVersion = modelProviderVersion.Replace("_", ".")[1..modelProviderVersion.Length];
            _currentManifest = _manifests.FirstOrDefault(m => m.Version.ToString() == manifestVersion);
            if (_currentManifest is null)
            {
                _consoleWriter.AddMessageLine($"no manifest for version {manifestVersion}", "red");
                return;
            }
        }

        private void FindTemplatesPath(string modelProviderVersion)
        {
            _templatesPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), modelProviderVersion, "Templates");
            if (!Directory.Exists(_templatesPath))
            {
                throw new Exception($"no templates found for version {modelProviderVersion}");
            }
        }

        private string ParseModelProviderVersion()
        {
            var regex = new Regex(@"(_[0-9]+(?:_[0-9]+){0,2})[^0-9]*");
            var match = regex.Match(_modelProvider.GetType().Name);
            if (!match.Success)
            {
                throw new Exception($"invalid model provider version");
            }
            return match.Groups[1].Value;
        }

        private void LoadModelProvider(Version projectVersion)
        {
            _modelProvider = _modelProviderFactory.GetModelProvider(projectVersion);
            if (_modelProvider is null)
            {
                throw new Exception($"incompatible project version {projectVersion}");
            }
        }

        private static void ParseProjectVersion(Project project, out Version projectVersion)
        {
            if (!Version.TryParse(project.FrameworkVersion, out projectVersion))
            {
                throw new Exception($"invalid project version");
            }
        }

        /// <summary>
        /// Check if current project is compatible for generation with current file generator service for CRUD or Option feature.
        /// </summary>
        /// <returns></returns>
        public bool IsProjectCompatibleForCrudOrOptionFeature()
        {
            return Version.TryParse(_currentProject?.FrameworkVersion, out Version projectVersion) && projectVersion >= new Version(5, 0);
        }

        public void SetPrettierAngularProjectPath(string path)
        {
            if(!Directory.Exists(path))
            {
                throw new Exception($"Unable to init prettier from unexisting front folder {path}");
            }
            _prettierAngularProjectPath = path;
        }

        public async Task GenerateDtoAsync(FileGeneratorDtoContext dtoContext)
        {
            try
            {
                _consoleWriter.AddMessageLine($"=== GENERATE DTO ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                var templateModel = _modelProvider.GetDtoTemplateModel(dtoContext);
                var dtoFeature = GetCurrentManifestFeature(Manifest.Feature.FeatureType.Dto);

                _currentContext = dtoContext;

                await GenerateTemplatesFromManifestFeatureAsync(dtoFeature, templateModel);
                _consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"DTO generation failed : {ex}", color: "red");
            }
        }

        public async Task GenerateOptionAsync(FileGeneratorOptionContext optionContext)
        {
            try
            {
                _consoleWriter.AddMessageLine($"=== GENERATE OPTION ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                var templateModel = _modelProvider.GetOptionTemplateModel(optionContext);
                var optionFeature = GetCurrentManifestFeature(Manifest.Feature.FeatureType.Option);

                _currentContext = optionContext;

                await GenerateTemplatesFromManifestFeatureAsync(optionFeature, templateModel);

                _consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"Option generation failed : {ex}", color: "red");
            }
        }

        public async Task GenerateCRUDAsync(FileGeneratorCrudContext crudContext)
        {
            try
            {
                _consoleWriter.AddMessageLine($"=== GENERATE CRUD ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                if (crudContext.GenerateFront && crudContext.HasParent)
                {
                    crudContext.ComputeAngularParentLocation(_currentProject.Folder);
                }

                var templateModel = _modelProvider.GetCrudTemplateModel(crudContext);
                var crudFeature = GetCurrentManifestFeature(Manifest.Feature.FeatureType.Crud);

                _currentContext = crudContext;

                await GenerateTemplatesFromManifestFeatureAsync(crudFeature, templateModel);

                _consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"CRUD generation failed : {ex}", color: "red");
            }
        }

        public Manifest.Feature GetCurrentManifestFeature(Manifest.Feature.FeatureType featureType)
        {
            return _currentManifest.Features.SingleOrDefault(f => f.Type == featureType)
                    ?? throw new KeyNotFoundException($"no {featureType} feature for template manifest {_currentManifest.Version}");
        }

        private void LoadTemplatesManifests()
        {
            var jsonSerializersettings = new JsonSerializerSettings
            {
                Converters = { new StringEnumConverter() },
                MissingMemberHandling = MissingMemberHandling.Error
            };
            var manifestsFiles = Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "manifest.json", System.IO.SearchOption.AllDirectories).ToList();
            manifestsFiles.ForEach(m => _manifests.Add(JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(m), jsonSerializersettings)));
        }

        private async Task GenerateTemplatesFromManifestFeatureAsync(Manifest.Feature manifestFeature, object model)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _consoleWriter.AddMessageLine("Please wait...", "darkgray");

            var generateAngularTemplatesTask = Task.CompletedTask;
            var generateDotNetTemplatesTask = Task.CompletedTask;

            if (_currentContext.GenerateFront)
            {
                generateAngularTemplatesTask = GenerateAngularTemplates(manifestFeature.AngularTemplates, model);
            }

            if (_currentContext.GenerateBack)
            {
                generateDotNetTemplatesTask = GenerateDotNetTemplatesAsync(manifestFeature.DotNetTemplates, model);
            }

            await Task.WhenAll(generateAngularTemplatesTask, generateDotNetTemplatesTask);

            stopwatch.Stop();
            _consoleWriter.AddMessageLine($"[Generated in {stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}min]", "darkgray");
        }

        private async Task GenerateDotNetTemplatesAsync(IEnumerable<Manifest.Feature.Template> templates, object model)
        {
            await RunGenerateTemplatesAsync(templates, model, GenerateDotNetTemplateAsync);
            var csharpFiles = _currentContext.GenerationReport.TemplatesGenerated
                .Select(x => GetDotNetTemplateOutputPath(x.OutputPath, _currentContext, _currentProject.Folder))
                .Where(x => Path.GetExtension(x).Equals(".cs"));
            
            foreach(var file in csharpFiles)
            {
                if(FileTransform.OrderUsingFromFile(file))
                {
                    _consoleWriter.AddMessageLine($"-> Usings ordered in {file}", "green");
                }
            }
        }

        private async Task GenerateDotNetTemplateAsync(Manifest.Feature.Template template, object model)
        {
            var templatePath = Path.Combine(_templatesPath, Constants.FolderDotNet, template.InputPath);
            await GenerateFromTemplateAsync(template, templatePath, model, GetDotNetTemplateOutputPath(template.OutputPath, _currentContext, _currentProject.Folder));
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
                    .Replace("{Parent}", context.ParentName)
            );
        }

        private async Task GenerateAngularTemplates(IEnumerable<Manifest.Feature.Template> templates, object model)
        {
            var angularProjectFolder = Path.Combine(_currentProject.Folder, _currentContext.AngularFront);
            
            if (!_fromUnitTest)
            {
                SetPrettierAngularProjectPath(angularProjectFolder);
            }

            await RunGenerateTemplatesAsync(templates, model, GenerateAngularTemplateAsync);
            await RunPrettierAsync(angularProjectFolder);
        }

        private async Task GenerateAngularTemplateAsync(Manifest.Feature.Template template, object model)
        {
            var templatePath = Path.Combine(_templatesPath, Constants.FolderAngular, template.InputPath);
            var outputPath = GetAngularTemplateOutputPath(template.OutputPath, _currentContext, _currentProject.Folder);
            await GenerateFromTemplateAsync(template, templatePath, model, outputPath);
        }

        private static async Task RunGenerateTemplatesAsync(IEnumerable<Manifest.Feature.Template> templates, object model, Func<Manifest.Feature.Template, object, Task> generateTemplateTask)
        {
            var groupTemplates = templates.GroupBy(t => t.OutputPath).Where(g => g.Count() > 1).ToList();
            var uniqueTemplates = templates.Where(t => !groupTemplates.Any(gt => gt.Key == t.OutputPath)).ToList();

            var uniqueTemplatesGenerationTasks = uniqueTemplates.Select(async (template) =>
            {
                await generateTemplateTask(template, model);
            });

            var groupTemplatesGenerationTasks = groupTemplates.Select(async (templates) =>
            {
                foreach (var template in templates)
                {
                    await generateTemplateTask(template, model);
                }
            });

            var generationTasks = uniqueTemplatesGenerationTasks.Concat(groupTemplatesGenerationTasks).ToList();
            await Task.WhenAll(generationTasks);
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

        public async Task RunPrettierAsync(string path)
        {
            var cts = new CancellationTokenSource();

            var process = new Process();
            process.StartInfo.WorkingDirectory = _prettierAngularProjectPath;
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/C npx prettier --write {path} --plugin=prettier-plugin-organize-imports --config \"{Path.Combine(_prettierAngularProjectPath, ".prettierrc")}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            _consoleWriter.AddMessageLine($"Prettier {path}...", "gray");

            try
            {
                cts.CancelAfter(30000);
                await process.WaitForExitAsync(cts.Token);
                _consoleWriter.AddMessageLine($"Prettier succeed !", "lightgreen");
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"prettier failed ({ex.Message})", "red");
                process.Kill();
            }
            finally
            {
                process.Dispose();
            }
        }

        private async Task GenerateFromTemplateAsync(Manifest.Feature.Template template, string templatePath, object model, string outputPath)
        {
            var relativeOutputPath = outputPath.Replace(_currentProject.Folder, string.Empty);
            var relativeTemplatePath = templatePath.Replace(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), string.Empty);
            var logMessagePrefix = $"Generation of {(template.IsPartial ? $"partial content '{template.PartialInsertionMarkup}' into" : "file")} '{relativeOutputPath}'" ;
#if DEBUG
            logMessagePrefix += $" from template file '{relativeTemplatePath}'";
#endif

            try
            {
                var generationTemplatePath = Path.GetTempFileName();
                var templateContent = await File.ReadAllTextAsync(templatePath);
                await File.WriteAllTextAsync(generationTemplatePath, templateContent);

                // Init template generator
                var templateGenerator = new VersionedTemplateGenerator(_modelProviderVersion);
                // Add reference to assembly of Manifest class to the template generator
                templateGenerator.Refs.Add(typeof(Manifest).Assembly.Location);
                // Inject Model parameter for template generation
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
#if DEBUG
                    _consoleWriter.AddMessageLine($"{logMessagePrefix} : [IGNORED]", "orange");
#endif
                    _currentContext.GenerationReport.TemplatesIgnored.Add(template);
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

                _consoleWriter.AddMessageLine($"{logMessagePrefix} : [SUCCESS]", "lightgreen");
                _currentContext.GenerationReport.TemplatesGenerated.Add(template);
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"{logMessagePrefix} : [ERROR] -> {ex}", color: "red");
                _currentContext.GenerationReport.TemplatesFailed.Add(template);
            }
        }

        private async Task WritePartialContentAsync(Manifest.Feature.Template template, string outputPath, string relativeOutputPath, List<string> generatedTemplateContent)
        {
            var insertionMarkup = GetInsertionMarkup(template, _currentContext);

            var outputContent = (await File.ReadAllLinesAsync(outputPath)).ToList();
            var insertionMarkupBegin = AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupBeginPattern, insertionMarkup), outputPath);
            var insertionMarkupEnd = AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupEndPattern, insertionMarkup), outputPath);
            if (!outputContent.Any(line => line.Trim().Equals(insertionMarkupBegin)) || !outputContent.Any(line => line.Trim().Equals(insertionMarkupEnd)))
            {
                throw new Exception($"Unable to find insertion markup {insertionMarkup} into {relativeOutputPath}");
            }

            (var partialInsertionMarkupBegin, var partialInsertionMarkupEnd) = GetPartialInsertionMarkups(_currentContext, template, outputPath);
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
                var indexBegin = outputContent.FindIndex(line => line.Trim().Equals(insertionMarkupEnd));
                outputContent.InsertRange(indexBegin, generatedTemplateContent);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            await File.WriteAllLinesAsync(outputPath, outputContent);
        }

        public static (string partialInsertionMarkupBegin, string partialInsertionMarkupEnd) GetPartialInsertionMarkups(FileGeneratorContext context, Manifest.Feature.Template template, string outputPath)
        {
            var partialInsertionMarkup = GetInsertionMarkup(template, context);

            return (
                AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupPartialBeginPattern, partialInsertionMarkup, template.UseDomainPartialInsertionMarkup ? context.DomainName : context.EntityName), outputPath),
                AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupPartialEndPattern, partialInsertionMarkup, template.UseDomainPartialInsertionMarkup ? context.DomainName :  context.EntityName), outputPath)
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
