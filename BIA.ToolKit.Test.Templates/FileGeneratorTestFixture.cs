namespace BIA.ToolKit.Test.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Domain.ModifyProject;
    using static BIA.ToolKit.Application.Templates.Manifest;

    public class FileGeneratorTestFixture : IDisposable
    {
        internal class ConsoleWriterTest : IConsoleWriter
        {
            public void AddMessageLine(string message, string? color = null, bool refreshimediate = true)
            {
                Debug.WriteLine(message);
            }
        }

        private readonly string referenceProjectPath;
        private readonly string testProjectPath;
        private readonly Project referenceProject;
        public Project TestProject { get; private set; }
        public FileGeneratorService FileGeneratorService { get; private set; }

        public FileGeneratorTestFixture()
        {
            var biaDemoZipPath = "..\\..\\..\\..\\BIADemoVersions\\BIADemo_5.0.0-alpha.zip";
            var currentDir = Directory.GetCurrentDirectory();
            referenceProjectPath = NormalisePath(Path.Combine(currentDir, "..\\..\\..\\..\\BIADemoVersions\\", Path.GetFileNameWithoutExtension(biaDemoZipPath)));

            if (!Directory.Exists(referenceProjectPath))
            {
                ZipFile.ExtractToDirectory(biaDemoZipPath, referenceProjectPath);
            }

            testProjectPath = Path.Combine(Path.GetTempPath(), "BIAToolKitTestTemplatesGenerated");
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

            var consoleWriter = new ConsoleWriterTest();
            FileGeneratorService = new FileGeneratorService(consoleWriter);
            FileGeneratorService.Init(TestProject);

            consoleWriter.AddMessageLine($"Reference project at {referenceProjectPath}");
            consoleWriter.AddMessageLine($"Generation path at {testProjectPath}");
        }

        public void Dispose()
        {
            /*if (Directory.Exists(testProjectPath))
            {
                Directory.Delete(testProjectPath, true);
            }
            if (Directory.Exists(referenceProjectPath))
            {
                Directory.Delete(referenceProjectPath, true);
            }*/
        }

        public (string referencePath, string generatedPath) GetDotNetFilesPath(string templateOutputPath, FileGeneratorContext context)
        {
            return (FileGeneratorService.GetDotNetTemplateOutputPath(templateOutputPath, context, referenceProject.Folder), FileGeneratorService.GetDotNetTemplateOutputPath(templateOutputPath, context, TestProject.Folder));
        }

        public (string referencePath, string generatedPath) GetAngularFilesPath(string templateOutputPath, FileGeneratorContext context)
        {
            return (FileGeneratorService.GetAngularTemplateOutputPath(templateOutputPath, context, referenceProject.Folder), FileGeneratorService.GetAngularTemplateOutputPath(templateOutputPath, context, TestProject.Folder));
        }



        public void AssertFilesEquals(FileGeneratorDtoContext dtoContext, Feature currentFeature)
        {
            var error = new List<string>();

            foreach (var dotNetTemplate in currentFeature.DotNetTemplates)
            {
                var (referencePath, generatedPath) = GetDotNetFilesPath(dotNetTemplate.OutputPath, dtoContext);
                if (!File.Exists(generatedPath))
                {
                    error.Add($"Missing file: {generatedPath}");
                }

                string? errorEquals = FileCompare.FilesEquals(referencePath, generatedPath);

                if (errorEquals != null)
                {
                    error.Add(errorEquals);
                }
            }

            foreach (var angularTemplate in currentFeature.AngularTemplates)
            {
                var (referencePath, generatedPath) = GetAngularFilesPath(angularTemplate.OutputPath, dtoContext);
                if (!File.Exists(generatedPath))
                {
                    error.Add($"Missing file: {generatedPath}");
                }

                string? errorEquals = FileCompare.FilesEquals(referencePath, generatedPath);

                if (errorEquals != null)
                {
                    error.Add(errorEquals);
                }
            }

            if (error.Count != 0)
            {
                throw new FilesEqualsException(string.Join("\n\n", error));
            }
        }

        public static string NormalisePath(string path)
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
