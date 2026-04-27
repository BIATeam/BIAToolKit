namespace BIA.ToolKit.Application.Helper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    static public partial class FileTransform
    {
        internal static readonly IList<string> projectFileExtensions = [".csproj", ".cs", ".sln", ".json", ".config", ".ps1", ".ts", ".html", ".yml", ".md", ".cshtml", ".edmx", ".sql", ".asax", ".sqlproj", ".scmp", ".xml", ".webmanifest", ".slnf"];
        private static readonly IList<string> allTextFileExtensions = [".csproj", ".cs", ".sln", ".json", ".config", ".ps1", ".ts", ".html", ".yml", ".md", ".cshtml", ".edmx", ".sql", ".asax", ".sqlproj", ".scmp", ".xml", ".webmanifest", ".editorconfig", ".gitignore", ".prettierrc", ".html", ".css", ".scss", ".svg", ".js", ".ruleset", ".props", ".slnf"];
        private static readonly IList<string> allTextFileNameWithoutExtension = ["browserslist", "Dockerfile", "Dockerfile_Rosa"];

        public static void SwapValues(this string[] source, long index1, long index2)
        {
            (source[index2], source[index1]) = (source[index1], source[index2]);
        }

        /// <summary>
        /// Order the usings in a csharp file
        /// </summary>
        /// <param name="sourceDir">source folder.</param>
        /// <param name="oldString">old string.</param>
        /// <param name="newString">new string.</param>
        static public void OrderUsingFromFolder(string sourceDir)
        {
            string[] filesToOrder = Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories);
            foreach (string file in filesToOrder)
            {
                OrderUsingFromFile(file);
            }
        }

        static public bool OrderUsingFromFile(string path)
        {
            string oldContent = File.ReadAllText(path);
            string endOfLine = Environment.NewLine;
            Match endOfLineMatch = MyRegex().Match(oldContent);
            if (endOfLineMatch.Success)
            {
                endOfLine = endOfLineMatch.Value;
            }
            string[] lines = File.ReadAllLines(path);
            OrderUsingInLines(lines);
            string newContent = string.Join(endOfLine, lines);
            Match match = EndOfFileRegex().Match(oldContent);
            if (match.Success)
            {
                newContent += match.Value;
            }
            // Skip the disk write when nothing changed: avoids touching mtime
            // and saves IO when the file is already correctly ordered.
            if (oldContent == newContent)
            {
                return false;
            }
            File.WriteAllText(path, newContent);
            return true;
        }

        // Sorts every using statement found in the file in-place. System.*
        // usings come first (preserved relative to themselves), then the
        // others, both alphabetic by namespace. The previous implementation
        // was a recursive bubble sort: O(N^2) and unbounded recursion depth
        // on long using blocks, which made post-generation ordering visibly
        // slow on large entities. Now O(N log N) and iterative.
        private static void OrderUsingInLines(string[] lines)
        {
            var usingIndexes = new List<int>(lines.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                if (IsUsingStatement(lines[i].Trim()))
                {
                    usingIndexes.Add(i);
                }
            }
            if (usingIndexes.Count < 2)
            {
                return;
            }

            string[] sortedUsings = usingIndexes
                .Select(i => lines[i])
                .OrderBy(line =>
                {
                    string body = line.Trim().Substring("using ".Length);
                    // Sort key: System.* group ('0_'), then everything else ('1_'),
                    // each sorted alphabetically by their namespace body.
                    return (body.StartsWith("System.") || body.StartsWith("System;") ? "0_" : "1_") + body;
                }, StringComparer.Ordinal)
                .ToArray();

            for (int j = 0; j < usingIndexes.Count; j++)
            {
                lines[usingIndexes[j]] = sortedUsings[j];
            }
        }

        private static bool IsUsingStatement(string line)
        {
            return line.StartsWith("using ") && line.EndsWith(';') && !line.Contains('=');
        }

        /// <summary>
        /// Copy files for VSIX AdditionnalFiles folder to the root solution folder.
        /// </summary>
        /// <param name="sourceDir">source folder.</param>
        /// <param name="oldString">old string.</param>
        /// <param name="newString">new string.</param>
        /// <param name="replaceInFileExtensions">types of files to include</param>
        static public async Task ReplaceInFileAndFileName(string sourceDir, string oldString, string newString, IList<string> replaceInFileExtensions, IConsoleWriter consoleWriter, CancellationToken cancellationToken = default)
        {
            if (oldString == newString) return;
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string name = Path.GetFileName(dir);
                if (name.Contains(oldString))
                {
                    string newName = Path.Combine(sourceDir, name.Replace(oldString, newString));
                    await Task.Run(() => Directory.Move(dir, newName), cancellationToken);
                }
            }

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string name = Path.GetFileName(file);
                string targetfile = file;
                if (name.Contains(oldString))
                {
                    string newName = Path.Combine(sourceDir, name.Replace(oldString, newString));
                    await Task.Run(() => File.Move(file, newName), cancellationToken);
                    targetfile = newName;
                }
                string extension = Path.GetExtension(targetfile).ToLower();
                if (replaceInFileExtensions.Contains(extension))
                {
                    await ReplaceInFile(targetfile, oldString, newString, consoleWriter, cancellationToken);
                }

                string fileName = Path.GetFileName(file);
                if (allTextFileNameWithoutExtension.Contains(fileName))
                {
                    await ReplaceInFile(targetfile, oldString, newString, consoleWriter, cancellationToken);
                }
            }

            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ReplaceInFileAndFileName(directory, oldString, newString, replaceInFileExtensions, consoleWriter, cancellationToken);
            }
        }


        /// <summary>
        /// Copy files for VSIX AdditionnalFiles folder to the root solution folder.
        /// </summary>
        /// <param name="sourceDir">source folder.</param>
        /// <param name="beginString">starting string of code to remove.</param>
        /// <param name="endString">ending string of code to remove.</param>
        /// <param name="replaceInFileExtensions">types of files to include</param>
        static public void RemoveTemplateOnly(string sourceDir, string beginString, string endString, IList<string> replaceInFileExtensions)
        {

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                _ = Path.GetFileName(file);
                string targetfile = file;
                string extension = Path.GetExtension(targetfile).ToLower();
                if (replaceInFileExtensions.Contains(extension))
                {
                    RemoveTemplateOnlyInFile(targetfile, beginString, endString);
                }
            }

            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                RemoveTemplateOnly(directory, beginString, endString, replaceInFileExtensions);
            }
        }

        static public void RemoveTemplateOnlyInFile(string filePath, string beginString, string endString)
        {
            string text = File.ReadAllText(filePath);
            if (text.Contains(beginString))
            {
                text = "";
                string line;
                List<string> lines = [];
                bool inSequence = false;
                var file = new System.IO.StreamReader(filePath);

                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains(beginString))
                    {
                        inSequence = true;
                    }
                    if (!inSequence)
                    {
                        text += line;
                        lines.Add(line);
                    }
                    if (line.Contains(endString))
                    {
                        inSequence = false;
                    }
                }

                file.Close();
                Encoding encoding = GetEncoding(filePath);
                File.WriteAllLines(filePath, lines, encoding);

            }
        }

        /// <summary>
        /// Determines a text file's encoding by analyzing its byte order mark (BOM).
        /// Defaults to ASCII when detection of the text file's endianness fails.
        /// </summary>
        /// <param name="filename">The text file to analyze.</param>
        /// <returns>The detected encoding.</returns>
        public static Encoding GetEncoding(string filename)
        {
            // Read the BOM
            byte[] bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.ReadExactly(bom, 0, 4);
            }

            // Analyze the BOM
