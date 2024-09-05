namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
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
        public bool ParseZipFile(ZipFeatureType zipData, string folderName, string dtoCustomAttributeName)
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
                                        ((WebApiFeatureData)featureData).PropertiesInfos = service.GetPlaneDtoPropertyList(classFile.PropertyList, dtoCustomAttributeName);
                                    }
                                }
                            }

                            if (fileType != WebApiFileType.Dto || fileType != WebApiFileType.Entity || fileType != WebApiFileType.Mapper)    // Not Dto, Entity or Mapper
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
                if (zipData.GenerationType == GenerationType.Front && zipData.FeatureType == FeatureType.CRUD)
                {
                    PopulateExtractItemDisplayBlock(zipData.FeatureDataList);
                }
            }
            else
            {
                consoleWriter.AddMessageLine($"Zip archive '{fileName}' is empty.", "Orange");
                return false;
            }

            return true;
        }

        private void PopulateExtractItemDisplayBlock(List<FeatureData> featureDataList)
        {
            List<FeatureData> featureProperties = featureDataList.Where(f =>
            {
                if (f.ExtractBlocks == null) return false;
                return f.ExtractBlocks.Select(b => b.DataUpdateType == CRUDDataUpdateType.Properties).FirstOrDefault();
            }).ToList();

            List<FeatureData> featureDisplay = featureDataList.Where(f =>
            {
                if (f.ExtractBlocks == null) return false;
                return f.ExtractBlocks.Select(b => b.DataUpdateType == CRUDDataUpdateType.Display).FirstOrDefault();
            }).ToList();

            if (featureProperties != null && featureDisplay != null)
            {
                ExtractPropertiesBlock propBlock = (ExtractPropertiesBlock)featureProperties.FirstOrDefault().ExtractBlocks.Where(b => b.DataUpdateType == CRUDDataUpdateType.Properties).FirstOrDefault();

                // Populate 'ExtractItem' of Display block
                foreach (FeatureData display in featureDisplay)
                {
                    foreach (ExtractDisplayBlock displayBlock in display.ExtractBlocks.Where(b => b.DataUpdateType == CRUDDataUpdateType.Display))
                    {
                        displayBlock.ExtractItem = propBlock.PropertiesList.FirstOrDefault().Name;
                    }
                }
            }
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
                            if (displayLines?.Count != 1)
                            {
                                consoleWriter.AddMessageLine($"Incorrect Display block format: '{blockLines}'", "Orange");
                            }
                            extractBlocksList.Add(new ExtractDisplayBlock(type, name, blockLines) { ExtractLine = displayLines[0].TrimStart() });
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
        private List<ExtractBlock> ExtractPartialFile(List<string> fileLines)
        {
            const string regex = @$"(?:{MARKER_BEGIN_PARTIAL})[\s+](\w+)(\s+\d*)?(\s*\w+)";
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
                    typeName = CommonTools.GetMatchRegexValue(regex, line, 1);
                    index = CommonTools.GetMatchRegexValue(regex, line, 2);
                    name = CommonTools.GetMatchRegexValue(regex, line, 3);
                }

                if (startFound)
                    lines.Add(line);

                if (startFound && line.Contains(MARKER_END_PARTIAL, StringComparison.InvariantCulture))
                {
                    startFound = false;
                    extractBlocksList.Add(new ExtractPartialBlock(CommonTools.GetEnumElement<CRUDDataUpdateType>(typeName), name?.TrimStart(), index?.TrimStart(), lines));
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
