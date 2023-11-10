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


        public ZipParserService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public string ReadZip(string zipPath, string entityName, string compagnyName, string projectName)
        {
            string tempDir = null;

            try
            {
                if (!File.Exists(zipPath))
                {
                    consoleWriter.AddMessageLine("Zip file path not exist", "Red");
                    return null;
                }

#if DEBUG
                consoleWriter.AddMessageLine($"*** Parse zip file: '{zipPath}' ***", "Green");
#endif

                tempDir = Path.Combine(Path.GetTempPath(), "BiaToolKit_CRUDGenerator");
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                Directory.CreateDirectory(tempDir);

                List<string> files = new();
                using ZipArchive archive = ZipFile.OpenRead(zipPath);
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith(".cs"))
                    {
                        continue;
                    }

#if DEBUG
                    consoleWriter.AddMessageLine($"File found: '{entry.FullName}'", "Green");
#endif
                    entry.ExtractToFile(Path.Combine(tempDir, entry.Name));
                    files.Add(entry.FullName);
                }

                //CheckZipArchive(entityName, compagnyName, projectName, files);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in zip file parsing process: {ex.Message}", "Red");
            }

            return tempDir;
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

        private bool CheckZipArchive(string entityName, string compagnyName, string projectName, List<string> files)
        {
            string applicationIFile = $@"{compagnyName}.{projectName}.Application/{entityName}/I{entityName}AppService.cs";
            string applicationFile = $@"{compagnyName}.{projectName}.Application/{entityName}/{entityName}AppService.cs";
            string domainFile = $@"{compagnyName}.{projectName}.Domain/{entityName}Module/Aggregate/{entityName}.cs";
            string domainMapperFile = $@"{compagnyName}.{projectName}.Domain/{entityName}Module/Aggregate/{entityName}Mapper.cs";
            string domainDtoFile = $@"{compagnyName}.{projectName}.Domain.Dto/{entityName}/{entityName}Dto.cs";
            string controllerFile = $@"{compagnyName}.{projectName}.Presentation.Api/Controllers/{entityName}/{entityName}sController.cs";

            if (!files.Contains(applicationIFile) ||
                !files.Contains(applicationFile) ||
                !files.Contains(domainFile) ||
                !files.Contains(domainMapperFile) ||
                !files.Contains(domainDtoFile) ||
                !files.Contains(controllerFile))
            {
                consoleWriter.AddMessageLine($"All files not found on zip archive.", "Orange");
                return false;
            }

#if DEBUG
            consoleWriter.AddMessageLine($"All files found on zip archive.", "Green");
#endif
            return true;
        }
    }
}
