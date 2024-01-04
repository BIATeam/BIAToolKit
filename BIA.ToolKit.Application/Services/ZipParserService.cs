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

        public const string BIA_MARKER = "BIAToolKit -";
        public const string ANGULAR_MARKER_BEGIN_PROPERTIES = $"{BIA_MARKER} Begin properties";
        public const string ANGULAR_MARKER_END_PROPERTIES = $"{BIA_MARKER} End properties";
        public const string ANGULAR_MARKER_BEGIN_BLOCK = $"{BIA_MARKER} Begin block";
        public const string ANGULAR_MARKER_END_BLOCK = $"{BIA_MARKER} End block";
        public const string ANGULAR_MARKER_BEGIN_CHILDREN = $"/* {BIA_MARKER} Begin Children */";
        public const string ANGULAR_MARKER_END_CHILDREN = $"/* {BIA_MARKER} End Children */";

        public const string DOTNET_MARKER_BEGIN_RIGHTS = $"{BIA_MARKER} Begin rights";
        public const string DOTNET_MARKER_END_RIGHTS = $"{BIA_MARKER} End rights";
        public const string DOTNET_MARKER_BEGIN_DEPENDENCY = $"{BIA_MARKER} Begin dependency";
        public const string DOTNET_MARKER_END_DEPENDENCY = $"{BIA_MARKER} End dependency";
        public const string DOTNET_MARKER_BEGIN_CONFIG = $"{BIA_MARKER} Begin config";
        public const string DOTNET_MARKER_END_CONFIG = $"{BIA_MARKER} End config";

        private const string ATTRIBUE_MARKER = "XXXXX";
        private const string ANGULAR_MARKER_BEGIN_ATTRIBUTE_BLOCK = $"{ANGULAR_MARKER_BEGIN_BLOCK} {ATTRIBUE_MARKER}";
        private const string ANGULAR_MARKER_END_ATTRIBUTE_BLOCK = $"{ANGULAR_MARKER_END_BLOCK} {ATTRIBUE_MARKER}";

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
        public (string, Dictionary<string, string>) ReadZipAndExtract(string zipPath, string compagnyName, string projectName, string folderType, FeatureType crudType)
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
        /// Analyze and extract angular file.
        /// </summary>
        public List<ExtractBlocks> AnalyzeAngularFile(string fileName, Dictionary<string, List<string>> planeDtoProperties)
        {
            if (!File.Exists(fileName))
            {
                consoleWriter.AddMessageLine($"Error on analysing angular file: file not exist on disk: '{fileName}'", "Orange");
                return null;
            }

            // Read file to verify if marker is present
            if (!IsFileContains(fileName, new List<string> { BIA_MARKER }))
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

            //// Extract children to delete
            //List<ExtractBlocks> blocks = ExtractChildrenToDelete(fileName, fileLines);
            //if (blocks != null && blocks.Count > 0)
            //{
            //    // Add only if block found
            //    extractBlocksList.AddRange(blocks);
            //}

            return extractBlocksList;
        }

        ///// <summary>
        ///// Extract block of lines between Children markers.
        ///// </summary>
        //public List<ExtractBlocks> ExtractChildrenToDelete(string fileName, List<string> fileLines)
        //{
        //    // Verify if marker is presentin file
        //    if (!IsFileContains(fileName, new List<string> { ANGULAR_MARKER_BEGIN_CHILDREN }))
        //    {
        //        return null;
        //    }

        //    // Find marker to extract lines
        //    List<ExtractBlocks> childrenToDelete = new();
        //    for (int i = 0; i < fileLines.Count; i++)
        //    {
        //        if (fileLines[i].Contains(ANGULAR_MARKER_BEGIN_CHILDREN, StringComparison.InvariantCultureIgnoreCase))
        //        {
        //            bool endFound = false;

        //            List<string> lines = new();
        //            for (int j = i; j < fileLines.Count && !endFound; j++)
        //            {
        //                string line = fileLines[j];
        //                lines.Add(line);
        //                if (line.Contains(ANGULAR_MARKER_END_CHILDREN, StringComparison.InvariantCultureIgnoreCase))
        //                {
        //                    endFound = true;
        //                    i = j;
        //                }
        //            }

        //            childrenToDelete.Add(new ExtractBlocks(CRUDDataUpdateType.Children, null, null, lines));
        //        }
        //    }

        //    return childrenToDelete;
        //}

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

            // Read file to verify if option are present
            if (!IsFileContains(fileName, options))
            {
                return null;
            }

            List<string> linesToDelete = new();

            // Read file in detail
            List<string> fileLines = File.ReadAllLines(fileName).ToList();

            // Extract lines to delete
            foreach (string line in fileLines)
            {
                foreach (string option in options)
                {
                    if (line.Contains(option, StringComparison.InvariantCultureIgnoreCase))
                    {
                        linesToDelete.Add(line);
                        continue;
                    }
                }
            }

            return linesToDelete;
        }

        /// <summary>
        /// Get the type of DotNet file.
        /// </summary>
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

        /// <summary>
        /// Get the "entity" name from DotNet file.
        /// </summary>
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
                    markerBegin = ANGULAR_MARKER_BEGIN_PROPERTIES;
                    markerEnd = ANGULAR_MARKER_END_PROPERTIES;
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
        private bool IsFileContains(string fileName, List<string> dataList)
        {
            // Read file
            string fileContent = File.ReadAllText(fileName);

            foreach (string data in dataList)
            {
                if (fileContent.Contains(data))
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
