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

        public const string ANGULAR_MARKER = "BIAToolKit - ";
        public const string ANGULAR_MARKER_BEGIN_PROPERTIES = ANGULAR_MARKER + "Begin properties";
        public const string ANGULAR_MARKER_END_PROPERTIES = ANGULAR_MARKER + "End properties";
        public const string ANGULAR_MARKER_BEGIN_BLOCK = ANGULAR_MARKER + "Begin block ";
        public const string ANGULAR_MARKER_END_BLOCK = ANGULAR_MARKER + "End block ";

        private const string ATTRIBUE_MARKER = "XXXXX";
        private const string ANGULAR_MARKER_BEGIN = ANGULAR_MARKER_BEGIN_BLOCK + ATTRIBUE_MARKER;
        private const string ANGULAR_MARKER_END = ANGULAR_MARKER_END_BLOCK + ATTRIBUE_MARKER;

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
                    consoleWriter.AddMessageLine($"Zip file path '{zipPath}' not exist", "Red");
                    return (tempDir, files);
                }

#if DEBUG
                consoleWriter.AddMessageLine($"*** Parse zip file: '{zipPath}' ***", "Green");
#endif

                tempDir = Path.Combine(Path.GetTempPath(), Constants.FolderCrudGenerationTmp, folderType);
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

            List<ExtractBlocks> extractBlocksList = new();

            // Read file to update
            List<string> fileLines = File.ReadAllLines(fileName).ToList();

            // Extract properties to update
            List<string> properties = FindBlock(CRUDDataUpdateType.Property, fileLines, null);
            if (properties != null)
            {
                foreach (string prop in properties)
                {
                    ExtractBlocks block = DecomposeProperty(prop);
                    if (block != null)
                    {
                        extractBlocksList.Add(block);
                    }
                }
            }
            else
            {
                consoleWriter.AddMessageLine($"Properties not found on file '{fileName}'.", "Orange");
            }


            // Extract block to update
            foreach (KeyValuePair<string, List<string>> dtoProperty in planeDtoProperties)
            {
                foreach (string attribute in dtoProperty.Value)
                {
                    List<string> block = FindBlock(CRUDDataUpdateType.Block, fileLines, attribute);
                    if (block != null && block.Count > 0)
                    {
                        // Add only if block found
                        extractBlocksList.Add(new ExtractBlocks(CRUDDataUpdateType.Block, dtoProperty.Key, attribute, block));
                    }
                    else
                    {
                        consoleWriter.AddMessageLine($"Block not found for '{attribute}' on file '{fileName}'", "Orange");
                    }
                }
            }

            return extractBlocksList;
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

        private ExtractBlocks DecomposeProperty(string property)
        {
            if (string.IsNullOrEmpty(property)) { return null; }

            if (property.Trim().StartsWith("//")) { return null; }

            MatchCollection matches = new Regex(@"^\s*(\w+):\s(\w+\W*\w*);$").Matches(property);
            if (matches != null && matches.Count > 0)
            {
                GroupCollection groups = matches[0].Groups;
                if (groups.Count > 2)
                {
                    return new ExtractBlocks(CRUDDataUpdateType.Property, groups[2].Value, groups[1].Value, new List<string>() { property });
                }
            }

            consoleWriter.AddMessageLine($"Property not correctly formated, not possible to decompose: '{property}'", "Orange");
            return null;
        }

        private List<string> FindBlock(CRUDDataUpdateType type, List<string> lines, string attributeName)
        {
            // Convert to camel case
            attributeName = CommonTools.ConvertToCamelCase(attributeName);

            string markerBegin;
            string markerEnd;

            // Set start and stop marker
            switch (type)
            {
                case CRUDDataUpdateType.Block:
                    markerBegin = ANGULAR_MARKER_BEGIN.Replace(ATTRIBUE_MARKER, attributeName);
                    markerEnd = ANGULAR_MARKER_END.Replace(ATTRIBUE_MARKER, attributeName);
                    break;
                case CRUDDataUpdateType.Property:
                    markerBegin = ANGULAR_MARKER_BEGIN_PROPERTIES;
                    markerEnd = ANGULAR_MARKER_END_PROPERTIES;
                    break;
                default:
                    throw new Exception($"Error on FindBlock: '{type}' case not implemented.");
            }

            // Find start and stop block
            int indexStart = lines.FindIndex(l => l.Contains(markerBegin));
            int indexEnd = lines.FindIndex(l => l.Contains(markerEnd));

            if (indexStart < 0 || indexEnd < 0)
            {
                return null;
            }

            // Keep block contains
            return lines.ToArray()[indexStart..++indexEnd].ToList();  // array with start and end lines included
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
