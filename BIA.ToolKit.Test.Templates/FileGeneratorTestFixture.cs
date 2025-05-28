namespace BIA.ToolKit.Test.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Compression;
    using System.Linq;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Templates;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Test.Templates.Assertions;
    using Microsoft.Build.Tasks;
    using static BIA.ToolKit.Application.Templates.Manifest;

    public class FileGeneratorTestFixture : IDisposable
    {
        internal class ConsoleWriterTest(Stopwatch stopwatch) : IConsoleWriter
        {
            public void AddMessageLine(string message, string? color = null, bool refreshimediate = true)
            {
                Console.WriteLine($"[{nameof(FileGeneratorTestFixture)} {stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}]\t{message}");
            }
        }

        public FileGeneratorService FileGeneratorService { get; private set; }
        private readonly string referenceProjectPath;
        private readonly string testProjectPath;
        private readonly Project referenceProject;
        private readonly Stopwatch stopwatch = new();
        public Feature CurrentTestFeature { get; private set; }
        public Project TestProject { get; private set; }
        public List<string> PartialMarkupIdentifiersToIgnore { get; private set; }

        public FileGeneratorTestFixture()
        {
            var consoleWriter = new ConsoleWriterTest(stopwatch);
            stopwatch.Start();

            var biaDemoZipPath = "..\\..\\..\\..\\BIADemoVersions\\BIADemo_5.0.0-alpha.zip";
            var currentDir = Directory.GetCurrentDirectory();
            referenceProjectPath = NormalisePath(Path.Combine(currentDir, "..\\..\\..\\..\\BIADemoVersions\\", Path.GetFileNameWithoutExtension(biaDemoZipPath)));
            testProjectPath = Path.Combine(Path.GetTempPath(), "BIAToolKitTestTemplatesGenerated");

            consoleWriter.AddMessageLine($"Reference project at {referenceProjectPath}");
            consoleWriter.AddMessageLine($"Generation path at {testProjectPath}");

            var doUnzip = !Directory.Exists(referenceProjectPath);
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

            referenceProject = new Project
            {
                Folder = referenceProjectPath,
                Name = "BIADemo",
                CompanyName = "TheBIADevCompany",
                BIAFronts = ["Angular"],
                FrameworkVersion = "5.0.0"
            };

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
            FileGeneratorService.Init(TestProject);

            if (referenceProject.BIAFronts.Count != 0)
            {
                var referenceProjetAngularPath = Path.Combine(referenceProject.Folder, referenceProject.BIAFronts.First());
                FileGeneratorService.SetPrettierAngularProjectPath(referenceProjetAngularPath);

                if (doUnzip)
                {
                    consoleWriter.AddMessageLine("npm i reference project...");
                    var process = new Process();
                    process.StartInfo.WorkingDirectory = referenceProjetAngularPath;
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = $"/C npm i";
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = false;
                    process.Start();
                    process.WaitForExit();
                }
            }

            consoleWriter.AddMessageLine($"Ready");
        }

        public void Dispose()
        {
            stopwatch.Stop();
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

        private async Task RunTestGenerateAllFilesEqualsAsync<TContext>(
            TContext context,
            Feature.FeatureType featureType,
            Func<TContext, Task> generateAsync,
            List<string> partialMarkupIdentifiersToIgnore = null,
            Action<TContext> preProcess = null)
            where TContext : FileGeneratorContext
        {
            PartialMarkupIdentifiersToIgnore = partialMarkupIdentifiersToIgnore;
            CurrentTestFeature = FileGeneratorService.GetCurrentManifestFeature(featureType);

            InitTestProjectFolder(featureType, context);

            preProcess?.Invoke(context);
            ImportTargetedPartialFiles(context);

            await generateAsync(context);
            GenerationAssertions.AssertAllFilesEquals(this, context);
        }

        private void InitTestProjectFolder(Feature.FeatureType featureType, FileGeneratorContext context)
        {
            var generationFolder = Path.Combine(TestProject.Folder, $"{featureType}_{context.EntityName}");
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
                foreach (var template in CurrentTestFeature.DotNetTemplates.Where(t => t.IsPartial))
                {
                    var (referencePath, generatedPath) = GetDotNetFilesPath(template.OutputPath, context);
                    if (!File.Exists(referencePath))
                        continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(generatedPath)!);
                    File.Copy(referencePath, generatedPath, true);
                }
            }

            if (context.GenerateFront)
            {
                foreach (var template in CurrentTestFeature.AngularTemplates.Where(t => t.IsPartial))
                {
                    var (referencePath, generatedPath) = GetAngularFilesPath(template.OutputPath, context);
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
            var components = path.Split(new Char[] { '\\' });

            var retval = new Stack<string>();
            foreach (var bit in components)
            {
                if (bit == "..")
                {
                    if (retval.Any())
                    {
                        var popped = retval.Pop();
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
