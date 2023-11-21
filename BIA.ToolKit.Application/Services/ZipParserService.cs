namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text.RegularExpressions;

    public class ZipParserService
    {
        private readonly IConsoleWriter consoleWriter;

        private const string ANGULAR_MARKER = "BIAToolKit - Begin XXXXX block";

        private string OldValuePascalSingular = "Plane";
        private string OldValuePascalPlural = "Planes";
        private string NewValuePascalSingular = "Plane";
        private string NewValuePascalPlural = "Planes";

        private string OldValueKebabSingular;
        private string OldValueKebabPlural;
        private string NewValueKebabSingular;
        private string NewValueKebabPlural;

        public ZipParserService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void InitRenameValues(string newValueSingular, string newValuePlurial, string oldValueSingular = "Plane", string oldValuePlurial = "Planes")
        {
            this.OldValuePascalSingular = oldValueSingular;
            this.OldValuePascalPlural = oldValuePlurial;

            this.NewValuePascalSingular = newValueSingular;
            this.NewValuePascalPlural = newValuePlurial;

            this.OldValueKebabSingular = ConvertPascalToKebabCase(OldValuePascalSingular);
            this.OldValueKebabPlural = ConvertPascalToKebabCase(OldValuePascalPlural);

            this.NewValueKebabSingular = ConvertPascalToKebabCase(NewValuePascalSingular);
            this.NewValueKebabPlural = ConvertPascalToKebabCase(NewValuePascalPlural);
        }

        public (string, Dictionary<string, string>) ReadZip(string zipPath, string entityName, string compagnyName, string projectName, string folderType)
        {
            string tempDir = null;
            Dictionary<string, string> files = null;

            try
            {
                if (!File.Exists(zipPath))
                {
                    consoleWriter.AddMessageLine("Zip file path not exist", "Red");
                    return (tempDir, files);
                }

#if DEBUG
                consoleWriter.AddMessageLine($"*** Parse zip file: '{zipPath}' ***", "Green");
#endif

                tempDir = Path.Combine(Path.GetTempPath(), "BiaToolKit_CRUDGenerator", folderType);
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                Directory.CreateDirectory(tempDir);

                files = new();
                using ZipArchive archive = ZipFile.OpenRead(zipPath);
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.Name))   // entry is folder
                    {
                        continue;
                    }

#if DEBUG
                    //consoleWriter.AddMessageLine($"File found: '{entry.FullName}'", "Green");
#endif
                    entry.ExtractToFile(Path.Combine(tempDir, entry.Name));
                    files.Add(entry.Name, entry.FullName);
                }

                return (tempDir, files);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in zip file parsing process: {ex.Message}", "Red");
            }

            return (tempDir, files);
        }

        public FileType? GetFileType(string fileName)
        {
            if (fileName == null)
            {
                consoleWriter.AddMessageLine($"File type not found for: '{fileName}'", "Orange");
                return null;
            }
            else if (fileName.EndsWith("AppService.cs"))
            {
                if (fileName.StartsWith("I"))
                {
                    return FileType.IAppService;
                }
                else
                {
                    return FileType.AppService;
                }
            }
            else if (fileName.EndsWith("Dto.cs"))
            {
                return FileType.Dto;
            }
            else if (fileName.EndsWith("Controller.cs"))
            {
                return FileType.Controller;
            }
            else if (fileName.EndsWith("Mapper.cs"))
            {
                return FileType.Mapper;
            }
            else
            {
                return FileType.Entity;
            }
        }

        public string GetEntityName(string fileName, FileType? type)
        {
            if (fileName == null || type == null)
            {
                return null;
            }

            string name = null;
            string pattern = "";
            switch (type)
            {
                case FileType.AppService:
                case FileType.IAppService:
                    pattern = @"^I?(\w+)AppService\.cs$";
                    break;
                case FileType.Dto:
                    pattern = @"^(\w+)Dto\.cs$";
                    break;
                case FileType.Controller:
                    pattern = @"^((\w+)s|(\w+))Controller\.cs$";
                    break;
                case FileType.Mapper:
                    pattern = @"^(\w+)Mapper\.cs$";
                    break;
                case FileType.Entity:
                    pattern = @"^(\w+)\.cs$";
                    break;
                default:
                    consoleWriter.AddMessageLine($"Get entity name not implemented for type: '{type}' and file '{fileName}'", "Orange");
                    break;
            }

            MatchCollection matches = new Regex(pattern).Matches(fileName);
            if (matches != null && matches.Count > 0)
            {
                GroupCollection groups = matches[0].Groups;
                if (groups.Count > 0)
                {
                    if (groups.Count > 2)
                    {
                        name = groups[2].Value; // Used only for controllers
                    }
                    else
                    {
                        name = groups[1].Value;
                    }
                }
            }

            return name;
        }


        private string RenameFile(string fileName)
        {
            if (fileName.Contains(OldValuePascalPlural))
            {
                return fileName.Replace(OldValuePascalPlural, NewValuePascalPlural);
            }
            else if (fileName.Contains(OldValueKebabPlural))
            {
                return fileName.Replace(OldValueKebabPlural, NewValueKebabPlural);
            }
            else if (fileName.Contains(OldValuePascalSingular))
            {
                return fileName.Replace(OldValuePascalSingular, NewValuePascalSingular);
            }
            else if (fileName.Contains(OldValueKebabSingular))
            {
                return fileName.Replace(OldValueKebabSingular, NewValueKebabSingular);
            }

            return fileName;
        }

        private void ReplaceInFile()
        {

        }

        private string ConvertPascalToKebabCase(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return Regex.Replace(value, "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z0-9])", "-$1", RegexOptions.Compiled)
                .Trim().ToLower();
        }

    }
}
