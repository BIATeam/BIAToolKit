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
    using BIA.ToolKit.Domain.ModifyProject;
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

        private readonly FileGeneratorService fileGeneratorService;
        private readonly string referenceProjectPath;
        private readonly string testProjectPath;
        private readonly Project referenceProject;
        private readonly Stopwatch stopwatch = new();
        private Feature? currentTestFeature;
        public Project TestProject { get; private set; }

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

            consoleWriter.AddMessageLine($"Unzipping {Path.GetFileName(biaDemoZipPath)}...");
            if (Directory.Exists(referenceProjectPath))
            {
                Directory.Delete(referenceProjectPath, true);
            }
            ZipFile.ExtractToDirectory(biaDemoZipPath, referenceProjectPath);

            consoleWriter.AddMessageLine($"Creating target test directory for generation...");
            if (Directory.Exists(testProjectPath))
            {
                Directory.Delete(testProjectPath, true);
            }
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
            fileGeneratorService = new FileGeneratorService(consoleWriter);
            fileGeneratorService.Init(TestProject);
            consoleWriter.AddMessageLine($"Ready");
        }

        public void Dispose()
        {
            stopwatch.Stop();
        }

        public async Task TestGenerateDtoAsync(FileGeneratorDtoContext dtoContext)
        {
            currentTestFeature = fileGeneratorService.GetCurrentManifestFeature("DTO");
            ImportTargetedPartialFiles(dtoContext);
            await fileGeneratorService.GenerateDtoAsync(dtoContext);
            AssertFilesEquals(dtoContext);
        }

        public async Task TestGenerateOptionAsync(FileGeneratorOptionContext optionContext)
        {
            currentTestFeature = fileGeneratorService.GetCurrentManifestFeature("Option");
            ImportTargetedPartialFiles(optionContext);
            await fileGeneratorService.GenerateOptionAsync(optionContext);
            AssertFilesEquals(optionContext);
        }

        private void AssertFilesEquals(FileGeneratorContext context)
        {
            var error = new List<string>();

            if (context.GenerateBack)
            {
                foreach (var dotNetTemplate in currentTestFeature!.DotNetTemplates)
                {
                    var (referencePath, generatedPath) = GetDotNetFilesPath(dotNetTemplate.OutputPath, context);
                    if (!File.Exists(generatedPath))
                    {
                        error.Add($"Missing file: {generatedPath}");
                        continue;
                    }

                    string? errorEquals = FileCompare.FilesEquals(referencePath, generatedPath, context, dotNetTemplate);

                    if (errorEquals != null)
                    {
                        error.Add(errorEquals);
                    }
                }
            }

            if (context.GenerateFront)
            {
                foreach (var angularTemplate in currentTestFeature!.AngularTemplates)
                {
                    var (referencePath, generatedPath) = GetAngularFilesPath(angularTemplate.OutputPath, context);
                    if (!File.Exists(generatedPath))
                    {
                        error.Add($"Missing file: {generatedPath}");
                        continue;
                    }

                    string? errorEquals = FileCompare.FilesEquals(referencePath, generatedPath, context, angularTemplate);

                    if (errorEquals != null)
                    {
                        error.Add(errorEquals);
                    }
                }
            }

            if (error.Count != 0)
            {
                throw new FilesEqualsException(string.Join("\n", error));
            }
        }

        private void ImportTargetedPartialFiles(FileGeneratorContext context)
        {
            if (context.GenerateBack)
            {
                foreach (var template in currentTestFeature!.DotNetTemplates.Where(t => t.IsPartial))
                {
                    var (referencePath, generatedPath) = GetDotNetFilesPath(template.OutputPath, context);
                    Directory.CreateDirectory(Path.GetDirectoryName(generatedPath)!);
                    File.Copy(referencePath, generatedPath, true);
                }
            }

            if (context.GenerateFront)
            {
                foreach (var template in currentTestFeature!.AngularTemplates.Where(t => t.IsPartial))
                {
                    var (referencePath, generatedPath) = GetAngularFilesPath(template.OutputPath, context);
                    Directory.CreateDirectory(Path.GetDirectoryName(generatedPath)!);
                    File.Copy(referencePath, generatedPath, true);
                }
            }
        }

        private (string referencePath, string generatedPath) GetDotNetFilesPath(string templateOutputPath, FileGeneratorContext context)
        {
            return (FileGeneratorService.GetDotNetTemplateOutputPath(templateOutputPath, context, referenceProject.Folder), FileGeneratorService.GetDotNetTemplateOutputPath(templateOutputPath, context, TestProject.Folder));
        }

        private (string referencePath, string generatedPath) GetAngularFilesPath(string templateOutputPath, FileGeneratorContext context)
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
