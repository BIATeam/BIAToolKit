namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class ZipParserService
    {
        private readonly IConsoleWriter consoleWriter;
        private readonly CSharpParserService service;

        private const string BIA_MARKER = "BIAToolKit -";
        public const string MARKER_BEGIN = $"{BIA_MARKER} Begin";
        public const string MARKER_END = $"{BIA_MARKER} End";
        // Tags
        public static readonly string MARKER_BEGIN_PROPERTIES = $"{MARKER_BEGIN} {CRUDDataUpdateType.Properties}";
        public static readonly string MARKER_END_PROPERTIES = $"{MARKER_END} {CRUDDataUpdateType.Properties}";
        public static readonly string MARKER_BEGIN_BLOCK = $"{MARKER_BEGIN} {CRUDDataUpdateType.Block}";
        public static readonly string MARKER_END_BLOCK = $"{MARKER_END} {CRUDDataUpdateType.Block}";
        public static readonly string MARKER_BEGIN_CHILD = $"{MARKER_BEGIN} {CRUDDataUpdateType.Child}";
        public static readonly string MARKER_END_CHILD = $"{MARKER_END} {CRUDDataUpdateType.Child}";
        public static readonly string MARKER_BEGIN_OPTION = $"{MARKER_BEGIN} {CRUDDataUpdateType.Option}";
        public static readonly string MARKER_END_OPTION = $"{MARKER_END} {CRUDDataUpdateType.Option}";
        // Tags Partial
        public const string MARKER_BEGIN_PARTIAL = $"{MARKER_BEGIN} Partial";
        public const string MARKER_END_PARTIAL = $"{MARKER_END} Partial";


        /// <summary>
        /// Constructor.
        /// </summary>
        public ZipParserService(CSharpParserService service, IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
            this.service = service;
        }

        /// <summary>
        /// Unzip archive on temp local folder and analyze files.
        /// </summary>
        public ZipFilesContent ParseZipFile(FeatureTypeData zipData, FeatureType type, string folderName/*, List<string> options*/)
        {
            string fileName = Path.Combine(zipData.ZipPath, zipData.ZipName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                consoleWriter.AddMessageLine($"No {type} Zip files found to parse.", "Orange");
                return null;
            }

            // Unzip archive
            (string workingDirectoryPath, Dictionary<string, string> fileList) = ReadZipAndExtract(fileName, folderName, type);
            if (string.IsNullOrWhiteSpace(workingDirectoryPath))
            {
                consoleWriter.AddMessageLine($"Zip archive '{fileName}' not found.", "Orange");
                return null;
            }

            // Parse files in zip
            if (fileList.Count > 0)
            {
                ZipFilesContent filesContent = new(type);
                foreach (KeyValuePair<string, string> file in fileList)
                {
                    string filePath = Path.Combine(workingDirectoryPath, file.Key);

                    WebApiFileType? fileType = null;
                    if (type == FeatureType.WebApi)     // Specific to WebApi part
                    {
                        fileType = GetFileType(file.Value);

                        // Ignore mapper and entity file
                        if (fileType != null && (fileType == WebApiFileType.Mapper || fileType == WebApiFileType.Entity))
                        {
                            continue;
                        }
                    }

                    FeatureData data;
                    if (type == FeatureType.WebApi)
                    {
                        data = new WebApiFeatureData(file.Value, file.Key, workingDirectoryPath, fileType);
                    }
                    else
                    {
                        data = new FeatureData(file.Value, file.Key, workingDirectoryPath);
                    }

                    if (fileType == WebApiFileType.Dto)   // Specific to WebApi part
                    {
                        // Parse "plane" Dto file
                        ClassDefinition classFile = service.ParseClassFile(Path.Combine(workingDirectoryPath, file.Key));
                        if (classFile != null)
                        {
                            classFile.EntityName = GetEntityName(file.Value, fileType);
                        }
                        ((WebApiFeatureData)data).ClassFileDefinition = classFile;
                    }
                    else
                    {
                        data.ExtractBlocks = AnalyzeFile(filePath, data.IsPartialFile);
                        //if (options != null && options.Count > 0 && !data.IsPartialFile)
                        //{
                        //    data.OptionToDelete = ExtractLinesContainsOptions(filePath, options);
                        //}
                    }

                    filesContent.FeatureDataList.Add(data);
                }

                return filesContent;
            }
            else
            {
                consoleWriter.AddMessageLine($"Zip archive '{fileName}' is empty.", "Orange");
            }

            return null;
        }


        /// <summary>
        /// Read Zip archive and extract files on temporary working directory.
        /// </summary>
        /// <returns>A tuple with working temporary directory and a dictionnary of files contains (key: file full path on archive, value : file name) in zip archives.</returns>
        private (string, Dictionary<string, string>) ReadZipAndExtract(string zipPath, string folderType, FeatureType crudType)
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

                // Create working temporary folder
                tempDir = Path.Combine(Path.GetTempPath(), Constants.FolderCrudGenerationTmp, folderType, crudType.ToString());
                CommonTools.PrepareFolder(tempDir);

                // Extract and list files from archive to temprory folder
                files = new();
                using ZipArchive archive = ZipFile.OpenRead(zipPath);
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.Name))
                    {
                        // if entry is folder, ignore it
                        continue;
                    }

                    CommonTools.CheckFolder(Path.Combine(tempDir, entry.FullName.Replace(entry.Name, "")));
                    entry.ExtractToFile(Path.Combine(tempDir, entry.FullName));
                    files.Add(entry.FullName, entry.Name);
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in zip file parsing process: {ex.Message}", "Red");
            }

            return (tempDir, files);
        }

        /// <summary>
        /// Analyze file and extract blocks.
        /// </summary>
        private List<ExtractBlock> AnalyzeFile(string fileName, bool isPartial)
        {
            if (!File.Exists(fileName))
            {
                consoleWriter.AddMessageLine($"Error on analysing file: file not exists on disk: '{fileName}'", "Orange");
                return null;
            }

            // Read file
            List<string> fileLines = File.ReadAllLines(fileName).ToList();

            // Read file to verify if marker is present
            if (!CommonTools.IsFileContainsData(fileLines, new List<string> { MARKER_BEGIN }))
            {
                return null;
            }

            List<ExtractBlock> extractBlocksList = new();

            if (isPartial)
            {
                extractBlocksList.AddRange(ExtractPartialFile(fileLines));
            }
            else
            {
                foreach (CRUDDataUpdateType type in Enum.GetValues(typeof(CRUDDataUpdateType)))
                {
                    // don't take partial file
                    if (type == CRUDDataUpdateType.Config
                        || type == CRUDDataUpdateType.Dependency
                        || type == CRUDDataUpdateType.Navigation
                        || type == CRUDDataUpdateType.Permission
                        || type == CRUDDataUpdateType.Rights
                        || type == CRUDDataUpdateType.Routing)
                    {
                        continue;
                    }
                    else
                    {
                        extractBlocksList.AddRange(ExtractBlocks(type, fileLines));
                    }
                }

                // Populate Blocks with properties type
                ExtractPropertiesBlock propertiesBlock = (ExtractPropertiesBlock)extractBlocksList.FirstOrDefault(b => b.DataUpdateType == CRUDDataUpdateType.Properties);
                if (propertiesBlock != null)
                {
                    extractBlocksList.Where(b => b.DataUpdateType == CRUDDataUpdateType.Block)?.ToList().ForEach(block =>
                    {
                        foreach (KeyValuePair<string, List<string>> properties in propertiesBlock.PropertiesList)
                        {
                            if (properties.Value.Contains(block.Name))
                            {
                                ((ExtractBlockBlock)block).Type = properties.Key;
                                return;
                            }
                        }
                    });
                }
            }

            return extractBlocksList;
        }

        /// <summary>
        /// Extract lines that contains options.
        /// </summary>
        private List<string> ExtractLinesContainsOptions(string fileName, List<string> options)
        {
            if (options == null || options.Count <= 0) return null;

            if (!File.Exists(fileName))
            {
                consoleWriter.AddMessageLine($"Error on analysing angular file: file not exist on disk: '{fileName}'", "Orange");
                return null;
            }

            // Read file in detail
            List<string> fileLines = File.ReadAllLines(fileName).ToList();

            // Read file to verify if option are present
            if (!CommonTools.IsFileContainsData(fileLines, options))
            {
                return null;
            }

            // Extract lines to delete
            List<string> linesToDelete = new();
            foreach (string option in options)
            {
                IEnumerable<string> linesFound = fileLines.Where(line => line.Contains(option, StringComparison.InvariantCultureIgnoreCase));
                if (linesFound != null && linesFound.Any())
                {
                    linesToDelete.AddRange(linesFound);
                }
            }

            return linesToDelete;
        }

        /// <summary>
        /// Get the type of DotNet file.
        /// </summary>
        private WebApiFileType? GetFileType(string fileName)
        {
            if (fileName == null)
            {
                consoleWriter.AddMessageLine($"File type not found for: '{fileName}'", "Orange");
                return null;
            }
            else if (fileName.EndsWith(".partial"))
            {
                return WebApiFileType.Partial;
            }
            else if (fileName.EndsWith(".cs"))
            {
                if (fileName.EndsWith("AppService.cs"))
                {
                    if (fileName.StartsWith("I"))
                        return WebApiFileType.IAppService;
                    else
                        return WebApiFileType.AppService;
                }
                else if (fileName.EndsWith("Dto.cs"))
                    return WebApiFileType.Dto;
                else if (fileName.EndsWith("Controller.cs"))
                    return WebApiFileType.Controller;
                else if (fileName.EndsWith("Mapper.cs"))
                    return WebApiFileType.Mapper;
                else
                    return WebApiFileType.Entity;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get the "entity" name from DotNet file.
        /// </summary>
        private string GetEntityName(string fileName, WebApiFileType? type)
        {
            if (fileName == null || type == null)
            {
                return null;
            }

            string name = null;
            string pattern = "";
            switch (type)
            {
                case WebApiFileType.AppService:
                case WebApiFileType.IAppService:
                    pattern = @"^I?(\w+)AppService\.cs$";
                    break;
                case WebApiFileType.Dto:
                    pattern = @"^(\w+)Dto\.cs$";
                    break;
                case WebApiFileType.Controller:
                    pattern = @"^((\w+)s|(\w+))Controller\.cs$";
                    break;
                case WebApiFileType.Mapper:
                    pattern = @"^(\w+)Mapper\.cs$";
                    break;
                case WebApiFileType.Entity:
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

        /// <summary>
        /// Find and extract block of lines from file content in function to DataType (Property, block, child).
        /// </summary>
        private List<ExtractBlock> ExtractBlocks(CRUDDataUpdateType type, List<string> lines)
        {
            List<ExtractBlock> extractBlocksList = new();
            string markerBegin = $"{MARKER_BEGIN} {type}";
            string markerEnd = $"{MARKER_END} {type}";

            int nbOccur = lines.Count(l => l.Contains(markerBegin));
            if (nbOccur > 0)
            {
                int indexStart = -1;
                int indexEnd = lines.Count - 1;

                // Get all occurences
                for (int i = 1; i <= nbOccur; i++)
                {
                    // Find start and stop block
                    indexStart = lines.FindIndex(indexStart + 1, lines.Count - indexStart - 1, l => l.Contains(markerBegin));
                    indexEnd = lines.FindIndex(indexStart, lines.Count - indexStart, l => l.Contains(markerEnd));

                    if (indexStart < 0 || indexEnd < 0)
                    {
                        // error
                        throw new Exception($"{type} block not correctly formated, marker begin ({indexStart}) or end({indexEnd}) not found.");
                    }

                    // Array with start and end lines included
                    List<string> blockLines = lines.ToArray()[indexStart..++indexEnd].ToList();

                    // Get block name (if exist)
                    string name = CommonTools.GetMatchRegexValue(@$"({markerBegin})[\s+](\w+)", blockLines[0], 2);

                    // Decompose property
                    if (type == CRUDDataUpdateType.Properties)
                    {
                        ExtractPropertiesBlock propBlock = new(type, name, blockLines);
                        blockLines.ForEach(line =>
                        {
                            (string left, string right) = DecomposeProperty(line);
                            if (!string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right))
                            {
                                CommonTools.AddToDictionnary(propBlock.PropertiesList, right, left); // key: property type, value: property name
                            }
                        });
                        extractBlocksList.Add(propBlock);
                    }
                    else if (type == CRUDDataUpdateType.Block)
                    {
                        extractBlocksList.Add(new ExtractBlockBlock(type, name, blockLines));
                    }
                    else
                    {
                        extractBlocksList.Add(new ExtractBlock(type, name, blockLines));
                    }
                }
            }

            return extractBlocksList;
        }

        /// <summary>
        /// Extract block of lines from partial file.
        /// </summary>
        private List<ExtractBlock> ExtractPartialFile(List<string> fileLines)
        {
            List<ExtractBlock> extractBlocksList = new();
            List<string> lines = new();
            bool startFound = false;
            string typeName = null;

            // Add blocks found between markers
            foreach (string line in fileLines)
            {
                if (line.Contains(MARKER_BEGIN_PARTIAL, StringComparison.InvariantCulture))
                {
                    lines = new();
                    startFound = true;
                    typeName = CommonTools.GetMatchRegexValue(@$"({MARKER_BEGIN_PARTIAL})[\s+](\w+)", line, 2);
                }

                if (startFound)
                    lines.Add(line);

                if (startFound && line.Contains(MARKER_END_PARTIAL, StringComparison.InvariantCulture))
                {
                    startFound = false;
                    extractBlocksList.Add(new ExtractBlock(GetCRUDDataUpdateType(typeName), null, lines));
                }
            }

            return extractBlocksList;
        }

        /// <summary>
        /// Get and format list of properties of Dto file.
        /// </summary>
        public Dictionary<string, List<string>> GetDtoProperties(List<PropertyDeclarationSyntax> propertyList)
        {
            Dictionary<string, List<string>> dico = new();
            propertyList.ForEach(p =>
            {
                CommonTools.AddToDictionnary(dico, p.Type.ToString(), p.Identifier.ToString());
            });

            return dico;
        }

        /// <summary>
        /// Convert "string" type value to "CRUDDataUpdateType" type value.
        /// </summary>
        private CRUDDataUpdateType GetCRUDDataUpdateType(string type)
        {
            if (type != null)
            {
                if (type == CRUDDataUpdateType.Config.ToString())
                    return CRUDDataUpdateType.Config;
                if (type == CRUDDataUpdateType.Dependency.ToString())
                    return CRUDDataUpdateType.Dependency;
                if (type == CRUDDataUpdateType.Navigation.ToString())
                    return CRUDDataUpdateType.Navigation;
                if (type == CRUDDataUpdateType.Permission.ToString())
                    return CRUDDataUpdateType.Permission;
                if (type == CRUDDataUpdateType.Rights.ToString())
                    return CRUDDataUpdateType.Rights;
                if (type == CRUDDataUpdateType.Routing.ToString())
                    return CRUDDataUpdateType.Routing;

                if (type == CRUDDataUpdateType.Block.ToString())
                    return CRUDDataUpdateType.Block;
                if (type == CRUDDataUpdateType.Child.ToString())
                    return CRUDDataUpdateType.Child;
                if (type == CRUDDataUpdateType.Properties.ToString())
                    return CRUDDataUpdateType.Properties;
            }

            throw new Exception($"CRUDDataUpdateType not reconized for '{type}'.");
        }

        /// <summary>
        /// Extract and format property: key = type, value = name.
        /// </summary>
        private (string left, string right) DecomposeProperty(string property)
        {
            if (string.IsNullOrEmpty(property)) { return (null, null); }

            if (property.Trim().StartsWith("//")) { return (null, null); }

            (string left, string right) data = CommonTools.DivideDataFromSeparator(Constants.PropertySeparator, property);
            if (string.IsNullOrWhiteSpace(data.left) || string.IsNullOrWhiteSpace(data.right))
            {
                consoleWriter.AddMessageLine($"Property not correctly formated, not possible to decompose: '{property}'", "Orange");
                return (null, null);
            }

            return data;
        }
    }
}
