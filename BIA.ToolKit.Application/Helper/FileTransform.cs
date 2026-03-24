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

    static public class FileTransform
    {
        static public IList<string> projectFileExtensions = new List<string>() { ".csproj", ".cs", ".sln", ".json", ".config", ".ps1", ".ts", ".html", ".yml", ".md", ".cshtml", ".edmx", ".sql", ".asax", ".sqlproj", ".scmp", ".xml", ".webmanifest", ".slnf" };
        static public IList<string> allTextFileExtensions = new List<string>() { ".csproj", ".cs", ".sln", ".json", ".config", ".ps1", ".ts", ".html", ".yml", ".md", ".cshtml", ".edmx", ".sql", ".asax", ".sqlproj", ".scmp", ".xml", ".webmanifest", ".editorconfig", ".gitignore", ".prettierrc", ".html", ".css", ".scss", ".svg", ".js", ".ruleset", ".props", ".slnf" };
        static public IList<string> allTextFileNameWithoutExtension = new List<string>() { "browserslist", "Dockerfile", "Dockerfile_Rosa" };

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
            Match endOfLineMatch = Regex.Match(oldContent, @"([\r\n]+)");
            if (endOfLineMatch.Success)
            {
                endOfLine = endOfLineMatch.Value;
            }
            string[] lines = File.ReadAllLines(path);
            OrderUsingInLines(lines);
            string newContent = string.Join(endOfLine, lines);
            Match match = Regex.Match(oldContent, @"([\r\n]+\Z)");
            if (match.Success)
            {
                newContent += match.Value;
            }
            File.WriteAllText(path, newContent);
            return oldContent != newContent;
        }

        private static void OrderUsingInLines(string[] lines)
        {
            bool orderChanged = false;
            for (int i = 0; i < lines.Length - 1; i++)
            {
                string line = lines[i].Trim();
                string nextLine = lines[i + 1].Trim();
                if (!line.StartsWith("using System")
                    && IsUsingStatement(line)
                    && IsUsingStatement(nextLine)
                    && line.Replace("using ", "").CompareTo(nextLine.Replace("using ", "")) > 0)
                {
                    orderChanged = true;
                    lines.SwapValues(i, i + 1);
                }
            }
            if (orderChanged)
            {
                OrderUsingInLines(lines);
            }
        }

        private static bool IsUsingStatement(string line)
        {
            return line.StartsWith("using ") && line.EndsWith(";") && !line.Contains("=");
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
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = Path.GetFileName(dir);
                if (name.Contains(oldString))
                {
                    string newName = Path.Combine(sourceDir, name.Replace(oldString, newString));
                    await Task.Run(() => Directory.Move(dir, newName), cancellationToken);
                }
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = Path.GetFileName(file);
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

                var fileName = Path.GetFileName(file);
                if (allTextFileNameWithoutExtension.Contains(fileName))
                {
                    await ReplaceInFile(targetfile, oldString, newString, consoleWriter, cancellationToken);
                }
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
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

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var name = Path.GetFileName(file);
                string targetfile = file;
                string extension = Path.GetExtension(targetfile).ToLower();
                if (replaceInFileExtensions.Contains(extension))
                {
                    RemoveTemplateOnlyInFile(targetfile, beginString, endString);
                }
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
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
                List<string> lines = new List<string>();
                bool inSequence = false;
                System.IO.StreamReader file = new System.IO.StreamReader(filePath);

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
                var encoding = GetEncoding(filePath);
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
            var bom = new byte[4];
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
                var name = Path.GetFileName(file);
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

        static public async Task ReplaceInFile(string filePath, string oldValue, string newValue, IConsoleWriter consoleWriter = null, CancellationToken cancellationToken = default)
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

                if (foldersToExcludes?.Any(f => Regex.Match(sourceDirName, f, RegexOptions.IgnoreCase).Success) == true)
                {
                    continue; // skip this folder if its name matches any of the folder names to exclude
                }

                var subTargetPath = sourceDir.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(sourceDir.Replace(sourcePath, targetPath));
                CopyFilesRecursively(sourceDir, subTargetPath, profile, filesToExclude, foldersToExcludes);
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string sourceFile in Directory.GetFiles(sourcePath, "*.*", SearchOption.TopDirectoryOnly))
            {
                var isFilenameExcluded = filesToExclude?.Any(s => Regex.Match(Path.GetFileName(sourceFile), s, RegexOptions.IgnoreCase).Success) == true;
                var isFilePathExcluded = filesToExclude?.Any(s => Regex.Match(sourceFile, s, RegexOptions.IgnoreCase).Success) == true;
                if (!isFilenameExcluded && !isFilePathExcluded)
                {
                    if (profile != "" && Regex.Match(sourceFile, "\\[.*\\]", RegexOptions.IgnoreCase).Success)
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
                if (filesToRemove.Any(s => Regex.Match(fileName, s, RegexOptions.IgnoreCase).Success))
                    File.Delete(filePath);
            }
        }

        public static void ForceDeleteDirectory(string path)
        {
            var directory = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };

            foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }

            directory.Delete(true);
        }
    }
}
