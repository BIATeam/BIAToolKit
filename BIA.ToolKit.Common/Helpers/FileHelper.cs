namespace BIA.ToolKit.Common.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

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

        public static void CleanFilesByTag(string path, string beginTag, string endTag, string extensionFile)
        {
            foreach (string directory in Directory.GetDirectories(path))
            {
                CleanFilesByTag(directory, beginTag, endTag, extensionFile);
            }

            foreach (string filepath in Directory.GetFiles(path, extensionFile))
            {
                string text = File.ReadAllText(filepath);
                string pattern = String.Format("{0}.*?{1}\\s*", Regex.Escape(beginTag), Regex.Escape(endTag));
                string cleanedText = Regex.Replace(text, pattern, "", RegexOptions.Singleline);
                File.WriteAllText(filepath, cleanedText);
            }
        }
    }
}
