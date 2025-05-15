namespace BIA.ToolKit.Test.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    internal static class FileCompare
    {
        public static string? FilesEquals(string expectedFilePath, string actualFilePath)
        {
            var expectedLines = File.ReadAllLines(expectedFilePath);
            var actualLines = File.ReadAllLines(actualFilePath);

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

            while (expectedIndex < expectedLines.Length || actualIndex < actualLines.Length)
            {
                if (expectedIndex >= expectedLines.Length)
                {
                    added++;
                    actualIndex++;
                }
                else if (actualIndex >= actualLines.Length)
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
                                if ((actualIsInExpectedLater.index - actualIndex) < (expectedIsInActualLater.index - expectedIndex))
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