#pragma warning disable SYSLIB0001 // Type or member is obsolete
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
#pragma warning restore SYSLIB0001 // Type or member is obsolete
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) return Encoding.UTF32; //UTF-32LE
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return new UTF32Encoding(true, true);  //UTF-32BE

            // We actually have no idea what the encoding is if we reach this point, so
            // you may wish to return null instead of defaulting to ASCII
            return Encoding.ASCII;
        }

        static public async Task FolderUnix2Dos(string filePath)
        {
            string[] filePaths = Directory.GetFiles(filePath, "*", SearchOption.AllDirectories);

            foreach (string file in filePaths)
            {
                string extension = Path.GetExtension(file).ToLower();
                string name = Path.GetFileName(file);
                if (allTextFileExtensions.Contains(extension) || allTextFileNameWithoutExtension.Contains(name))
                {
                    await Unix2Dos(file);
                }

            }
        }

        static public async Task Unix2Dos(string filename)
        {
            await ReplaceInFile(filename, "\n", "\r\n");
            await ReplaceInFile(filename, "\r\r\n", "\r\n");
        }

        static public async Task ReplaceInFile(string filePath, string oldValue, string newValue, IConsoleWriter _ = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string text = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (text.Contains(oldValue))
            {
                text = text.Replace(oldValue, newValue);
                await File.WriteAllTextAsync(filePath, text, cancellationToken);
            }
        }

        static public void CopyFilesRecursively(string sourcePath, string targetPath, string profile = "", IList<string> filesToExclude = null, IList<string> foldersToExcludes = null)
        {
            //Now Create all of the directories
            foreach (string sourceDir in Directory.GetDirectories(sourcePath, "*", SearchOption.TopDirectoryOnly))
            {
                string sourceDirName = Path.GetFileName(sourceDir);

                if (foldersToExcludes?.Any(f => Regex.IsMatch(sourceDirName, f, RegexOptions.IgnoreCase)) == true)
                {
                    continue; // skip this folder if its name matches any of the folder names to exclude
                }

                string subTargetPath = sourceDir.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(sourceDir.Replace(sourcePath, targetPath));
                CopyFilesRecursively(sourceDir, subTargetPath, profile, filesToExclude, foldersToExcludes);
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string sourceFile in Directory.GetFiles(sourcePath, "*.*", SearchOption.TopDirectoryOnly))
            {
                bool isFilenameExcluded = filesToExclude?.Any(s => Regex.IsMatch(Path.GetFileName(sourceFile), s, RegexOptions.IgnoreCase)) == true;
                bool isFilePathExcluded = filesToExclude?.Any(s => Regex.IsMatch(sourceFile, s, RegexOptions.IgnoreCase)) == true;
                if (!isFilenameExcluded && !isFilePathExcluded)
                {
                    if (profile != "" && ProfileBracketRegex().IsMatch(sourceFile))
                    {
                        if (sourceFile.Contains("[" + profile + "]"))
                        {
                            File.Copy(sourceFile, sourceFile.Replace(sourcePath, targetPath).Replace("[" + profile + "]", ""), true);
                        }
                    }
                    else
                    {
                        File.Copy(sourceFile, sourceFile.Replace(sourcePath, targetPath), true);
                    }
                }
            }
        }

        static public void RemoveRecursively(string targetPath, IList<string> filesToRemove)
        {
            //Copy all the files & Replaces any files with the same name
            foreach (string filePath in Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(filePath);
                if (filesToRemove.Any(s => Regex.IsMatch(fileName, s, RegexOptions.IgnoreCase)))
                    File.Delete(filePath);
            }
        }

        public static void ForceDeleteDirectory(string path)
        {
            var directory = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };

            foreach (FileSystemInfo info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }

            directory.Delete(true);
        }

        [GeneratedRegex(@"([\r\n]+\Z)")]
        private static partial Regex EndOfFileRegex();

        [GeneratedRegex(@"\[.*\]", RegexOptions.IgnoreCase)]
        private static partial Regex ProfileBracketRegex();

        [GeneratedRegex(@"([\r\n]+)")]
        private static partial Regex MyRegex();
    }
}
