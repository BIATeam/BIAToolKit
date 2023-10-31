namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;

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

                CheckZipArchive(entityName, compagnyName, projectName, files);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in zip file parsing process: {ex.Message}", "Red");
            }

            return tempDir;
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
