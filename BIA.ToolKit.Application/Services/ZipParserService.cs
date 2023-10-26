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

        private string applicationIFile;
        private string applicationFile;
        private string domainFile;
        private string domainMapperFile;
        private string domainDtoFile;
        private string controllerFile;


        public ZipParserService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        private void InitPath(string entityName, string compagnyName, string projectName)
        {
            applicationIFile = $@"{compagnyName}.{projectName}.Application/{entityName}/I{entityName}AppService.cs";
            applicationFile = $@"{compagnyName}.{projectName}.Application/{entityName}/{entityName}AppService.cs";
            domainFile = $@"{compagnyName}.{projectName}.Domain/{entityName}Module/Aggregate/{entityName}.cs";
            domainMapperFile = $@"{compagnyName}.{projectName}.Domain/{entityName}Module/Aggregate/{entityName}Mapper.cs";
            domainDtoFile = $@"{compagnyName}.{projectName}.Domain.Dto/{entityName}/{entityName}Dto.cs";
            controllerFile = $@"{compagnyName}.{projectName}.Presentation.Api/Controllers/{entityName}/{entityName}sController.cs";
        }

        public bool ReadZip(string zipPath, string entityName, string compagnyName, string projectName)
        {
            bool allFilesFound = false;

            try
            {
                if (!File.Exists(zipPath))
                {
                    consoleWriter.AddMessageLine("Zip file path not exist", "Red");
                    return false;
                }

                InitPath(entityName, compagnyName, projectName);

#if DEBUG
                consoleWriter.AddMessageLine($"*** Parse zip file: '{zipPath}' ***", "Green");
#endif

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
                    files.Add(entry.FullName);
                }

                if (files.Contains(applicationIFile) &&
                    files.Contains(applicationFile) &&
                    files.Contains(domainFile) &&
                    files.Contains(domainMapperFile) &&
                    files.Contains(domainDtoFile) &&
                    files.Contains(controllerFile)
                    )
                {
                    allFilesFound = true;
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in zip file parsing process: {ex.Message}", "Red");
            }

            return allFilesFound;
        }
    }
}
