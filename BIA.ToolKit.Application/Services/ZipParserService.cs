namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
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
        // Tags Front
        public const string MARKER_BEGIN_FRONT = $"{MARKER_BEGIN} Front";
        public const string MARKER_END_FRONT = $"{MARKER_END} Front";

        public const string MARKER_BEGIN_DISPLAY = $"{MARKER_BEGIN} Display";
        public const string MARKER_END_DISPLAY = $"{MARKER_END} Display";

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
        public bool ParseZipFile(ZipFeatureType zipData, string folderName)
        {
            string fileName = Path.Combine(zipData.ZipPath, zipData.ZipName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                consoleWriter.AddMessageLine($"No '{zipData.FeatureType}' zip files found to parse.", "Orange");
                return false;
            }

            // Unzip archive
            (string workingDirectoryPath, Dictionary<string, string> fileList) = ReadZipAndExtract(fileName, folderName, zipData.FeatureType);
            if (string.IsNullOrWhiteSpace(workingDirectoryPath))
            {
                consoleWriter.AddMessageLine($"Zip archive '{fileName}' not found.", "Orange");
                return false;
            }

            // Parse files in zip
            if (fileList.Count > 0)
            {
                FeatureData featureData;
                zipData.FeatureDataList = new();
                foreach (KeyValuePair<string, string> file in fileList)
                {
                    string filePath = Path.Combine(workingDirectoryPath, file.Key);

                    if (zipData.FeatureType == FeatureType.WebApi)
                    {
                        WebApiFileType? fileType = GetFileType(file.Value);
                        featureData = new WebApiFeatureData(file.Value, file.Key, workingDirectoryPath, fileType);

                        if (fileType != null)
                        {
                            if (fileType != WebApiFileType.Partial)
                            {
                                ClassDefinition classFile = service.ParseClassFile(Path.Combine(workingDirectoryPath, file.Key));
                                if (classFile != null)
                                {
                                    ((WebApiFeatureData)featureData).Namespace = classFile.NamespaceSyntax.Name.ToString();
                                    if (fileType == WebApiFileType.Dto)
                                    {
                                        classFile.EntityName = GetEntityName(file.Value, fileType);
                                        ((WebApiFeatureData)featureData).ClassFileDefinition = classFile;
                                    }
                                }
                            }

                            if (fileType != WebApiFileType.Dto || fileType != WebApiFileType.Entity || fileType != WebApiFileType.Mapper)    // Not Dto, Entity or Mapper
                            {
                                featureData.ExtractBlocks = AnalyzeFile(filePath, featureData.IsPartialFile);
                            }
                        }
                    }
                    else    // Not WebApi
                    {
                        featureData = new FeatureData(file.Value, file.Key, workingDirectoryPath);
                        featureData.ExtractBlocks = AnalyzeFile(filePath, featureData.IsPartialFile);
                    }

                    zipData.FeatureDataList.Add(featureData);
                }
            }
            else
            {
                consoleWriter.AddMessageLine($"Zip archive '{fileName}' is empty.", "Orange");
                return false;
            }

            return true;
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

                consoleWriter.AddMessageLine($"*** Parse zip file: '{zipPath}' ***", "Green");

                // Create working temporary folder
                tempDir = Path.Combine(Path.GetTempPath(), Constants.FolderCrudGenerationTmp, folderType, crudType.ToString());
                CommonTools.CheckFolder(tempDir, true);

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
                                ((ExtractBlocksBlock)block).PropertyType = new CRUDPropertyType(properties.Key);
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
                        extractBlocksList.Add(new ExtractBlocksBlock(type, name, blockLines));
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
            const string regex = @$"({MARKER_BEGIN_PARTIAL})[\s+](\w+)(\s+\d*)?(\s*\w+)";
            List<ExtractBlock> extractBlocksList = new();
            List<string> lines = new();
            bool startFound = false;
            string index = null, name = null, typeName = null;

            // Add blocks found between markers
            foreach (string line in fileLines)
            {
                if (line.Contains(MARKER_BEGIN_PARTIAL, StringComparison.InvariantCulture))
                {
                    lines = new();
                    startFound = true;
                    typeName = CommonTools.GetMatchRegexValue(regex, line, 2);
                    index = CommonTools.GetMatchRegexValue(regex, line, 3);
                    name = CommonTools.GetMatchRegexValue(regex, line, 4);
                }

                if (startFound)
                    lines.Add(line);

                if (startFound && line.Contains(MARKER_END_PARTIAL, StringComparison.InvariantCulture))
                {
                    startFound = false;
                    extractBlocksList.Add(new ExtractPartialBlock(GetCRUDDataUpdateType(typeName), name?.TrimStart(), index?.TrimStart(), lines));
                }
            }

            return extractBlocksList;
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
