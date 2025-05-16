namespace BIA.ToolKit.Test.Templates.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Templates;

    internal static class FileCompareHelper
    {
        public static string? FilesEquals(string expectedFilePath, string actualFilePath, FileGeneratorContext context, Manifest.Feature.Template template)
        {
            var expectedLines = File.ReadAllLines(expectedFilePath).ToList();
            var actualLines = File.ReadAllLines(actualFilePath).ToList();

            if (template.IsPartial)
            {
                ExtractPartialContent(ref expectedFilePath, ref actualFilePath, context, template, ref expectedLines, ref actualLines);
            }

            RemoveBiaDemoCodeExample(ref expectedFilePath, expectedLines);
            return CompareFiles(expectedFilePath, actualFilePath, expectedLines, actualLines);
        }

        private static void RemoveBiaDemoCodeExample(ref string expectedFilePath, List<string> expectedLines)
        {
            const string biaDemoMarkupBegin = "Begin BIADemo";
            const string biaDemoMarkupEnd = "End BIADemo";

            var expectedBiaDemoMarkupBeginIndex = expectedLines.FindIndex(l => l.Contains(biaDemoMarkupBegin));
            var expectedBiaDemoMarkupEndIndex = expectedLines.FindIndex(l => l.Contains(biaDemoMarkupEnd));
            var areLinesRemoved = false;
            while (expectedBiaDemoMarkupBeginIndex > -1 && expectedBiaDemoMarkupEndIndex > -1 && expectedBiaDemoMarkupBeginIndex < expectedBiaDemoMarkupEndIndex)
            {
                areLinesRemoved = true;
                if (expectedLines[expectedBiaDemoMarkupBeginIndex - 1].Trim() == string.Empty)
                {
                    expectedBiaDemoMarkupBeginIndex--;
                }

                expectedLines.RemoveRange(expectedBiaDemoMarkupBeginIndex, expectedBiaDemoMarkupEndIndex - expectedBiaDemoMarkupBeginIndex + 1);
                expectedBiaDemoMarkupBeginIndex = expectedLines.FindIndex(l => l.Contains(biaDemoMarkupBegin));
                expectedBiaDemoMarkupEndIndex = expectedLines.FindIndex(l => l.Contains(biaDemoMarkupEnd));
            }

            if (areLinesRemoved)
            {
                expectedFilePath = expectedFilePath.Replace(Path.GetFileNameWithoutExtension(expectedFilePath), $"{Path.GetFileNameWithoutExtension(expectedFilePath)}_Cleaned");
                if (File.Exists(expectedFilePath))
                {
                    File.Delete(expectedFilePath);
                }
                File.AppendAllLines(expectedFilePath, expectedLines);
            }
        }

        private static void ExtractPartialContent(ref string expectedFilePath, ref string actualFilePath, FileGeneratorContext context, Manifest.Feature.Template template, ref List<string> expectedLines, ref List<string> actualLines)
        {
            (var partialInsertionMarkupBegin, var partialInsertionMarkupEnd) = FileGeneratorService.GetPartialInsertionMarkups(context, template, template.OutputPath);

            var expectedMarkupBeginIndex = expectedLines.FindIndex(l => l.Contains(partialInsertionMarkupBegin));
            if (expectedMarkupBeginIndex < 0)
            {
                throw new PartialInsertionMarkupNotFoundException(partialInsertionMarkupBegin, expectedFilePath);
            }
            var expectedMarkupEndIndex = expectedLines.FindIndex(l => l.Contains(partialInsertionMarkupEnd));
            if (expectedMarkupEndIndex < 0)
            {
                throw new PartialInsertionMarkupNotFoundException(partialInsertionMarkupEnd, expectedFilePath);
            }

            var actualMarkupBeginIndex = actualLines.FindIndex(l => l.Contains(partialInsertionMarkupBegin));
            if (actualMarkupBeginIndex < 0)
            {
                throw new PartialInsertionMarkupNotFoundException(partialInsertionMarkupBegin, actualFilePath);
            }
            var actualMarkupEndIndex = actualLines.FindIndex(l => l.Contains(partialInsertionMarkupEnd));
            if (actualMarkupEndIndex < 0)
            {
                throw new PartialInsertionMarkupNotFoundException(partialInsertionMarkupEnd, actualFilePath);
            }

            expectedLines = expectedLines.GetRange(expectedMarkupBeginIndex, expectedMarkupEndIndex - expectedMarkupBeginIndex + 1);
            actualLines = actualLines.GetRange(actualMarkupBeginIndex, actualMarkupEndIndex - actualMarkupBeginIndex + 1);

            expectedFilePath = expectedFilePath.Replace(Path.GetFileNameWithoutExtension(expectedFilePath), $"{Path.GetFileNameWithoutExtension(expectedFilePath)}_Partial_{template.PartialInsertionMarkup}{context.EntityName}");
            actualFilePath = actualFilePath.Replace(Path.GetFileNameWithoutExtension(actualFilePath), $"{Path.GetFileNameWithoutExtension(actualFilePath)}_Partial_{template.PartialInsertionMarkup}{context.EntityName}");
            if (File.Exists(expectedFilePath))
            {
                File.Delete(expectedFilePath);
            }
            if (File.Exists(actualFilePath))
            {
                File.Delete(actualFilePath);
            }
            File.AppendAllLines(expectedFilePath, expectedLines);
            File.AppendAllLines(actualFilePath, actualLines);
        }

        private static string? CompareFiles(string expectedFilePath, string actualFilePath, List<string> expectedLines, List<string> actualLines)
        {
            int modified = 0;
            int moved = 0;
            int added = 0;
            int deleted = 0;

            int expectedIndex = 0;
            int actualIndex = 0;

            List<int> actual_deplaced_line = new List<int>();
            List<int> expected_deplaced_line = new List<int>();


            /*var toBeUpdated =
            expectedLines.Where(
            a => actualLines.Any(
                b => (b.ObjectiveDetailId == a.ObjectiveDetailId) &&
                     (b.Number != a.Number || !b.Text.Equals(a.Text))));*/

            var toBeAdded =
                        expectedLines.Where(a => actualLines.All(
                        b => b != a));

            var toBeDeleted =
                        actualLines.Where(a => expectedLines.All(
                        b => b != a));

            while (expectedIndex < expectedLines.Count || actualIndex < actualLines.Count)
            {
                if (expectedIndex >= expectedLines.Count)
                {
                    added++;
                    actualIndex++;
                }
                else if (actualIndex >= actualLines.Count)
                {
                    deleted++;
                    expectedIndex++;
                }
                else
                {
                    var expectedLine = expectedLines[expectedIndex];
                    var actualLine = actualLines[actualIndex];

                    if (string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
                    {
                        expectedIndex++;
                        actualIndex++;
                    }
                    else
                    {
                        var isActualEmpty = string.IsNullOrEmpty(actualLine);
                        var isExpectedEmpty = string.IsNullOrEmpty(expectedLine);
                        if (isActualEmpty && isExpectedEmpty)
                        {
                            modified++;
                            expectedIndex++;
                            actualIndex++;
                        }
                        else if (isActualEmpty)
                        {
                            added++;
                            actualIndex++;
                        }
                        else if (isExpectedEmpty)
                        {
                            deleted++;
                            expectedIndex++;
                        }
                        else if (isExpectedEmpty)
                        {
                            deleted++;
                            expectedIndex++;
                        }
                        else if (actualLine.Contains(expectedLine) || expectedLine.Contains(actualLine))
                        {
                            modified++;
                            expectedIndex++;
                            actualIndex++;
                        }
                        else
                        {
                            var actualIsInExpectedLater = expectedLines.Select((s, index) => new { s, index }).Where(x => x.index > expectedIndex).FirstOrDefault(b => b.s == actualLine);
                            var expectedIsInActualLater = actualLines.Select((s, index) => new { s, index }).Where(x => x.index > actualIndex).FirstOrDefault(b => b.s == expectedLine);
                            if (actualIsInExpectedLater != null && expectedIsInActualLater == null)
                            {
                                if (expected_deplaced_line.Contains(expectedIndex))
                                {
                                    expected_deplaced_line.Remove(expectedIndex);
                                }
                                else
                                {
                                    deleted++;
                                }
                                // actual is in expected later
                                expectedIndex++;

                            }
                            else if (actualIsInExpectedLater == null && expectedIsInActualLater != null)
                            {
                                if (actual_deplaced_line.Contains(actualIndex))
                                {
                                    actual_deplaced_line.Remove(actualIndex);
                                }
                                else
                                {
                                    added++;
                                }
                                // expected is in actual later
                                actualIndex++;
                            }
                            else if (actualIsInExpectedLater != null && expectedIsInActualLater != null)
                            {
                                if (actualIsInExpectedLater.index - actualIndex < expectedIsInActualLater.index - expectedIndex)
                                {
                                    expected_deplaced_line.Add(expectedIsInActualLater.index);
                                    actualIndex++;
                                    moved++;
                                }
                                else
                                {
                                    actual_deplaced_line.Add(actualIsInExpectedLater.index);
                                    expectedIndex++;
                                    moved++;
                                }
                            }
                            else
                            {
                                modified++;
                                expectedIndex++;
                                actualIndex++;
                            }
                        }
                    }
                }
            }

            if (modified != 0 || added != 0 || deleted != 0 || moved != 0)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"File : {Path.GetFileName(expectedFilePath)} : {modified} lines modified, {moved} lines moved, {added} lines added, {deleted} lines deleted");
                builder.AppendLine($"kdiff3.exe {expectedFilePath} {actualFilePath}");
                //builder.AppendLine();
                //builder.AppendLine(string.Join("\n\n", differences));
                return builder.ToString();
            }

            return null;
        }
    }
}
