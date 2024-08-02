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

        public static void CleanFilesByTag(string path, string beginTag, string endTag, string extension)
        {
            var files = Directory.EnumerateFiles(path, extension, SearchOption.AllDirectories);

            // foreach (string file in files)
            Parallel.ForEach(files, file =>
            {
                List<string> lines = File.ReadLines(file).ToList();

                // Indexes of tags
                List<int> tagIndexes = lines
                    .Select((line, index) => new { line, index })
                    .Where(x => x.line.Contains(beginTag) || x.line.Contains(endTag))
                    .Select(x => x.index).Distinct().ToList();

                // Assure that we have pairs
                if (tagIndexes.Any() && tagIndexes.Count % 2 == 0)
                {
                    // Create pair to remove range between tags
                    var indexPairs = tagIndexes
                        .Select((value, i) => new { Value = value, Index = i })
                        .GroupBy(x => x.Index / 2)
                        .Select(group => new { Begin = group.First().Value, End = group.Last().Value })
                        .ToList();

                    indexPairs.Reverse();

                    // Indexes of empty lines
                    List<int> emptyIndexes = lines
                        .Select((line, index) => new { line, index })
                        .Where(x => string.IsNullOrWhiteSpace(x.line))
                        .Select(x => x.index).Distinct().ToList();

                    foreach (var pair in indexPairs)
                    {
                        int begin = pair.Begin;
                        int end = pair.End;

                        // Expand range to remove empty lines surrounding tags
                        if (emptyIndexes.Contains(pair.Begin - 1))
                        {
                            begin = pair.Begin - 1;
                        }
                        else if (emptyIndexes.Contains(pair.End + 1))
                        {
                            end = pair.End + 1;
                        }

                        lines.RemoveRange(begin, end - begin + 1);
                    }

                    File.WriteAllLines(file, lines);
                }
            });
        }
    }
}
