namespace BIA.ToolKit.Test.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Test.Templates.Assertions;
    using Xunit.Sdk;
    using static BIA.ToolKit.Application.Templates.Manifest;

    public abstract class GenerateTestFixture(IMessageSink diagnosticMessageSink) : IDisposable
    {
        private sealed class DiagnosticMessage(string message) : IDiagnosticMessage
        {
            public string Message => message;
            public string ToJson() => $"{{\"$type\":\"diagnostic\",\"Message\":{System.Text.Json.JsonSerializer.Serialize(message)}}}";
        }

        internal class ConsoleWriterTest(IMessageSink messageSink, Stopwatch stopwatch, string fixtureName) : IConsoleWriter
        {
            public void AddMessageLine(string message, string color = null, bool refreshimediate = true)
            {
                messageSink.OnMessage(new DiagnosticMessage($"[{fixtureName} {stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}]\t{message}"));
            }

            public void Clear() { }
            public void CopyToClipboard() { }
        }

        public FileGeneratorService FileGeneratorService { get; private set; }
        private string referenceProjectPath;
        private string testProjectPath;
        private Project referenceProject;
        private string prettierAngularProjectPath;
        private ConsoleWriterTest consoleWriter;
        private readonly Stopwatch stopwatch = new();
        public Feature CurrentTestFeature { get; private set; }
        public Project TestProject { get; private set; }
        public List<string> PartialMarkupIdentifiersToIgnore { get; private set; }

        protected void Init(string biaDemoArchiveName, Project biaDemoProject)
        {
            consoleWriter = new ConsoleWriterTest(diagnosticMessageSink, stopwatch, GetType().Name);
            stopwatch.Start();

            string biaDemoZipPath = $"..\\..\\..\\..\\BIADemoVersions\\{biaDemoArchiveName}.zip";
            string currentDir = Directory.GetCurrentDirectory();
            referenceProjectPath = NormalisePath(Path.Combine(currentDir, "..\\..\\..\\..\\BIADemoVersions\\", Path.GetFileNameWithoutExtension(biaDemoZipPath)));
            testProjectPath = Path.Combine(Path.GetTempPath(), "BIAToolKitTestTemplatesGenerated", biaDemoProject.FrameworkVersion);

            consoleWriter.AddMessageLine($"Reference project at {referenceProjectPath}");
            consoleWriter.AddMessageLine($"Generation path at {testProjectPath}");

            bool doUnzip = !Directory.Exists(referenceProjectPath);
            if (doUnzip)
            {
                consoleWriter.AddMessageLine($"Unzipping {Path.GetFileName(biaDemoZipPath)}...");
                if (Directory.Exists(referenceProjectPath))
                {
                    Directory.Delete(referenceProjectPath, true);
                }
                ZipFile.ExtractToDirectory(biaDemoZipPath, referenceProjectPath);
            }
            else
            {
                if (!Directory.Exists(referenceProjectPath))
                {
                    throw new DirectoryNotFoundException(referenceProjectPath);
                }
            }

            consoleWriter.AddMessageLine($"Creating target test directory for generation...");
            Directory.CreateDirectory(testProjectPath);

            referenceProject = biaDemoProject;
            referenceProject.Folder = referenceProjectPath;

            TestProject = new Project
            {
                Folder = testProjectPath,
                Name = referenceProject.Name,
                CompanyName = referenceProject.CompanyName,
                BIAFronts = referenceProject.BIAFronts,
                FrameworkVersion = referenceProject.FrameworkVersion
            };

            consoleWriter.AddMessageLine($"Init service...");
            FileGeneratorService = new FileGeneratorService(consoleWriter);
            FileGeneratorService.Init(TestProject, true).Wait();

            if (referenceProject.BIAFronts.Count != 0)
            {
                string referenceProjectAngularPath = Path.Combine(referenceProject.Folder, referenceProject.BIAFronts.First());
                CreateUnitTestPrettierToolProject(referenceProjectAngularPath);
                FileGeneratorService.SetPrettierAngularProjectPath(prettierAngularProjectPath);
            }

            consoleWriter.AddMessageLine($"Ready");
        }

        public void Dispose()
        {
            stopwatch.Stop();
            CleanupTemporaryPrettierToolProject(prettierAngularProjectPath);
            GC.SuppressFinalize(this);
        }

        public async Task RunTestGenerateDtoAllFilesEqualsAsync(FileGeneratorDtoContext dtoContext, List<string> partialMarkupIdentifiersToIgnore = null)
        {
            await RunTestGenerateAllFilesEqualsAsync(
                dtoContext,
                Feature.FeatureType.Dto,
                FileGeneratorService.GenerateDtoAsync,
                partialMarkupIdentifiersToIgnore);
        }

        public async Task RunTestGenerateOptionAllFilesEqualsAsync(FileGeneratorOptionContext optionContext, List<string> partialMarkupIdentifiersToIgnore = null)
        {
            await RunTestGenerateAllFilesEqualsAsync(
                optionContext,
                Feature.FeatureType.Option,
                FileGeneratorService.GenerateOptionAsync,
                partialMarkupIdentifiersToIgnore);
        }

        public async Task RunTestGenerateCrudAllFilesEqualsAsync(FileGeneratorCrudContext crudContext, List<string> partialMarkupIdentifiersToIgnore = null)
        {
            await RunTestGenerateAllFilesEqualsAsync(
                crudContext,
                Feature.FeatureType.Crud,
                FileGeneratorService.GenerateCRUDAsync,
                partialMarkupIdentifiersToIgnore,
                context =>
                {
                    if (context.GenerateFront && context.HasParent)
                    {
                        context.ComputeAngularParentLocation(referenceProjectPath);
                    }
                });
        }

        private void CreateUnitTestPrettierToolProject(string referenceProjectAngularPath)
        {
            prettierAngularProjectPath = Path.Combine(Path.GetTempPath(), $"BIAToolKit_Prettier_{Guid.NewGuid()}");

            try
            {
                Directory.CreateDirectory(prettierAngularProjectPath);

                string prettierrcSourcePath = Path.Combine(referenceProjectAngularPath, ".prettierrc");
                if (!File.Exists(prettierrcSourcePath))
                {
                    throw new Exception($"Unable to find .prettierrc into {prettierrcSourcePath}");
                }

                File.Copy(prettierrcSourcePath, Path.Combine(prettierAngularProjectPath, ".prettierrc"));

                string packageJson = @"{
  ""name"": ""biatoolkit-prettier-temp"",
  ""version"": ""1.0.0"",
  ""private"": true,
  ""dependencies"": {},
  ""devDependencies"": {
    ""prettier"": ""~3.8.1"",
    ""prettier-plugin-organize-imports"": ""~4.3.0"",
    ""typescript"": ""~5.9.3""
  }
}";
                File.WriteAllText(Path.Combine(prettierAngularProjectPath, "package.json"), packageJson);

                var npmInstallProcess = new Process();
                npmInstallProcess.StartInfo.WorkingDirectory = prettierAngularProjectPath;
                npmInstallProcess.StartInfo.FileName = "cmd.exe";
                npmInstallProcess.StartInfo.Arguments = "/C npm install";
                npmInstallProcess.StartInfo.UseShellExecute = false;
                npmInstallProcess.StartInfo.CreateNoWindow = false;

                consoleWriter.AddMessageLine($"Installing prettier packages in temporary project...");
                npmInstallProcess.Start();
                npmInstallProcess.WaitForExit(TimeSpan.FromSeconds(30));

                if (npmInstallProcess.ExitCode != 0)
                {
                    throw new Exception($"npm install failed");
                }

                consoleWriter.AddMessageLine($"Prettier tool project ready at {prettierAngularProjectPath}");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Failed to create temporary prettier project: {ex.Message}");
                CleanupTemporaryPrettierToolProject(prettierAngularProjectPath);
                throw;
            }
        }

        private void CleanupTemporaryPrettierToolProject(string prettierAngularProjectPath)
        {
            if (string.IsNullOrEmpty(prettierAngularProjectPath) || !Directory.Exists(prettierAngularProjectPath))
            {
                return;
            }

            try
            {
                Directory.Delete(prettierAngularProjectPath, true);
                consoleWriter.AddMessageLine($"Cleaned up temporary prettier project");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Failed to cleanup temporary prettier project: {ex.Message}");
            }
        }

        private async Task RunTestGenerateAllFilesEqualsAsync<TContext>(
            TContext context,
            Feature.FeatureType featureType,
            Func<TContext, CancellationToken, Task> generateAsync,
            List<string> partialMarkupIdentifiersToIgnore = null,
            Action<TContext> preProcess = null)
            where TContext : FileGeneratorContext
        {
            PartialMarkupIdentifiersToIgnore = partialMarkupIdentifiersToIgnore;
            CurrentTestFeature = FileGeneratorService.GetCurrentManifestFeature(featureType);

            InitTestProjectFolder(featureType, context);

            preProcess?.Invoke(context);
            ImportTargetedPartialFiles(context);

            await generateAsync(context, CancellationToken.None);
            GenerationAssertions.AssertAllFilesEquals(this, context);
        }

        private void InitTestProjectFolder(Feature.FeatureType featureType, FileGeneratorContext context)
        {
            string generationFolder = Path.Combine(testProjectPath, $"{featureType}_{context.EntityName}");
            if (Directory.Exists(generationFolder))
            {
                Directory.Delete(generationFolder, true);
            }
            Directory.CreateDirectory(generationFolder);
            TestProject.Folder = generationFolder;
        }

        private void ImportTargetedPartialFiles(FileGeneratorContext context)
        {
            if (context.GenerateBack)
            {
                foreach (Feature.Template template in CurrentTestFeature.DotNetTemplates.Where(t => t.IsPartial))
                {
                    (string referencePath, string generatedPath) = GetDotNetFilesPath(template.OutputPath, context);
                    if (!File.Exists(referencePath))
                        continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(generatedPath)!);
                    File.Copy(referencePath, generatedPath, true);
                }
            }

            if (context.GenerateFront)
            {
                foreach (Feature.Template template in CurrentTestFeature.AngularTemplates.Where(t => t.IsPartial))
                {
                    (string referencePath, string generatedPath) = GetAngularFilesPath(template.OutputPath, context);
                    if (!File.Exists(referencePath))
                        continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(generatedPath)!);
                    File.Copy(referencePath, generatedPath, true);
                }
            }
        }

        internal (string referencePath, string generatedPath) GetDotNetFilesPath(string templateOutputPath, FileGeneratorContext context)
        {
            return (FileGeneratorService.GetDotNetTemplateOutputPath(templateOutputPath, context, referenceProject.Folder), FileGeneratorService.GetDotNetTemplateOutputPath(templateOutputPath, context, TestProject.Folder));
        }

        internal (string referencePath, string generatedPath) GetAngularFilesPath(string templateOutputPath, FileGeneratorContext context)
        {
            return (FileGeneratorService.GetAngularTemplateOutputPath(templateOutputPath, context, referenceProject.Folder), FileGeneratorService.GetAngularTemplateOutputPath(templateOutputPath, context, TestProject.Folder));
        }

        private static string NormalisePath(string path)
        {
            string[] components = path.Split(['\\']);

            var retval = new Stack<string>();
            foreach (string bit in components)
            {
                if (bit == "..")
                {
                    if (retval.Count > 0)
                    {
                        string popped = retval.Pop();
                        if (popped == "..")
                        {
                            retval.Push(popped);
                            retval.Push(bit);
                        }
                    }
                    else
                    {
                        retval.Push(bit);
                    }
                }
                else
                {
                    retval.Push(bit);
                }
            }

            var final = retval.ToList();
            final.Reverse();
            return string.Join("\\", final.ToArray());
        }
    }
}
