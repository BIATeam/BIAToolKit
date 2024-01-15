namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
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

        private const string BIA_MARKER = "BIAToolKit -";
        private const string MARKER_BEGIN = $"{BIA_MARKER} Begin";
        private const string MARKER_END = $"{BIA_MARKER} End";
        // CRUD
        public const string MARKER_BEGIN_PROPERTIES = $"{MARKER_BEGIN} Properties";
        public const string MARKER_END_PROPERTIES = $"{MARKER_END} Properties";
        public const string MARKER_BEGIN_BLOCK = $"{MARKER_BEGIN} Block";
        public const string MARKER_END_BLOCK = $"{MARKER_END} Block";
        public const string MARKER_BEGIN_CHILDREN = $"/* {MARKER_BEGIN} Children */";
        public const string MARKER_END_CHILDREN = $"/* {MARKER_END} Children */";
        // Back
        public const string MARKER_BEGIN_RIGHTS = $"{MARKER_BEGIN} Rights";
        public const string MARKER_END_RIGHTS = $"{MARKER_END} Rights";
        public const string MARKER_BEGIN_DEPENDENCY = $"{MARKER_BEGIN} Dependency";
        public const string MARKER_END_DEPENDENCY = $"{MARKER_END} Dependency";
        public const string MARKER_BEGIN_CONFIG = $"{MARKER_BEGIN} Config";
        public const string MARKER_END_CONFIG = $"{MARKER_END} Config";
        // Angular
        public const string MARKER_BEGIN_NAVIGATION = $"{MARKER_BEGIN} Navigation";
        public const string MARKER_END_NAVIGATION = $"{MARKER_END} Navigation";
        public const string MARKER_BEGIN_PERMISSION = $"{MARKER_BEGIN} Permission";
        public const string MARKER_END_PERMISSION = $"{MARKER_END} Permission";
        public const string MARKER_BEGIN_ROUTING = $"{MARKER_BEGIN} Routing";
        public const string MARKER_END_ROUTING = $"{MARKER_END} Routing";
        // Partial
        public const string MARKER_BEGIN_PARTIAL = $"{MARKER_BEGIN} Partial";
        public const string MARKER_END_PARTIAL = $"{MARKER_END} Partial";

        private const string ATTRIBUE_MARKER = "XXXXX";
        private const string ANGULAR_MARKER_BEGIN_ATTRIBUTE_BLOCK = $"{MARKER_BEGIN_BLOCK} {ATTRIBUE_MARKER}";
        private const string ANGULAR_MARKER_END_ATTRIBUTE_BLOCK = $"{MARKER_END_BLOCK} {ATTRIBUE_MARKER}";

        /// <summary>
        /// Constructor.
        /// </summary>
        public ZipParserService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        /// <summary>
        /// Read Zip archive and extract files on temporary working directory.
        /// </summary>
        /// <returns>A tuple with working temporary directory and a dictionnary of files contains (key: file full path on archive, value : file name) in zip archives.</returns>
        public (string, Dictionary<string, string>) ReadZipAndExtract(string zipPath, string folderType, FeatureType crudType)
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
        /// Analyze partial file and extract blocks.
        /// </summary>
        public List<ExtractBlocks> AnalyzePartialFile(string fileName)
        {
            List<ExtractBlocks> extractBlocksList = new();
            List<string> lines = new();
            bool startFound = false;

            // Read partial file
            List<string> fileLines = File.ReadAllLines(fileName).ToList();

            // Add blocks found between markers
            foreach (string line in fileLines)
            {
                if (line.Contains(MARKER_BEGIN_PARTIAL, StringComparison.InvariantCulture))
                {
                    lines = new();
                    startFound = true;
                }

                if (startFound)
                    lines.Add(line);

                if (line.Contains(MARKER_END_PARTIAL, StringComparison.InvariantCulture))
                {
                    startFound = false;
                    extractBlocksList.Add(new ExtractBlocks(CRUDDataUpdateType.Partial, null, null, lines));
                }
            }

            return extractBlocksList;
        }

        /// <summary>
        /// Analyze angular file and extract blocks.
        /// </summary>
        public List<ExtractBlocks> AnalyzeAngularFile(string fileName, Dictionary<string, List<string>> planeDtoProperties)
        {
            if (!File.Exists(fileName))
            {
                consoleWriter.AddMessageLine($"Error on analysing angular file: file not exist on disk: '{fileName}'", "Orange");
                return null;
            }

            // Read file to update
            List<string> fileLines = File.ReadAllLines(fileName).ToList();

            // Read file to verify if marker is present
            if (!IsFileContains(fileLines, new List<string> { MARKER_BEGIN }))
            {
                return null;
            }

            // Extract properties to update
            List<ExtractBlocks> extractBlocksList = new();
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

            // Extract block to update
            foreach (KeyValuePair<string, List<string>> dtoProperty in planeDtoProperties)
            {
                foreach (string attribute in dtoProperty.Value)
                {
                    List<string> block = FindBlock(CRUDDataUpdateType.Block, fileLines, attribute);
                    if (block != null && block.Count > 0)
                    {
                        // Add only if block found
                        extractBlocksList.Add(new ExtractBlocks(CRUDDataUpdateType.Block, dtoProperty.Key.TrimEnd('?'), attribute, block));
                    }
                }
            }

            return extractBlocksList;
        }

        /// <summary>
        /// Extract lines that contains options.
        /// </summary>
        public List<string> ExtractLinesContainsOptions(string fileName, List<string> options)
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
            if (!IsFileContains(fileLines, options))
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
        public BackFileType? GetFileType(string fileName)
        {
            if (fileName == null)
            {
                consoleWriter.AddMessageLine($"File type not found for: '{fileName}'", "Orange");
                return null;
            }
            else if (fileName.EndsWith(".partial"))
            {
                if (fileName.ToLower().StartsWith("bianetconfig.json"))
                    return BackFileType.Config;
                else if (fileName.ToLower().StartsWith("ioccontainer.cs"))
                    return BackFileType.Dependency;
                else if (fileName.ToLower().StartsWith("rights.cs"))
                    return BackFileType.Rights;
                else return null;
            }
            else if (fileName.EndsWith(".cs"))
            {
                if (fileName.EndsWith("AppService.cs"))
                {
                    if (fileName.StartsWith("I"))
                        return BackFileType.IAppService;
                    else
                        return BackFileType.AppService;
                }
                else if (fileName.EndsWith("Dto.cs"))
                    return BackFileType.Dto;
                else if (fileName.EndsWith("Controller.cs"))
                    return BackFileType.Controller;
                else if (fileName.EndsWith("Mapper.cs"))
                    return BackFileType.Mapper;
                else
                    return BackFileType.Entity;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get the "entity" name from DotNet file.
        /// </summary>
        public string GetEntityName(string fileName, BackFileType? type)
        {
            if (fileName == null || type == null)
            {
                return null;
            }

            string name = null;
            string pattern = "";
            switch (type)
            {
                case BackFileType.AppService:
                case BackFileType.IAppService:
                    pattern = @"^I?(\w+)AppService\.cs$";
                    break;
                case BackFileType.Dto:
                    pattern = @"^(\w+)Dto\.cs$";
                    break;
                case BackFileType.Controller:
                    pattern = @"^((\w+)s|(\w+))Controller\.cs$";
                    break;
                case BackFileType.Mapper:
                    pattern = @"^(\w+)Mapper\.cs$";
                    break;
                case BackFileType.Entity:
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
        /// Extract and format property.
        /// </summary>
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

        /// <summary>
        /// Find and extract block of lines from file content in function to DataType (Property or block).
        /// </summary>
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
                    markerBegin = ANGULAR_MARKER_BEGIN_ATTRIBUTE_BLOCK.Replace(ATTRIBUE_MARKER, attributeName);
                    markerEnd = ANGULAR_MARKER_END_ATTRIBUTE_BLOCK.Replace(ATTRIBUE_MARKER, attributeName);
                    break;
                case CRUDDataUpdateType.Property:
                    markerBegin = MARKER_BEGIN_PROPERTIES;
                    markerEnd = MARKER_END_PROPERTIES;
                    break;
                default:
                    throw new Exception($"Error on FindBlock: case {type}' not implemented.");
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

        /// <summary>
        /// Verify if file contains occurence of datas.
        /// </summary>
        private bool IsFileContains(List<string> fileLines, List<string> dataList)
        {
            foreach (string data in dataList)
            {
                if (fileLines.Where(line => line.Contains(data)).Any())
                {
                    return true;
                }
            }

            return false;
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
    }
}
