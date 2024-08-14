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
                return files.Select(x => x.Replace(path, replacementPath)).Where(x => Directory.Exists(x)).ToList();
            }

            return files.ToList();
        }

        public static void CleanFilesByTag(string path, List<string> beginTags, List<string> endTags, string extension)
        {
            var files = Directory.EnumerateFiles(path, extension, SearchOption.AllDirectories);

            Parallel.ForEach(files, file =>
            {
                List<string> lines = File.ReadLines(file).ToList();

                List<int> beginTagIndexes = GetTagIndexes(beginTags, lines);

                if (beginTagIndexes.Any())
                {
                    List<int> endTagIndexes = GetTagIndexes(endTags, lines);

                    if (endTagIndexes.Count >= beginTagIndexes.Count)
                    {
                        var indexPairs = beginTagIndexes
                        .Select(beginTagIndex => new
                        {
                            BeginIndex = beginTagIndex,
                            EndIndex = endTagIndexes.First(endTagIndex => endTagIndex > beginTagIndex)
                        })
                        .OrderByDescending(x => x.BeginIndex).ToList();

                        foreach (var pair in indexPairs)
                        {
                            lines.RemoveRange(pair.BeginIndex, pair.EndIndex - pair.BeginIndex + 1);
                        }

                        File.WriteAllLines(file, lines);
                    }
                    else
                    {
                        throw new Exception("CleanFilesByTag endTagIndexes < beginTagIndexes");
                    }
                }

                static List<int> GetTagIndexes(List<string> tags, List<string> lines)
                {
                    return lines.Select((line, index) => new { line, index })
                    .Where(x => tags.Exists(tag => x.line.Contains(tag)))
                    .Select(x => x.index)
                    .Distinct()
                    .OrderBy(x => x).ToList();
                }
            });
        }
    }
}
