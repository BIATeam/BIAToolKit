namespace BIA.ToolKit.Application.Helper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    static public class FileTransform
    {
        static public IList<string> replaceInFileExtensions = new List<string>() { ".csproj", ".cs", ".sln", ".json", ".config", ".ps1", ".ts", ".html", ".yml", ".md" };
        static public IList<string> allTextFileExtensions = new List<string>() { ".csproj", ".cs", ".sln", ".json", ".config", ".ps1", ".ts", ".html", ".yml", ".md", 
            ".editorconfig", ".gitignore" , ".prettierrc", ".html" ,".css" ,".scss", ".svg" , ".js", ".ruleset", ".props" };
        static public IList<string> allTextFileNameWithoutExtension = new List<string>() { "browserslist" };

        /// <summary>
        /// Copy files for VSIX AdditionnalFiles folder to the root solution folder.
        /// </summary>
        /// <param name="sourceDir">source folder.</param>
        /// <param name="oldString">old string.</param>
        /// <param name="newString">new string.</param>
        static public void ReplaceInFileAndFileName(string sourceDir, string oldString, string newString, IList<string> replaceInFileExtenssions)
        {
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
                if (replaceInFileExtenssions.Contains(extension))
                {
                    ReplaceInFile(targetfile, oldString, newString);
                }
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                ReplaceInFileAndFileName(directory, oldString, newString, replaceInFileExtenssions);
            }
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

        static public void CopyFilesRecursively(string sourcePath, string targetPath, string profile, IList<string> filesToExclude)
        {
            //Now Create all of the directories
            foreach (string sourceDir in Directory.GetDirectories(sourcePath, "*", SearchOption.TopDirectoryOnly))
            {
                string sourceDirName = Path.GetFileName(sourceDir);

                if (!filesToExclude.Any(s => Regex.Match(sourceDirName, s, RegexOptions.IgnoreCase).Success))
                {
                    var subTargetPath = sourceDir.Replace(sourcePath, targetPath);
                    Directory.CreateDirectory(sourceDir.Replace(sourcePath, targetPath));
                    CopyFilesRecursively(sourceDir, subTargetPath, profile, filesToExclude);
                }
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string sourceFile in Directory.GetFiles(sourcePath, "*.*", SearchOption.TopDirectoryOnly))
            {

                string sourceFileName = Path.GetFileName(sourceFile);

                if (!filesToExclude.Any( s => Regex.Match(sourceFileName, s, RegexOptions.IgnoreCase).Success))
                {
                    if (Regex.Match(sourceFile, "\\[.*\\]", RegexOptions.IgnoreCase).Success)
                    {
                        if (sourceFile.Contains("["+ profile + "]"))
                        {
                            File.Copy(sourceFile, sourceFile.Replace(sourcePath, targetPath).Replace("[" + profile + "]",""), true);
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
                if (filesToRemove.Any( s => Regex.Match(fileName, s, RegexOptions.IgnoreCase).Success))
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
