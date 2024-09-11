namespace BIA.ToolKit.Application.Helper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    static public class FileTransform
    {
        static public IList<string> projectFileExtensions = new List<string>() { ".csproj", ".cs", ".sln", ".json", ".config", ".ps1", ".ts", ".html", ".yml", ".md", ".cshtml", ".edmx", ".sql", ".asax", ".sqlproj", ".scmp", ".xml" };
        static public IList<string> allTextFileExtensions = new List<string>() { ".csproj", ".cs", ".sln", ".json", ".config", ".ps1", ".ts", ".html", ".yml", ".md" , ".cshtml", ".edmx", ".sql", ".asax", ".sqlproj", ".scmp", ".xml",
            ".editorconfig", ".gitignore" , ".prettierrc", ".html" ,".css" ,".scss", ".svg" , ".js", ".ruleset", ".props" };
        static public IList<string> allTextFileNameWithoutExtension = new List<string>() { "browserslist", "Dockerfile" };

        public static void SwapValues(this string[] source, long index1, long index2)
        {
            (source[index2], source[index1]) = (source[index1], source[index2]);
        }

        /// <summary>
        /// Copy files for VSIX AdditionnalFiles folder to the root solution folder.
        /// </summary>
        /// <param name="sourceDir">source folder.</param>
        /// <param name="oldString">old string.</param>
        /// <param name="newString">new string.</param>
        static public void OrderUsing(string sourceDir)
        {
            string[] filesToOrder = Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories);
            foreach (string file in filesToOrder)
            {
                string[] lines = File.ReadAllLines(file);
                OrderUsingInLines(lines);
                File.WriteAllLines(file, lines);
            }

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
        static public void ReplaceInFileAndFileName(string sourceDir, string oldString, string newString, IList<string> replaceInFileExtensions)
        {
            if (oldString == newString) return;
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var name = Path.GetFileName(dir);
                if (name.Contains(oldString))
                {
                    string newName = Path.Combine(sourceDir, name.Replace(oldString, newString));
                    Directory.Move(dir, newName);
                }
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var name = Path.GetFileName(file);
                string targetfile = file;
                if (name.Contains(oldString))
                {
                    string newName = Path.Combine(sourceDir, name.Replace(oldString, newString));
                    File.Move(file, newName);
                    targetfile = newName;
                }
                string extension = Path.GetExtension(targetfile).ToLower();
                if (replaceInFileExtensions.Contains(extension))
                {
                    ReplaceInFile(targetfile, oldString, newString);
                }

                var fileName = Path.GetFileName(file);
                if (allTextFileNameWithoutExtension.Contains(fileName))
                {
                    // todo
                    ReplaceInFile(targetfile, oldString, newString);
                }
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                ReplaceInFileAndFileName(directory, oldString, newString, replaceInFileExtensions);
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
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) return Encoding.UTF32; //UTF-32LE
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return new UTF32Encoding(true, true);  //UTF-32BE

            // We actually have no idea what the encoding is if we reach this point, so
            // you may wish to return null instead of defaulting to ASCII
            return Encoding.ASCII;
        }

        static public void FolderUnix2Dos(string filePath)
        {
            string[] filePaths = Directory.GetFiles(filePath, "*", SearchOption.AllDirectories);

            foreach (string file in filePaths)
            {
                string extension = Path.GetExtension(file).ToLower();
                var name = Path.GetFileName(file);
                if (allTextFileExtensions.Contains(extension) || allTextFileNameWithoutExtension.Contains(name))
                {
                    Unix2Dos(file);
                }

            }
        }

        static public void Unix2Dos(string filename)
        {
            /*string[] lines = File.ReadAllLines(filename);
            List<string> list_of_string = new List<string>();
            foreach (string line in lines)
            {
                list_of_string.Add(line.Replace("\n", "\r\n"));
            }
            File.WriteAllLines(filename, list_of_string);*/

            ReplaceInFile(filename, "\n", "\r\n");
            ReplaceInFile(filename, "\r\r\n", "\r\n");
        }
        /*
                static public void Dos2Unix(string fileName)
                {
                    const byte CR = 0x0D;
                    const byte LF = 0x0A;
                    byte[] data = File.ReadAllBytes(fileName);
                    using (FileStream fileStream = File.OpenWrite(fileName))
                    {
                        BinaryWriter bw = new BinaryWriter(fileStream);
                        int position = 0;
                        int index = 0;
                        do
                        {
                            index = Array.IndexOf<byte>(data, CR, position);
                            if ((index >= 0) && (data[index + 1] == LF))
                            {
                                // Write before the CR
                                bw.Write(data, position, index - position);
                                // from LF
                                position = index + 1;
                            }
                        }
                        while (index >= 0);
                        bw.Write(data, position, data.Length - position);
                        fileStream.SetLength(fileStream.Position);
                    }
                }
        */
        static public void ReplaceInFile(string filePath, string oldValue, string newValue)
        {
            string text = File.ReadAllText(filePath);
            if (text.Contains(oldValue))
            {
                text = text.Replace(oldValue, newValue);
                File.WriteAllText(filePath, text);
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

                if (filesToExclude == null || !filesToExclude.Any(s => Regex.Match(sourceDirName, s, RegexOptions.IgnoreCase).Success))
                {
                    var subTargetPath = sourceDir.Replace(sourcePath, targetPath);
                    Directory.CreateDirectory(sourceDir.Replace(sourcePath, targetPath));
                    CopyFilesRecursively(sourceDir, subTargetPath, profile, filesToExclude, foldersToExcludes);
                }
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string sourceFile in Directory.GetFiles(sourcePath, "*.*", SearchOption.TopDirectoryOnly))
            {

                string sourceFileName = Path.GetFileName(sourceFile);

                if (filesToExclude == null || !filesToExclude.Any(s => Regex.Match(sourceFileName, s, RegexOptions.IgnoreCase).Success))
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
