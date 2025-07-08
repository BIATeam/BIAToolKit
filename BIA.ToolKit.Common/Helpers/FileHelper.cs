namespace BIA.ToolKit.Common.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public static class FileHelper
    {
        /// <summary>
        /// Gets the files from path with extension.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="extension">The extension.</param>
        /// <param name="replacementPath">The replacement path.</param>
        /// <returns></returns>
        public static List<string> GetFilesFromPathWithExtension(string path, string extension, string replacementPath = null)
        {
            string[] files = Directory.GetFiles(path, extension, SearchOption.AllDirectories);
            if (!string.IsNullOrWhiteSpace(replacementPath))
            {
                return files.Select(x => x.Replace(path, replacementPath)).ToList();
            }

            return files.ToList();
        }

        public static void CleanFilesByTag(string path, List<string> beginTags, string endTag, string extension, bool hasRemoveRange)
        {
            string[] files = Directory.GetFiles(path, extension, SearchOption.AllDirectories);

            Parallel.ForEach(files, file =>
            {
                var lines = File.ReadLines(file).ToList();

                foreach (var beginTag in beginTags)
                {
                    var beginTagIndexes = GetTagIndexes(beginTag, lines);
                    if (!beginTagIndexes.Any())
                        continue;

                    var endTagIndexes = GetTagIndexes(endTag, lines);
                    var indexPairs = beginTagIndexes
                    .Select(beginTagIndex => new
                    {
                        BeginIndex = beginTagIndex,
                        EndIndex = endTagIndexes.First(endTagIndex => endTagIndex > beginTagIndex)
                    })
                    .OrderByDescending(x => x.BeginIndex).ToList();

                    foreach (var pair in indexPairs)
                    {
                        if (hasRemoveRange)
                        {
                            var elseTagIndexes = GetElseTagIndexesInRange(lines, pair.BeginIndex, pair.EndIndex);
                            if (elseTagIndexes.Any())
                            {
                                lines.RemoveAt(pair.EndIndex);
                                var elseTagIndex = elseTagIndexes.First() + pair.BeginIndex;
                                lines.RemoveRange(pair.BeginIndex, elseTagIndex - pair.BeginIndex + 1);
                            }
                            else
                            {
                                lines.RemoveRange(pair.BeginIndex, pair.EndIndex - pair.BeginIndex + 1);
                            }
                        }
                        else
                        {
                            var elseTagIndexes = GetElseTagIndexesInRange(lines, pair.BeginIndex, pair.EndIndex);
                            if (elseTagIndexes.Any())
                            {
                                var elseTagIndex = elseTagIndexes.First() + pair.BeginIndex;
                                lines.RemoveRange(elseTagIndex, pair.EndIndex - elseTagIndex + 1);
                            }
                            else
                            {
                                lines.RemoveAt(pair.EndIndex);
                            }
                            lines.RemoveAt(pair.BeginIndex);
                        }
                    }

                    File.WriteAllLines(file, lines);
                }

                static List<int> GetTagIndexes(string tag, List<string> lines)
                {
                    return lines.Select((line, index) => new { line, index })
                    .Where(x => x.line.Contains(tag))
                    .Select(x => x.index)
                    .Distinct()
                    .OrderBy(x => x).ToList();
                }

                static List<int> GetElseTagIndexesInRange(List<string> lines, int rangeStart, int rangeEnd)
                {
                    var rangeLines = lines.Take(new Range(rangeStart, rangeEnd + 1)).ToList();
                    return GetTagIndexes("#else", rangeLines);
                }
            });
        }
    }
}
