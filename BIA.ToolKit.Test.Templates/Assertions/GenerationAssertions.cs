namespace BIA.ToolKit.Test.Templates.Assertions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Templates;
    using BIA.ToolKit.Test.Templates.Helpers;

    internal static class GenerationAssertions
    {
        public static void AssertAllFilesEquals(FileGeneratorTestFixture testFixture, FileGeneratorContext context, Manifest.Feature feature)
        {
            var error = new List<string>();

            if (context.GenerateBack)
            {
                foreach (var dotNetTemplate in feature!.DotNetTemplates)
                {
                    var (referencePath, generatedPath) = testFixture.GetDotNetFilesPath(dotNetTemplate.OutputPath, context);
                    if (!File.Exists(generatedPath))
                    {
                        error.Add($"Missing file: {generatedPath}");
                        continue;
                    }

                    string? errorEquals = FileCompareHelper.FilesEquals(referencePath, generatedPath, context, dotNetTemplate);

                    if (errorEquals != null)
                    {
                        error.Add(errorEquals);
                    }
                }
            }

            if (context.GenerateFront)
            {
                foreach (var angularTemplate in feature!.AngularTemplates)
                {
                    var (referencePath, generatedPath) = testFixture.GetAngularFilesPath(angularTemplate.OutputPath, context);
                    if (!File.Exists(generatedPath))
                    {
                        error.Add($"Missing file: {generatedPath}");
                        continue;
                    }

                    string? errorEquals = FileCompareHelper.FilesEquals(referencePath, generatedPath, context, angularTemplate);

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
    }
}
