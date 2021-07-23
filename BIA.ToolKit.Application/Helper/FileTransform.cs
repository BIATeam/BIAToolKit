namespace BIA.ToolKit.Application.Helper
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    static public class FileTransform
    {
        static public IList<string> replaceInFileExtenssions = new List<string>() { ".csproj", ".cs", ".sln", ".json", ".config", ".ps1", ".ts", ".html", ".yml" };

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

        static public void ReplaceInFile(string filePath, string oldValue, string newValue)
        {
            string text = File.ReadAllText(filePath);
            if (text.Contains(oldValue))
            {
                if (filePath.Contains(".jpg"))
                {
                    int i = 1;
                }
                text = text.Replace(oldValue, newValue);
                File.WriteAllText(filePath, text);
            }
        }

        static public void CopyFilesRecursively(string sourcePath, string targetPath, string profile, IList<string> filesToExclude)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                if (!filesToExclude.Any( s => Regex.Match(newPath, s, RegexOptions.IgnoreCase).Success))
                {
                    if (Regex.Match(newPath, "\\[.*\\]", RegexOptions.IgnoreCase).Success)
                    {
                        if (newPath.Contains("["+ profile + "]"))
                        {
                            File.Copy(newPath, newPath.Replace(sourcePath, targetPath).Replace("[" + profile + "]",""), true);
                        }
                    }
                    else
                    {
                        File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
                    }
                }

            }
        }       
        
        static public void RemoveRecursively(string targetPath, IList<string> filesToRemove)
        {
            //Copy all the files & Replaces any files with the same name
            foreach (string filePath in Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories))
            {
                if (filesToRemove.Any( s => Regex.Match(filePath, s, RegexOptions.IgnoreCase).Success))
                    File.Delete(filePath);
            }
        }
    }
}
