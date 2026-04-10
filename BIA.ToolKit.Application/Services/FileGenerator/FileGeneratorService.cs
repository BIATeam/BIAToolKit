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
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualBasic.FileIO;
    using Mono.TextTemplating;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Diagnostics;
    using System.Threading;
    using static BIA.ToolKit.Application.Templates.Manifest.Feature;
    using Microsoft.VisualStudio.TextTemplating;

    public partial class FileGeneratorService
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
        private string _prettierAngularProjectPathOverride;
        private bool _fromUnitTest;

        public bool IsInit { get; private set; }

        public FileGeneratorService(IConsoleWriter consoleWriter)
        {
            _consoleWriter = consoleWriter;
            _modelProviderFactory = new FileGeneratorModelProviderFactory(consoleWriter);

            LoadTemplatesManifests();
        }

        public async Task Init(Project project, bool fromUnitTest = false, CancellationToken cancellationToken = default)
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
            _prettierAngularProjectPathOverride = null;

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
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"File generator : {ex.Message}", "red");
            }
        }

        private void SetCurrentManifest(string modelProviderVersion)
        {
            string manifestVersion = modelProviderVersion.Replace("_", ".")[1..modelProviderVersion.Length];
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
            Regex regex = MyRegex();
            Match match = regex.Match(_modelProvider.GetType().Name);
            if (!match.Success)
            {
                throw new Exception($"invalid model provider version");
            }
            return match.Groups[1].Value;
        }

        private void LoadModelProvider(Version projectVersion)
        {
            _modelProvider = _modelProviderFactory.GetModelProvider(projectVersion) ?? throw new Exception($"incompatible project version {projectVersion}");
        }

        private static void ParseProjectVersion(Project project, out Version projectVersion)
        {
            if (!Version.TryParse(project.FrameworkVersion, out projectVersion))
            {
                throw new Exception($"invalid project version");
            }
        }

        private static string FindInPath(string fileName)
        {
            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string dir in pathEnv.Split(Path.PathSeparator))
            {
                string fullPath = Path.Combine(dir, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
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
            if (!Directory.Exists(path))
            {
                throw new Exception($"Unable to init prettier from unexisting front folder {path}");
            }
            _prettierAngularProjectPath = path;
        }

        /// <summary>
        /// Sets a prettier path that takes precedence over the project folder path normally derived
        /// from <see cref="_currentProject"/>. Useful when generating features into a temporary
        /// folder that does not have <c>node_modules</c> installed.
        /// The override is cleared automatically when <see cref="Init"/> is called.
        /// </summary>
        public void SetPrettierProjectPathOverride(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new Exception($"Unable to set prettier path override from unexisting folder {path}");
            }
            _prettierAngularProjectPathOverride = path;
        }

        /// <summary>
        /// Returns all template output paths declared in the current manifest, together with a flag
        /// indicating whether each path belongs to a DotNet (<c>true</c>) or Angular (<c>false</c>)
        /// template. Returns an empty collection when the generator is not initialised.
        /// </summary>
        public IEnumerable<(string OutputPath, bool IsDotNet)> GetAllManifestOutputPaths()
        {
            if (!IsInit || _currentManifest == null)
                return [];

            var result = new List<(string, bool)>();
            foreach (Manifest.Feature feature in _currentManifest.Features)
            {
                result.AddRange(feature.DotNetTemplates.Select(t => (t.OutputPath, true)));
                result.AddRange(feature.AngularTemplates.Select(t => (t.OutputPath, false)));
            }

            return result;
        }

        public async Task GenerateDtoAsync(FileGeneratorDtoContext dtoContext, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                _consoleWriter.AddMessageLine($"=== GENERATE DTO ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                object templateModel = _modelProvider.GetDtoTemplateModel(dtoContext);
                Manifest.Feature dtoFeature = GetCurrentManifestFeature(Manifest.Feature.FeatureType.Dto);

                _currentContext = dtoContext;

                await GenerateTemplatesFromManifestFeatureAsync(dtoFeature, templateModel);
                _consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"DTO generation failed : {ex}", color: "red");
            }
        }

        public async Task GenerateOptionAsync(FileGeneratorOptionContext optionContext, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                _consoleWriter.AddMessageLine($"=== GENERATE OPTION ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                object templateModel = _modelProvider.GetOptionTemplateModel(optionContext);
                Manifest.Feature optionFeature = GetCurrentManifestFeature(Manifest.Feature.FeatureType.Option);

                _currentContext = optionContext;

                await GenerateTemplatesFromManifestFeatureAsync(optionFeature, templateModel);

                _consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"Option generation failed : {ex}", color: "red");
            }
        }

        public async Task GenerateCRUDAsync(FileGeneratorCrudContext crudContext, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                _consoleWriter.AddMessageLine($"=== GENERATE CRUD ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                if (crudContext.GenerateFront && crudContext.HasParent)
                {
                    crudContext.ComputeAngularParentLocation(_currentProject.Folder);
                }

                object templateModel = _modelProvider.GetCrudTemplateModel(crudContext);
                Manifest.Feature crudFeature = GetCurrentManifestFeature(Manifest.Feature.FeatureType.Crud);

                _currentContext = crudContext;

                await GenerateTemplatesFromManifestFeatureAsync(crudFeature, templateModel);

                _consoleWriter.AddMessageLine($"=== END ===", color: "lightblue");
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"CRUD generation failed : {ex}", color: "red");
            }
        }

        public async Task GenerateTeamAsync(FileGeneratorTeamContext teamContext, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                _consoleWriter.AddMessageLine($"=== GENERATE TEAM ===", color: "lightblue");

                if (!IsInit)
                    throw new Exception("file generator has not been initialiazed");

                if (teamContext.GenerateFront && teamContext.HasParent)
                {
                    teamContext.ComputeAngularParentLocation(_currentProject.Folder);
                }

                object templateModel = _modelProvider.GetTeamTemplateModel(teamContext);
                Manifest.Feature teamFeature = GetCurrentManifestFeature(Manifest.Feature.FeatureType.Team);

                _currentContext = teamContext;

                await GenerateTemplatesFromManifestFeatureAsync(teamFeature, templateModel);

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

            Task generateAngularTemplatesTask = Task.CompletedTask;
            Task generateDotNetTemplatesTask = Task.CompletedTask;

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
            IEnumerable<string> csharpFiles = _currentContext.GenerationReport.TemplatesGenerated
                .Select(x => GetDotNetTemplateOutputPath(x.OutputPath, _currentContext, _currentProject.Folder))
                .Where(x => Path.GetExtension(x).Equals(".cs"));

            foreach (string file in csharpFiles)
            {
                if (FileTransform.OrderUsingFromFile(file))
                {
                    _consoleWriter.AddMessageLine($"-> Usings ordered in {file}", "green");
                }
            }
        }

        private async Task GenerateDotNetTemplateAsync(Manifest.Feature.Template template, object model)
        {
            string templatePath = Path.Combine(_templatesPath, Constants.FolderDotNet, template.InputPath);
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
            string angularProjectFolder = Path.Combine(_currentProject.Folder, _currentContext.AngularFront);

            if (!_fromUnitTest)
            {
                // Use externally supplied path if set (e.g. when generating into a temp folder that
                // does not have node_modules); otherwise fall back to the project's own front folder.
                SetPrettierAngularProjectPath(_prettierAngularProjectPathOverride ?? angularProjectFolder);
            }

            await RunGenerateTemplatesAsync(templates, model, GenerateAngularTemplateAsync);

            var filesExtensionToPrettier = new List<string> { ".html", ".ts" };
            var templatesWithFileToPrettier = _currentContext.GenerationReport.TemplatesGenerated.Where(x => filesExtensionToPrettier.Contains(Path.GetExtension(x.OutputPath))).ToList();
            var prettierTasks = templatesWithFileToPrettier.Select(async template =>
            {
                string filePath = GetAngularTemplateOutputPath(template.OutputPath, _currentContext, _currentProject.Folder);
                await RunPrettierAsync(filePath);
            }).ToList();
            await Task.WhenAll(prettierTasks);
        }

        private async Task GenerateAngularTemplateAsync(Manifest.Feature.Template template, object model)
        {
            string templatePath = Path.Combine(_templatesPath, Constants.FolderAngular, template.InputPath);
            string outputPath = GetAngularTemplateOutputPath(template.OutputPath, _currentContext, _currentProject.Folder);
            await GenerateFromTemplateAsync(template, templatePath, model, outputPath);
        }

        private static async Task RunGenerateTemplatesAsync(IEnumerable<Manifest.Feature.Template> templates, object model, Func<Manifest.Feature.Template, object, Task> generateTemplateTask)
        {
            var groupTemplates = templates.GroupBy(t => t.OutputPath).Where(g => g.Count() > 1).ToList();
            var uniqueTemplates = templates.Where(t => !groupTemplates.Any(gt => gt.Key == t.OutputPath)).ToList();

            IEnumerable<Task> uniqueTemplatesGenerationTasks = uniqueTemplates.Select(async (template) =>
            {
                await generateTemplateTask(template, model);
            });

            IEnumerable<Task> groupTemplatesGenerationTasks = groupTemplates.Select(async (templates) =>
            {
                foreach (Template template in templates)
                {
                    await generateTemplateTask(template, model);
                }
            });

            var generationTasks = uniqueTemplatesGenerationTasks.Concat(groupTemplatesGenerationTasks).ToList();
            await Task.WhenAll(generationTasks);
        }

        public static string GetAngularTemplateOutputPath(string templateOutputPath, FileGeneratorContext context, string projectFolder)
        {
            string outputPath = Path.Combine(
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

        private readonly SemaphoreSlim _prettierSemaphore = new(10);

        public async Task RunPrettierAsync(string path)
        {
            var cts = new CancellationTokenSource();

            var process = new Process();
            process.StartInfo.WorkingDirectory = _prettierAngularProjectPath;
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/C npx prettier --write \"{path}\" --plugin=prettier-plugin-organize-imports --config \"{Path.Combine(_prettierAngularProjectPath, ".prettierrc")}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            try
            {
                await _prettierSemaphore.WaitAsync();
                _consoleWriter.AddMessageLine($"Prettier {path}...", "gray");
                process.Start();
                cts.CancelAfter(30000);
                await process.WaitForExitAsync(cts.Token);
                _consoleWriter.AddMessageLine($"Prettier succeed for {path} !", "lightgreen");
            }
            catch (Exception ex)
            {
                _consoleWriter.AddMessageLine($"Prettier failed for {path} (PID={process.Id}) : {ex.Message}", "red");
                process.Kill();
            }
            finally
            {
                process.Dispose();
                _prettierSemaphore.Release();
            }
        }

        private async Task GenerateFromTemplateAsync(Manifest.Feature.Template template, string templatePath, object model, string outputPath)
        {
            string relativeOutputPath = outputPath.Replace(_currentProject.Folder, string.Empty);
            string relativeTemplatePath = templatePath.Replace(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), string.Empty);
            string logMessagePrefix = $"Generation of {(template.IsPartial ? $"partial content '{template.PartialInsertionMarkup}' into" : "file")} '{relativeOutputPath}'";
#if DEBUG
            logMessagePrefix += $" from template file '{relativeTemplatePath}'";
#endif

            try
            {
                string generationTemplatePath = Path.GetTempFileName();
                string templateContent = await File.ReadAllTextAsync(templatePath);
                await File.WriteAllTextAsync(generationTemplatePath, templateContent);

                // Ensure DOTNET_ROOT is set for Mono.TextTemplating (not set when running as WPF app)
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT")))
                {
                    string dotnetExe = FindInPath("dotnet.exe") ?? FindInPath("dotnet");
                    if (dotnetExe != null)
                    {
                        Environment.SetEnvironmentVariable("DOTNET_ROOT", Path.GetDirectoryName(dotnetExe));
                    }
                }

                // Init template generator
                var templateGenerator = new VersionedTemplateGenerator(_modelProviderVersion);
                // Add reference to assembly of Manifest class to the template generator
                templateGenerator.Refs.Add(typeof(Manifest).Assembly.Location);
                // Inject Model parameter for template generation
                ITextTemplatingSession templateGeneratorSession = templateGenerator.GetOrCreateSession();
                templateGeneratorSession.Add("Model", model);

                // Generate content from template into temp file
                string generatedTemplatePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(outputPath));
                bool success = await templateGenerator.ProcessTemplateAsync(generationTemplatePath, generatedTemplatePath);
                File.Delete(generationTemplatePath);
                if (!success)
                {
                    throw new Exception(JsonConvert.SerializeObject(templateGenerator.Errors));
                }

                // Check if generated content has any line
                List<string> generatedTemplateContent = [.. (await File.ReadAllLinesAsync(generatedTemplatePath))];
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
            string insertionMarkup = GetInsertionMarkup(template, _currentContext);

            List<string> outputContent = [.. (await File.ReadAllLinesAsync(outputPath))];
            string insertionMarkupBegin = AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupBeginPattern, insertionMarkup), outputPath);
            string insertionMarkupEnd = AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupEndPattern, insertionMarkup), outputPath);
            if (!outputContent.Any(line => line.Trim().Equals(insertionMarkupBegin)) || !outputContent.Any(line => line.Trim().Equals(insertionMarkupEnd)))
            {
                throw new Exception($"Unable to find insertion markup {insertionMarkup} into {relativeOutputPath}");
            }

            (string partialInsertionMarkupBegin, string partialInsertionMarkupEnd) = GetPartialInsertionMarkups(_currentContext, template, outputPath);
            // Partial content already exists
            if (outputContent.Any(line => line.Trim().Equals(partialInsertionMarkupBegin)) && outputContent.Any(line => line.Trim().Equals(partialInsertionMarkupEnd)))
            {
                // Retrieve content into ignored inner markups
                var ignoredInnerMarkupsContent = new Dictionary<(string insertionMarkup, string insertionMarkupBegin, string insertionMarkupEnd), List<string>>();
                var ignoredInnerMarkups = template.IgnoredInnerMarkups.Select(x =>
                {
                    string insertionMarkup = GetInsertionMarkup(new Template { PartialInsertionMarkup = x }, _currentContext);
                    string insertionMarkupBegin = AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupBeginPattern, insertionMarkup), outputPath);
                    string insertionMarkupEnd = AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupEndPattern, insertionMarkup), outputPath);
                    return (
                        insertionMarkup,
                        insertionMarkupBegin,
                        insertionMarkupEnd);
                }).ToList();

                foreach ((string insertionMarkup, string insertionMarkupBegin, string insertionMarkupEnd) ignoredInnerMarkup in ignoredInnerMarkups)
                {
                    int ignoredInnerMarkupBeginIndex = outputContent.FindIndex(line => line.Trim().Equals(ignoredInnerMarkup.insertionMarkupBegin));
                    int ignoredInnerMarkupEndIndex = outputContent.FindIndex(line => line.Trim().Equals(ignoredInnerMarkup.insertionMarkupEnd));
                    if (ignoredInnerMarkupBeginIndex > -1 && ignoredInnerMarkupEndIndex > -1)
                    {
                        ignoredInnerMarkupsContent.Add(ignoredInnerMarkup, outputContent.GetRange(ignoredInnerMarkupBeginIndex + 1, ignoredInnerMarkupEndIndex - ignoredInnerMarkupBeginIndex - 1));
                    }
                }

                // Find begin/end markups
                int indexBegin = outputContent.FindIndex(line => line.Trim().Equals(partialInsertionMarkupBegin));
                int indexEnd = outputContent.FindIndex(line => line.Trim().Equals(partialInsertionMarkupEnd));
                // Replace previous generated content by new one
                outputContent.RemoveRange(indexBegin, indexEnd - indexBegin + 1);
                outputContent.InsertRange(indexBegin, generatedTemplateContent);

                // Reinsert content of ignored inner markups
                foreach (KeyValuePair<(string insertionMarkup, string insertionMarkupBegin, string insertionMarkupEnd), List<string>> ignoredInnerMarkupContent in ignoredInnerMarkupsContent)
                {
                    int ignoredInnerMarkupBeginIndex = outputContent.FindIndex(line => line.Trim().Equals(ignoredInnerMarkupContent.Key.insertionMarkupBegin));
                    outputContent.InsertRange(ignoredInnerMarkupBeginIndex + 1, ignoredInnerMarkupContent.Value);
                }
            }
            else
            {
                int indexBegin = outputContent.FindIndex(line => line.Trim().Equals(insertionMarkupEnd));
                outputContent.InsertRange(indexBegin, generatedTemplateContent);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            await File.WriteAllLinesAsync(outputPath, outputContent);
        }

        public static (string partialInsertionMarkupBegin, string partialInsertionMarkupEnd) GetPartialInsertionMarkups(FileGeneratorContext context, Manifest.Feature.Template template, string outputPath)
        {
            string partialInsertionMarkup = GetInsertionMarkup(template, context);

            return (
                AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupPartialBeginPattern, partialInsertionMarkup, template.UseDomainPartialInsertionMarkup ? context.DomainName : context.EntityName), outputPath),
                AdaptBiaToolKitMarkup(string.Format(BiaToolKitMarkupPartialEndPattern, partialInsertionMarkup, template.UseDomainPartialInsertionMarkup ? context.DomainName : context.EntityName), outputPath)
                );
        }

        private static string GetInsertionMarkup(Manifest.Feature.Template template, FileGeneratorContext context)
        {
            return template.PartialInsertionMarkup
                .Replace("{Parent}", context.ParentName)
                .Replace("{Domain}", context.DomainName);
        }

        private static string AdaptBiaToolKitMarkup(string markup, string outputPath)
        {
            if (outputPath.EndsWith(".html"))
            {
                return markup.Replace("//", "<!--") + " -->";
            }

            return markup;
        }

        [GeneratedRegex(@"(_[0-9]+(?:_[0-9]+){0,2})[^0-9]*")]
        private static partial Regex MyRegex();
    }
}
