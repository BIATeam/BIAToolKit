﻿namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
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
        // Tags Partial
        public const string MARKER_BEGIN_PARTIAL = $"{MARKER_BEGIN} Partial";
        public const string MARKER_END_PARTIAL = $"{MARKER_END} Partial";
        // Tags Nested
        public const string MARKER_BEGIN_NESTED = $"{MARKER_BEGIN} Nested";
        public const string MARKER_END_NESTED = $"{MARKER_END} Nested";

        /// <summary>
        /// Constructor.
        /// </summary>
        public ZipParserService(CSharpParserService service, IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
            this.service = service;
        }

        /// <summary>
        /// Parse all zips.
        /// </summary>
        public bool ParseZips(IEnumerable<ZipFeatureType> zipFeatures, Project project, string biaFront, CRUDSettings settings)
        {
            var parsed = false;
            foreach (var zipFeatureType in zipFeatures)
            {
                parsed |= ParseZipFile(zipFeatureType, project, biaFront, settings);
            }

            if(!parsed)
                CleanBiaFolders(zipFeatures, project, biaFront);

            return parsed;
        }

        /// <summary>
        /// Parse Zip files (WebApi, CRUD, option or team).
        /// </summary>
        private bool ParseZipFile(ZipFeatureType zipData, Project project, string biaFront, CRUDSettings settings)
        {
            try
            {
                string folderName = (zipData.GenerationType == GenerationType.WebApi) ? Constants.FolderDotNet : biaFront;
                string biaFolder = Path.Combine(project.Folder, folderName, Constants.FolderBia);
                if (!new DirectoryInfo(biaFolder).Exists)
                {
                    return false;
                }

                return ParseZipFile(zipData, biaFolder, settings.DtoCustomAttributeFieldName);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on parsing '{zipData.FeatureType}' Zip File: {ex.Message}", "Red");
            }
            return false;
        }

        /// <summary>
        /// Unzip archive on temp local folder and analyze files.
        /// </summary>
        private bool ParseZipFile(ZipFeatureType zipData, string folderName, string dtoCustomAttributeName)
        {
            if (string.IsNullOrWhiteSpace(zipData.ZipName))
            {
                return false;
            }

            string fileName = Path.Combine(zipData.ZipPath, zipData.ZipName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                consoleWriter.AddMessageLine($"No '{zipData.FeatureType}' zip files found to parse.", "Orange");
                return false;
            }

            // Unzip archive
            (string workingDirectoryPath, Dictionary<string, string> fileList) = ReadZipAndExtract(fileName, folderName, zipData.ZipName);
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

                    if (zipData.GenerationType == GenerationType.WebApi)
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
                                    classFile.EntityName = GetEntityName(file.Value, fileType);
                                    ((WebApiFeatureData)featureData).Namespace = classFile.NamespaceSyntax.Name.ToString();
                                    ((WebApiFeatureData)featureData).ClassFileDefinition = classFile;
                                    if (fileType == WebApiFileType.Dto)
                                    {
                                        ((WebApiFeatureData)featureData).PropertiesInfos = service.GetPropertyList(classFile.PropertyList, dtoCustomAttributeName);
                                    }
                                }
                            }

                            if (
                                fileType != WebApiFileType.Dto ||
                                fileType != WebApiFileType.Entity ||
                                fileType != WebApiFileType.Mapper)    // Not Dto, Entity or Mapper
                            {
                                featureData.ExtractBlocks = AnalyzeFile(filePath, featureData);
                            }
                        }
                    }
                    else    // Not WebApi
                    {
                        featureData = new FeatureData(file.Value, file.Key, workingDirectoryPath);
                        featureData.ExtractBlocks = AnalyzeFile(filePath, featureData);
                        featureData.IsPropertyFile = featureData.ExtractBlocks?.Find(e => e.DataUpdateType == CRUDDataUpdateType.Properties) != null;
                    }

                    zipData.FeatureDataList.Add(featureData);
                }

                // Populate 'ExtractItem' of Display block
                PopulateNullExtractItemDisplayBlock(zipData.FeatureDataList);
            }
            else
            {
                consoleWriter.AddMessageLine($"Zip archive '{fileName}' is empty.", "Orange");
                return false;
            }

            return true;
        }

        public void CleanBiaFolders(IEnumerable<ZipFeatureType> zipFeatures, Project project, string biaFront)
        {
            foreach (var zipFeatureType in zipFeatures)
            {
                string folderName = (zipFeatureType.GenerationType == GenerationType.WebApi) ? Constants.FolderDotNet : biaFront;
                string biaFolder = Path.Combine(project.Folder, folderName, Constants.FolderBia);

                foreach (var item in Directory.EnumerateFileSystemEntries(biaFolder))
                {
                    var isRootItem = Path.GetDirectoryName(item) == biaFolder;
                    if (isRootItem && Path.GetExtension(item).Equals(".zip", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    if (Directory.Exists(item))
                    {
                        Directory.Delete(item, true);
                        continue;
                    }

                    File.Delete(item);
                }
            }
        }

        /// <summary>
        /// Assign to null extract item display elements of display blocks the first property name of properties blocks if exists
        /// </summary>
        /// <param name="featureDataList"></param>
        private static void PopulateNullExtractItemDisplayBlock(List<FeatureData> featureDataList)
        {
            var featureProperties = featureDataList
                .Where(x => x.ExtractBlocks != null)
                .SelectMany(x => x.ExtractBlocks)
                .Where(x => x.DataUpdateType == CRUDDataUpdateType.Properties)
                .Cast<ExtractPropertiesBlock>()
                .SelectMany(x => x.PropertiesList)
                .ToList();

            foreach (var featureData in featureDataList.Where(fd => fd.ExtractBlocks != null))
            {
                var displayBlocks = featureData.ExtractBlocks
                    .Where(eb => eb.DataUpdateType == CRUDDataUpdateType.Display)
                    .Cast<ExtractDisplayBlock>()
                    .ToList();

                if (featureProperties.Count != 0 && displayBlocks.Count != 0)
                {
                    foreach(var displayBlock in displayBlocks.Where(db => db.ExtractItem == null))
                    {
                        displayBlock.ExtractItem = featureProperties.FirstOrDefault().Name;
                    }
                }
            }
        }

        /// <summary>
        /// Read Zip archive and extract files on temporary working directory.
        /// </summary>
        /// <returns>A tuple with working temporary directory and a dictionary of files contains (key: file full path on archive, value : file name) in zip archives.</returns>
        private (string, Dictionary<string, string>) ReadZipAndExtract(string zipPath, string folderType, string zipName)
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
                tempDir = Path.Combine(Path.GetTempPath(), Constants.FolderCrudGenerationTmp, folderType, Path.GetFileNameWithoutExtension(zipName));
                CommonTools.CheckFolder(tempDir);

                // Extract and list files from archive to temporary folder
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
                    entry.ExtractToFile(Path.Combine(tempDir, entry.FullName), true);
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
        private List<ExtractBlock> AnalyzeFile(string fileName, FeatureData featureData)
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

            if (featureData.IsPartialFile)
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
            }

            return extractBlocksList;
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
                else if (fileName.EndsWith("OptionMapper.cs"))
                    return WebApiFileType.OptionMapper;
                else if (fileName.EndsWith("Mapper.cs"))
                    return WebApiFileType.Mapper;
                else if (fileName.EndsWith("Specification.cs"))
                    return WebApiFileType.Specification;
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
                case WebApiFileType.OptionAppService:
                case WebApiFileType.IOptionAppService:
                    pattern = @"^I?(\w+)OptionAppService\.cs$";
                    break;
                case WebApiFileType.Dto:
                    pattern = @"^(\w+)Dto\.cs$";
                    break;
                case WebApiFileType.Controller:
                    pattern = @"^((\w+)s|(\w+))Controller\.cs$";
                    break;
                case WebApiFileType.OptionsController:
                    pattern = @"^((\w+)s|(\w+))OptionsController\.cs$";
                    break;
                case WebApiFileType.Mapper:
                    pattern = @"^(\w+)Mapper\.cs$";
                    break;
                case WebApiFileType.OptionMapper:
                    pattern = @"^(\w+)OptionMapper\.cs$";
                    break;
                case WebApiFileType.Entity:
                    pattern = @"^(\w+)\.cs$";
                    break;
                case WebApiFileType.Specification:
                    pattern = @"^(\w+)Specification\.cs$";
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
            string regex = @$"{markerBegin}\s+(\w+)\s*(\w+)*";
            string regexBegin = @$"{markerBegin}(?![A-Za-z]+)";
            string regexEnd = @$"{markerEnd}(?![A-Za-z]+)";

            int nbOccur = lines.Count(l => CommonTools.IsMatchRegexValue(regexBegin, l));
            if (nbOccur > 0)
            {
                int indexStart = -1;
                int indexEnd = lines.Count - 1;

                // Get all occurences
                for (int i = 1; i <= nbOccur; i++)
                {
                    // Find start and stop block
                    indexStart = lines.FindIndex(indexStart + 1, lines.Count - indexStart - 1, l => CommonTools.IsMatchRegexValue(regexBegin, l));
                    indexEnd = lines.FindIndex(indexStart, lines.Count - indexStart, l => CommonTools.IsMatchRegexValue(regexEnd, l));

                    if (indexStart < 0 || indexEnd < 0)
                    {
                        // error
                        throw new Exception($"{type} block not correctly formated, marker begin ({indexStart}) or end({indexEnd}) not found.");
                    }

                    // Array with start and end lines included
                    List<string> blockLines = lines.ToArray()[indexStart..++indexEnd].ToList();

                    // Get block name (if exist)
                    string name = CommonTools.GetMatchRegexValue(regex, blockLines[0], 1);

                    switch (type)
                    {
                        case CRUDDataUpdateType.Properties:
                            // Decompose property
                            List<CRUDPropertyType> propertyList = new();
                            blockLines.ForEach(line =>
                            {
                                (string propName, string propType) = DecomposeProperty(line);
                                if (!string.IsNullOrWhiteSpace(propName) && !string.IsNullOrWhiteSpace(propType))
                                {
                                    propertyList.Add(new CRUDPropertyType(propName, propType));
                                }
                            });
                            extractBlocksList.Add(new ExtractPropertiesBlock(type, name, blockLines) { PropertiesList = propertyList });
                            break;
                        case CRUDDataUpdateType.Display:
                            List<string> displayLines = blockLines.Where(l => !l.TrimStart().StartsWith("//")).ToList();
                            if (displayLines.Count < 1 && displayLines.Count > 2)
                            {
                                consoleWriter.AddMessageLine($"Incorrect Display block format: '{blockLines}'", "Orange");
                            }
                            var extractLine = displayLines[0].Trim();
                            var extractItem = blockLines[0].Split(' ').Last();
                            if(extractItem == CRUDDataUpdateType.Display.ToString())
                            {
                                extractItem = null;
                            }
                            extractBlocksList.Add(new ExtractDisplayBlock(type, name, blockLines) { ExtractLine = extractLine, ExtractItem = extractItem });
                            break;
                        case CRUDDataUpdateType.OptionField:
                            string fieldName = CommonTools.GetMatchRegexValue(regex, blockLines[0], 2);
                            extractBlocksList.Add(new ExtractOptionFieldBlock(type, name, fieldName, blockLines));
                            break;
                        default:
                            extractBlocksList.Add(new ExtractBlock(type, name, blockLines));
                            break;
                    }
                }
            }

            return extractBlocksList;
        }

        /// <summary>
        /// Extract block of lines from partial file.
        /// </summary>
        private static List<ExtractBlock> ExtractPartialFile(List<string> fileLines)
        {
            const string regexPartial = @$"(?:{MARKER_BEGIN_PARTIAL})[\s+](\w+)(\s+\d*)?(\s*\w+)";
            const string regexNested = @$"(?:{MARKER_BEGIN_NESTED})[\s+](\w+)(\s+\d*)?(\s*\w+)";
            List<ExtractBlock> extractBlocksList = new();
            ExtractPartialBlock currentExtractPartialBlock = null;
            // Add blocks found between markers
            foreach (string line in fileLines)
            {
                if (line.Contains(MARKER_BEGIN_PARTIAL, StringComparison.InvariantCulture))
                {
                    var typeName = CommonTools.GetMatchRegexValue(regexPartial, line, 1);
                    var index = CommonTools.GetMatchRegexValue(regexPartial, line, 2);
                    var name = CommonTools.GetMatchRegexValue(regexPartial, line, 3);

                    currentExtractPartialBlock = new ExtractPartialBlock(CommonTools.GetEnumElement<CRUDDataUpdateType>(typeName), name?.TrimStart(), index?.TrimStart());
                }

                if (line.Contains(MARKER_BEGIN_NESTED, StringComparison.InvariantCulture))
                {
                    var typeName = CommonTools.GetMatchRegexValue(regexNested, line, 1);
                    var index = CommonTools.GetMatchRegexValue(regexNested, line, 2);
                    var name = CommonTools.GetMatchRegexValue(regexNested, line, 3);

                    currentExtractPartialBlock.AddNestedBlock(new ExtractPartialBlock(CommonTools.GetEnumElement<CRUDDataUpdateType>(typeName), name?.TrimStart(), index?.TrimStart()));
                    currentExtractPartialBlock = currentExtractPartialBlock.GetLastNestedBlock();
                }

                currentExtractPartialBlock?.AddLine(line);

                if (line.Contains(MARKER_END_NESTED, StringComparison.InvariantCulture))
                {
                    currentExtractPartialBlock = currentExtractPartialBlock.ParentBlock;
                }

                if (line.Contains(MARKER_END_PARTIAL, StringComparison.InvariantCulture))
                {
                    extractBlocksList.Add(currentExtractPartialBlock);
                }
            }

            return extractBlocksList;
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

        ///// <summary>
        ///// Extract type found on block lines.
        ///// </summary>
        //private string ExtractBlockType(List<string> blockLines)
        //{
        //    const string regex = @"(?:\w*):\s*(?:\w*).(\w*)";
        //    string type = null;
        //    foreach (string line in blockLines)
        //    {
        //        type = CommonTools.GetMatchRegexValue(regex, line);
        //        if (!string.IsNullOrEmpty(type))
        //        {
        //            return type;
        //        }
        //    }
        //    return type;
        //}
    }
}
