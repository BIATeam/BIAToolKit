namespace BIA.ToolKit.Test.Templates.Assertions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Templates;
    using BIA.ToolKit.Test.Templates.Helpers;

    internal static class GenerationAssertions
    {
        public static void AssertAllFilesEquals(GenerateTestFixture testFixture, FileGeneratorContext context)
        {
            if(context.GenerationReport.HasFailed)
            {
                throw new GenerationFailureException();
            }

            var assertionExceptions = new List<GenerationAssertionException>();

            if (context.GenerateBack)
            {
                foreach (var dotNetTemplate in testFixture.CurrentTestFeature.DotNetTemplates)
                {
                    try
                    {
                        var (referencePath, generatedPath) = testFixture.GetDotNetFilesPath(dotNetTemplate.OutputPath, context);
                        VerifyFilesEquals(testFixture, referencePath, generatedPath, context, dotNetTemplate);
                    }
                    catch (GenerationAssertionException ex)
                    {
                        assertionExceptions.Add(ex);
                        continue;
                    }
                }
            }

            if (context.GenerateFront)
            {
                foreach (var angularTemplate in testFixture.CurrentTestFeature.AngularTemplates)
                {
                    try
                    {
                        var (referencePath, generatedPath) = testFixture.GetAngularFilesPath(angularTemplate.OutputPath, context);
                        VerifyFilesEquals(testFixture, referencePath, generatedPath, context, angularTemplate, true);
                    }
                    catch (GenerationAssertionException ex)
                    {
                        assertionExceptions.Add(ex);
                        continue;
                    }
                }
            }

            if (assertionExceptions.Count != 0)
            {
                throw new AllFilesNotEqualsException(string.Join("\n\n", assertionExceptions.Select(x => $"[{x.GetType().Name}]\n{x.Message}")));
            }
        }

        private static void VerifyFilesEquals(GenerateTestFixture testFixture, string referencePath, string generatedPath, FileGeneratorContext context, Manifest.Feature.Template template, bool isAngularTemplate = false)
        {
            if (context.GenerationReport.TemplatesIgnored.Any(t => t.Equals(template)))
                return;

            if (!File.Exists(referencePath))
                throw new ReferenceFileNotFoundException(referencePath);
            if (!File.Exists(generatedPath))
                throw new GeneratedFileNotFoundException(generatedPath);

            if (template.IsPartial)
            {
                ExtractPartialContent(ref referencePath, ref generatedPath, context, template, testFixture.PartialMarkupIdentifiersToIgnore);
            }

            RemoveGenerationIgnoreCode(testFixture, ref referencePath, isAngularTemplate);
            var result = DiffPlexFileComparer.CompareFiles(referencePath, generatedPath);
            if (!result.AreEqual)
            {
                throw new FilesNotEqualsException(
                    referencePath,
                    generatedPath,
                    result.Modified,
                    result.Moved,
                    result.Added,
                    result.Deleted);
            }
        }

        private static void RemoveGenerationIgnoreCode(GenerateTestFixture testFixture, ref string referencePath, bool isAngularTemplate)
        {
            const string biaDemoMarkupBegin = "Begin BIAToolKit Generation Ignore";
            const string biaDemoMarkupEnd = "End BIAToolKit Generation Ignore";

            var referenceLines = File.ReadAllLines(referencePath).ToList();
            if (RemoveLinesBetweenMarkups(biaDemoMarkupBegin, biaDemoMarkupEnd, referenceLines))
            {
                referencePath = referencePath.Replace(Path.GetFileName(referencePath), $"{Path.GetFileNameWithoutExtension(referencePath)}_Cleaned{Path.GetExtension(referencePath)}");
                if (File.Exists(referencePath))
                {
                    File.Delete(referencePath);
                }
                File.AppendAllLines(referencePath, referenceLines);

                // Prettier front files (excluded HTML)
                if (isAngularTemplate && !referencePath.EndsWith("html"))
                {
                    testFixture.FileGeneratorService.RunPrettierAsync(referencePath).Wait();
                }
            }
        }

        private static bool RemoveLinesBetweenMarkups(string markupBegin, string markupEnd, List<string> referenceLines)
        {
            var expectedBiaDemoMarkupBeginIndex = referenceLines.FindIndex(l => l.Contains(markupBegin));
            var expectedBiaDemoMarkupEndIndex = referenceLines.FindIndex(l => l.Contains(markupEnd));
            var areLinesRemoved = false;
            while (expectedBiaDemoMarkupBeginIndex > -1 && expectedBiaDemoMarkupEndIndex > -1 && expectedBiaDemoMarkupBeginIndex < expectedBiaDemoMarkupEndIndex)
            {
                areLinesRemoved = true;
                if (referenceLines[expectedBiaDemoMarkupBeginIndex - 1].Trim() == string.Empty)
                {
                    expectedBiaDemoMarkupBeginIndex--;
                }

                referenceLines.RemoveRange(expectedBiaDemoMarkupBeginIndex, expectedBiaDemoMarkupEndIndex - expectedBiaDemoMarkupBeginIndex + 1);
                expectedBiaDemoMarkupBeginIndex = referenceLines.FindIndex(l => l.Contains(markupBegin));
                expectedBiaDemoMarkupEndIndex = referenceLines.FindIndex(l => l.Contains(markupEnd));
            }

            return areLinesRemoved;
        }

        private static void ExtractPartialContent(ref string referencePath, ref string generatedPath, FileGeneratorContext context, Manifest.Feature.Template template, List<string> partialMarkupIdentifiersToIgnore)
        {
            (var partialInsertionMarkupBegin, var partialInsertionMarkupEnd) = FileGeneratorService.GetPartialInsertionMarkups(context, template, template.OutputPath);
            var referenceLines = File.ReadAllLines(referencePath).ToList();
            var generatedLines = File.ReadAllLines(generatedPath).ToList();

            var referenceMarkupBeginIndex = referenceLines.FindIndex(l => l.Contains(partialInsertionMarkupBegin));
            if (referenceMarkupBeginIndex < 0)
            {
                throw new PartialInsertionMarkupNotFoundException(partialInsertionMarkupBegin, referencePath);
            }
            var referenceMarkupEndIndex = referenceLines.FindIndex(l => l.Contains(partialInsertionMarkupEnd));
            if (referenceMarkupEndIndex < 0)
            {
                throw new PartialInsertionMarkupNotFoundException(partialInsertionMarkupEnd, referencePath);
            }

            var generatedMarkupBeginIndex = generatedLines.FindIndex(l => l.Contains(partialInsertionMarkupBegin));
            if (generatedMarkupBeginIndex < 0)
            {
                throw new PartialInsertionMarkupNotFoundException(partialInsertionMarkupBegin, generatedPath);
            }
            var generatedMarkupEndIndex = generatedLines.FindIndex(l => l.Contains(partialInsertionMarkupEnd));
            if (generatedMarkupEndIndex < 0)
            {
                throw new PartialInsertionMarkupNotFoundException(partialInsertionMarkupEnd, generatedPath);
            }

            referenceLines = referenceLines.GetRange(referenceMarkupBeginIndex, referenceMarkupEndIndex - referenceMarkupBeginIndex + 1);
            generatedLines = generatedLines.GetRange(generatedMarkupBeginIndex, generatedMarkupEndIndex - generatedMarkupBeginIndex + 1);

            if(partialMarkupIdentifiersToIgnore is not null)
            {
                foreach(var markup in partialMarkupIdentifiersToIgnore)
                {
                    RemoveContentBetweenIgnoredMarkups(referencePath, referenceLines, markup);
                    RemoveContentBetweenIgnoredMarkups(generatedPath, generatedLines, markup);
                }
            }

            referencePath = referencePath
                .Replace(Path.GetFileNameWithoutExtension(referencePath), $"{Path.GetFileNameWithoutExtension(referencePath)}_Partial_{template.PartialInsertionMarkup}_{context.EntityName}")
                .Replace("{Parent}", context.ParentName)
                .Replace("{Domain}", context.DomainName);
            generatedPath = generatedPath
                .Replace(Path.GetFileNameWithoutExtension(generatedPath), $"{Path.GetFileNameWithoutExtension(generatedPath)}_Partial_{template.PartialInsertionMarkup}_{context.EntityName}")
                .Replace("{Parent}", context.ParentName)
                .Replace("{Domain}", context.DomainName);

            if (File.Exists(referencePath))
            {
                File.Delete(referencePath);
            }
            if (File.Exists(generatedPath))
            {
                File.Delete(generatedPath);
            }
            File.AppendAllLines(referencePath, referenceLines);
            File.AppendAllLines(generatedPath, generatedLines);
        }

        private static void RemoveContentBetweenIgnoredMarkups(string filePath, List<string> lines, string markup)
        {
            var markupSafe = Regex.Escape(markup);
            var markupBeginPattern = $@"^\/\/ BIAToolKit - Begin Partial\b.*?\b{markupSafe}$";
            var markupEndPattern = $@"^\/\/ BIAToolKit - End Partial\b.*?\b{markupSafe}$";

            var beginMarkups = lines.Where(l => Regex.IsMatch(l.Trim(), markupBeginPattern)).Distinct().ToList();
            var endMarkups = lines.Where(l => Regex.IsMatch(l.Trim(), markupEndPattern)).Distinct().ToList();
            if (beginMarkups.Count != endMarkups.Count)
            {
                throw new PartialInsertionMarkupBeginAndMarkupCountNotEqualException(markupSafe, filePath);
            }

            foreach (var beginMarkup in beginMarkups)
            {
                var endMarkupTarget = beginMarkup.Replace("Begin", "End");
                var endMarkup = endMarkups.FirstOrDefault(x => x == endMarkupTarget);
                if (string.IsNullOrWhiteSpace(endMarkup))
                {
                    throw new PartialInsertionMarkupNotFoundException(endMarkupTarget, filePath);
                }
                RemoveLinesBetweenMarkups(beginMarkup, endMarkup, lines);
            }
        }
    }
}
