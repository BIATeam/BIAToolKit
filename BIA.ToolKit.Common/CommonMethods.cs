using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
namespace BIA.ToolKit.Common
{
    public static class CommonMethods
    {
        public static List<string> GetProperties(FileInfo tempFile, ref string _projectName)
        {
            List<string> result = new List<string>();
            string line;
            string text = string.Empty;
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new StreamReader(tempFile.FullName);
                //Read the first line of text
                line = sr.ReadLine();
                //Continue to read until you reach end of file
                while (line != null)
                {
                    //write the line to console window
                    //Console.WriteLine(line);
                    //Read the next line

                    if (ValidLine(line, ref _projectName))
                    {
                        result.Add(CleanLine(line));
                    }
                    line = sr.ReadLine();
                }
                //close the file
                sr.Close();
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Console.WriteLine("Executing finally block.");
            }

            //File.Delete(tempFile.FullName);
            return result;
        }

        public static string CleanLine(string line)
        {
            string[] values = line.Trim().Split(' ');
            return $"{values[1].Trim()}|{values[2].Trim()}";
        }

        public static bool ValidLine(string line, ref string _projectName)
        {
            bool result = true;
            if (!string.IsNullOrEmpty(line))
            {
                line = line.Replace("{", "").Replace("}", "").Trim();

                if (line.Contains("namespace") || line.Contains("using") || line.Contains(@"///") || line.Contains("public class ") || string.IsNullOrEmpty(line) || line.Contains("[") || line.Contains(@"//"))
                {
                    result = false;
                }

                if (line.Contains("namespace"))
                {
                    _projectName = line.Trim().Split(' ')[1].Replace("namespace ", "").Split('.')[1];
                }
            }
            else
            {
                result = false;
            }

            return result;
        }

        public static void CleanFolder(DirectoryInfo folder)
        {
            //Clean files in folder
            List<FileInfo> fileList = folder.GetFiles().ToList();
            fileList.ForEach(s => s.Delete());
            //Clean files in folders
            List<DirectoryInfo> folderList = folder.GetDirectories().ToList();
            folderList.ForEach(s => CleanFolder(s));
        }

        public static void CreateFile(StringBuilder stringb, string filename)
        {
            //this code section write stringbuilder content to physical text file.
            using (StreamWriter swriter = new StreamWriter(filename))
            {
                swriter.Write(stringb.ToString());
            }
        }

        public static string GetValueFromList(string value, int i)
        {
            return value.Split('|')[i];
        }

        public static string AddNewLine(string line)
        {
            return line + Environment.NewLine;
        }

        public static string ToCamelCase(string value)
        {
            string firstletter = value.Substring(0, 1).ToLower();
            return $"{firstletter}{value.Substring(1)}";
        }

        public static string FirstLetterUppercase(string value)
        {
            string firstletter = value.Substring(0, 1).ToUpper();
            return $"{firstletter}{value.Substring(1).ToLower()}";
        }

        public static string ToAngularTypes(string value)
        {
            string result = string.Empty;
            switch (value.Replace("?", "").ToLower())
            {
                case "int":
                    result = "number";
                    break;
                default:
                    result = value;
                    break;
            }

            return result.ToLower();
        }

        public static void CheckFoler(DirectoryInfo dir)
        {
            if (!dir.Exists)
            {
                dir.Create();
            }
        }
    }
}
