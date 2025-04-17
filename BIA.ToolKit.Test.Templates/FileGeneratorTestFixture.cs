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
    using BIA.ToolKit.Application.Services.FileGenerator.Context;
    using BIA.ToolKit.Domain.ModifyProject;

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
        private readonly Project testProject;
        public FileGeneratorService FileGeneratorService { get; private set; }

        public FileGeneratorTestFixture()
        {
            var biaDemoZipPath = "C:\\sources\\Github\\BIAToolKit\\BIADemoVersions\\BIADemo_5.0.0-alpha.zip";
            referenceProjectPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(biaDemoZipPath));

            if (Directory.Exists(referenceProjectPath))
            {
                Directory.Delete(referenceProjectPath, true);
            }
            ZipFile.ExtractToDirectory(biaDemoZipPath, referenceProjectPath);

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

            testProject = new Project
            {
                Folder = testProjectPath,
                Name = referenceProject.Name,
                CompanyName = referenceProject.CompanyName,
                BIAFronts = referenceProject.BIAFronts,
                FrameworkVersion = referenceProject.FrameworkVersion
            };

            var consoleWriter = new ConsoleWriterTest();
            FileGeneratorService = new FileGeneratorService(consoleWriter);
            FileGeneratorService.Init(testProject);

            consoleWriter.AddMessageLine($"Reference project at {referenceProjectPath}");
            consoleWriter.AddMessageLine($"Generation path at {testProjectPath}");
        }

        public void Dispose()
        {
            if (Directory.Exists(testProjectPath))
            {
                Directory.Delete(testProjectPath, true);
            }
            if (Directory.Exists(referenceProjectPath))
            {
                Directory.Delete(referenceProjectPath, true);
            }
        }

        public (string referencePath, string generatedPath) GetDotNetFilesPath(string templateOutputPath, FileGeneratorContext context)
        {
            return (FileGeneratorService.GetDotNetTemplateOutputPath(templateOutputPath, context, referenceProject.Folder), FileGeneratorService.GetDotNetTemplateOutputPath(templateOutputPath, context, testProject.Folder));
        }

        public (string referencePath, string generatedPath) GetAngularFilesPath(string templateOutputPath, FileGeneratorContext context)
        {
            return (FileGeneratorService.GetAngularTemplateOutputPath(templateOutputPath, context, referenceProject.Folder), FileGeneratorService.GetAngularTemplateOutputPath(templateOutputPath, context, testProject.Folder));
        }
    }
}
