namespace BIA.ToolKit.Helper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    static class FileTransform
    {
        /// <summary>
        /// Copy files for VSIX AdditionnalFiles folder to the root solution folder.
        /// </summary>
        /// <param name="sourceDir">source folder.</param>
        /// <param name="oldString">old string.</param>
        /// <param name="newString">new string.</param>
        static public void ReplaceInFileAndFileName(string sourceDir, string oldString, string newString)
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

                ReplaceInFile(targetfile, oldString, newString);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                ReplaceInFileAndFileName(directory, oldString, newString);
            }
        }

        static public void ReplaceInFile(string filePath, string oldValue, string newValue)
        {
            string text = File.ReadAllText(filePath);
            if (text.Contains(oldValue))
            {
                text = text.Replace(oldValue, newValue);
                File.WriteAllText(filePath, text);
            }
        }

        static public void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                if (!newPath.Contains(".biaCompanyFiles"))
                    File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }
    }
}
