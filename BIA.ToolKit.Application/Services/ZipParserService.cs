namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class ZipParserService
    {
        private readonly IConsoleWriter consoleWriter;
        private const string TMP_FOLDER_NAME = "BiaToolKit_CRUDGenerator";
        private const string ATTRIBUE_MARKER = "XXXXX";
        public const string ANGULAR_MARKER = "BIAToolKit -";
        private const string ANGULAR_MARKER_BEGIN = ANGULAR_MARKER + " Begin " + ATTRIBUE_MARKER + " block";
        private const string ANGULAR_MARKER_END = ANGULAR_MARKER + " End " + ATTRIBUE_MARKER + " block";

        public ZipParserService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public (string, Dictionary<string, string>) ReadZipAndExtract(string zipPath, string entityName, string compagnyName, string projectName, string folderType)
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

                tempDir = Path.Combine(Path.GetTempPath(), TMP_FOLDER_NAME, folderType);
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

        public List<ExtractBlocks> AnalyzeAngularFile(string fileName, Dictionary<string, List<string>> planeDtoProperties)
        {
            if (!File.Exists(fileName))
            {
                consoleWriter.AddMessageLine($"Error on analysing angular file: file not exist on disk: '{fileName}'", "Orange");
                return null;
            }

            // Read file
            string fileContent = File.ReadAllText(fileName);
            if (!fileContent.Contains(ANGULAR_MARKER))
            {
                return null;
            }

            // File to update
            List<ExtractBlocks> extractBlocks = new();
            List<string> fileLines = File.ReadAllLines(fileName).ToList();
            foreach (KeyValuePair<string, List<string>> dtoProperty in planeDtoProperties)
            {
                foreach (string attribute in dtoProperty.Value)
                {
                    List<string> block = FindBlock(fileLines, attribute);
                    if (block != null && block.Count > 0)
                    {
                        // Add only if block found
                        extractBlocks.Add(new ExtractBlocks(dtoProperty.Key, attribute, block));
                    }
                }
            }

            return extractBlocks;
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

        private List<string> FindBlock(List<string> lines, string attributeName)
        {
            // Convert to camel case
            attributeName = CommonTools.ConvertToCamelCase(attributeName);

            // Set start and stop block
            string markerBegin = ANGULAR_MARKER_BEGIN.Replace(ATTRIBUE_MARKER, attributeName);
            string markerEnd = ANGULAR_MARKER_END.Replace(ATTRIBUE_MARKER, attributeName);

            // Find start and stop block
            int start = lines.FindIndex(l => l.Contains(markerBegin));
            int end = lines.FindIndex(l => l.Contains(markerEnd));

            if (start < 0 || end < 0)
            {
                consoleWriter.AddMessageLine($"Block not correctly found for {attributeName}", "Orange");
                return null;
            }

            // Keep block contains
            return lines.ToArray()[start..++end].ToList();  // array with start and end lines included
        }

        public Dictionary<string, List<string>> GetDtoProperties(List<PropertyDeclarationSyntax> list)
        {
            Dictionary<string, List<string>> dico = new();
            list.ForEach(p =>
            {
                CommonTools.AddToDictionnary(dico, p.Type.ToString(), p.Identifier.ToString());
            });

            return dico;
        }
    }
}
