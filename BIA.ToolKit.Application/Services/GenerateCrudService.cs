namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class GenerateCrudService
    {
        internal const string BIA_DTO_FIELD_TYPE = "Type";
        internal const string BIA_DTO_FIELD_REQUIRED = "Required";
        internal const string BIA_DTO_FIELD_ISPARENT = "IsParent";

        private const string ATTRIBUTE_TYPE_NOT_MANAGED = $"// CRUD GENERATOR FOR PLANE TO REVIEW : Field {ATTRIBUTE_TYPE_NOT_MANAGED_FIELD} or type {ATTRIBUTE_TYPE_NOT_MANAGED_TYPE} not managed.";
        private const string ATTRIBUTE_TYPE_NOT_MANAGED_FIELD = "XXXFieldXXX";
        private const string ATTRIBUTE_TYPE_NOT_MANAGED_TYPE = "YYYTypeYYY";
        private const string IS_REQUIRED_PROPERTY = "isRequired:";

        private readonly IConsoleWriter consoleWriter;

        private string NewCrudNamePascalSingular = "Plane";
        private string NewCrudNamePascalPlural = "Planes";
        private string NewCrudNameCamelSingular = "plane";
        private string NewCrudNameCamelPlural = "planes";
        private string NewCrudNameKebabSingular;
        private string NewCrudNameKebabPlural;

        private string OldCrudNamePascalSingular = "Plane";
        private string OldCrudNamePascalPlural = "Planes";
        private string OldCrudNameCamelSingular = "plane";
        private string OldCrudNameCamelPlural = "planes";

        private string OldOptionNamePascalSingular = "Airport";
        private string OldOptionNamePascalPlural = "Airports";
        private string OldOptionNameCamelSingular = "airport";
        private string OldOptionNameCamelPlural = "airports";

        private string OldTeamNamePascalSingular = "AircraftMaintenanceCompany";
        private string OldTeamNamePascalPlural = "AircraftMaintenanceCompanies";
        private string OldTeamNameCamelSingular = "aircraftMaintenanceCompany";
        private string OldTeamNameCamelPlural = "aircraftMaintenanceCompanies";

        /// <summary>
        /// Constructor.
        /// </summary>
        public GenerateCrudService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void InitRenameValues(string newValueSingular, string newValuePlurial,
                                    string crudNameSingular, string crudNamePlurial,
                                    string optionNameSingular, string optionNamePlurial,
                                    string teamNameSingular, string teamNamePlurial)
        {
            this.NewCrudNamePascalSingular = newValueSingular;
            this.NewCrudNamePascalPlural = newValuePlurial;
            this.NewCrudNameCamelSingular = CommonTools.ConvertToCamelCase(NewCrudNamePascalSingular);
            this.NewCrudNameCamelPlural = CommonTools.ConvertToCamelCase(NewCrudNamePascalPlural);
            this.NewCrudNameKebabSingular = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalSingular);
            this.NewCrudNameKebabPlural = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalPlural);

            // Get Pascal case value
            this.OldCrudNamePascalSingular = string.IsNullOrWhiteSpace(crudNameSingular) ? this.OldCrudNamePascalSingular : crudNameSingular;
            this.OldCrudNamePascalPlural = string.IsNullOrWhiteSpace(crudNamePlurial) ? this.OldCrudNamePascalPlural : crudNamePlurial;
            this.OldOptionNamePascalSingular = string.IsNullOrWhiteSpace(optionNameSingular) ? this.OldOptionNamePascalSingular : optionNameSingular;
            this.OldOptionNamePascalPlural = string.IsNullOrWhiteSpace(optionNamePlurial) ? this.OldOptionNamePascalPlural : optionNamePlurial;
            this.OldTeamNamePascalSingular = string.IsNullOrWhiteSpace(teamNameSingular) ? this.OldTeamNamePascalSingular : teamNameSingular;
            this.OldTeamNamePascalPlural = string.IsNullOrWhiteSpace(teamNamePlurial) ? this.OldTeamNamePascalPlural : teamNamePlurial;

            // Convert value to Camel case
            this.OldCrudNameCamelSingular = CommonTools.ConvertToCamelCase(OldCrudNamePascalSingular);
            this.OldCrudNameCamelPlural = CommonTools.ConvertToCamelCase(OldCrudNamePascalPlural);
            this.OldOptionNameCamelSingular = CommonTools.ConvertToCamelCase(OldOptionNamePascalSingular);
            this.OldOptionNameCamelPlural = CommonTools.ConvertToCamelCase(OldOptionNamePascalPlural);
            this.OldTeamNameCamelSingular = CommonTools.ConvertToCamelCase(OldTeamNamePascalSingular);
            this.OldTeamNameCamelPlural = CommonTools.ConvertToCamelCase(OldTeamNamePascalPlural);
        }

        public string GenerateCrudFiles(Project currentProject, EntityInfo crudDtoEntity, List<ZipFeatureType> zipFeatureTypeList, string displayItem, bool generateInProjectFolder = true)
        {
            string generationFolder = null;
            try
            {
                // Get generation folders
                generationFolder = GetGenerationFolder(currentProject, generateInProjectFolder);
                string dotnetDir = Path.Combine(generationFolder, Constants.FolderDotNet);
                string angularDir = Path.Combine(generationFolder, currentProject.BIAFronts);

                // Get CRUD dto properties
                List<CrudProperty> crudDtoProperties = GetDtoProperties(crudDtoEntity);

                // Generate WebApi DotNet files
                ZipFeatureType backFeatureType = zipFeatureTypeList.Where(x => x.FeatureType == FeatureType.WebApi).FirstOrDefault();
                if (backFeatureType != null && backFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate DotNet files on '{dotnetDir}' ***", "Green");

                    GenerateWebApi(dotnetDir, backFeatureType.FeatureDataList, currentProject, crudDtoProperties, displayItem);
                }

                // Generate CRUD angular files
                ZipFeatureType crudFeatureType = zipFeatureTypeList.Where(x => x.FeatureType == FeatureType.CRUD).FirstOrDefault();
                if (crudFeatureType != null && crudFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate Angular CRUD files on '{angularDir}' ***", "Green");

                    if (crudDtoProperties == null)
                    {
                        consoleWriter.AddMessageLine($"Can't generate Angular CRUD files: Dto properties are empty.", "Red");
                    }
                    else
                    {
                        WebApiFeatureData dtoRefFeature = (WebApiFeatureData)backFeatureType?.FeatureDataList?.FirstOrDefault(f => ((WebApiFeatureData)f).FileType == WebApiFileType.Dto);
                        GenerateCRUD(angularDir, crudFeatureType.FeatureDataList, currentProject, crudDtoProperties, displayItem, dtoRefFeature?.PropertiesInfos);
                    }
                }

                // Generate Option angular files
                ZipFeatureType optionFeatureType = zipFeatureTypeList.Where(x => x.FeatureType == FeatureType.Option).FirstOrDefault();
                if (optionFeatureType != null && optionFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate Angular Option files on '{angularDir}' ***", "Green");

                    GenerateOption(angularDir, optionFeatureType.FeatureDataList);
                }

                // Generate Team angular files
                ZipFeatureType teamFeatureType = zipFeatureTypeList.Where(x => x.FeatureType == FeatureType.Team).FirstOrDefault();
                if (teamFeatureType != null && teamFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"Team generation not yet implemented!", "Orange");

                    GenerateTeam(angularDir, teamFeatureType.FeatureDataList);
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in CRUD generation process: {ex.Message}", "Red");
            }

            return generationFolder;
        }

        #region Feature
        private void GenerateWebApi(string destDir, List<FeatureData> featureDataList, Project currentProject, List<CrudProperty> crudDtoProperties, string displayItem)
        {
            try
            {
                string srcDir = Path.Combine(GetGenerationFolder(currentProject), Constants.FolderDotNet);
                WebApiFeatureData dtoPlaneFeature = ((WebApiFeatureData)featureDataList.FirstOrDefault(x => ((WebApiFeatureData)x).FileType == WebApiFileType.Dto));
                ClassDefinition dtoClassDefiniton = dtoPlaneFeature?.ClassFileDefinition;
                List<WebApiNamespace> crudNamespaceList = ListCrudNamespaces(destDir, featureDataList, currentProject, dtoClassDefiniton);

                // Generate Crud files
                foreach (WebApiFeatureData crudData in featureDataList.Where(ft => !ft.IsPartialFile))
                {
                    if (crudData.FileType == WebApiFileType.Dto ||
                        crudData.FileType == WebApiFileType.Entity ||
                        crudData.FileType == WebApiFileType.Mapper)
                    {
                        // Ignore file : not necessary to regenerate it
                        continue;
                    }

                    // Update WebApi files (not partial)
                    UpdateWebApiFile(destDir, currentProject, crudData, dtoClassDefiniton, crudNamespaceList, crudDtoProperties, displayItem);
                }

                // Update partial files
                foreach (WebApiFeatureData crudData in featureDataList.Where(ft => ft.IsPartialFile))
                {
                    // Update with partial value
                    UpdatePartialFile(srcDir, destDir, currentProject, crudData, FeatureType.WebApi, dtoClassDefiniton);
                }

            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in DotNet CRUD generation process: {ex.Message}", "Red");
            }
        }

        private void GenerateCRUD(string angularDir, List<FeatureData> featureDataList, Project currentProject, List<CrudProperty> crudDtoProperties, string displayItem, List<PropertyInfo> dtoRefProperties)
        {
            try
            {
                foreach (FeatureData crudData in featureDataList)
                {
                    if (crudData.IsPartialFile)
                    {
                        // Update with partial file
                        string srcDir = Path.Combine(GetGenerationFolder(currentProject), currentProject.BIAFronts);
                        UpdatePartialFile(srcDir, angularDir, currentProject, crudData, FeatureType.CRUD);
                    }
                    else
                    {
                        GenerationCrudData generationData = ExtractGenerationCrudData(crudData, crudDtoProperties, dtoRefProperties, displayItem);

                        // Create file
                        string src = Path.Combine(crudData.ExtractDirPath, crudData.FilePath);
                        string dest = ConvertCamelToKebabCrudName(Path.Combine(angularDir, crudData.FilePath), FeatureType.CRUD);

                        // Replace blocks
                        GenerateAngularFile(FeatureType.CRUD, src, dest, generationData);
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in Angular CRUD generation process: {ex.Message}", "Red");
            }
        }

        private void GenerateOption(string angularDir, List<FeatureData> featureDataList)
        {
            try
            {
                foreach (FeatureData angularFile in featureDataList)
                {
                    // Create file
                    string src = Path.Combine(angularFile.ExtractDirPath, angularFile.FilePath);
                    string dest = ConvertCamelToKebabCrudName(Path.Combine(angularDir, angularFile.FilePath), FeatureType.Option);

                    // replace blocks 
                    GenerateAngularFile(FeatureType.Option, src, dest);
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in Angular Option generation process: {ex.Message}", "Red");
            }
        }

        private void GenerateTeam(string angularDir, List<FeatureData> featureDataList)
        {
            // TODO
            consoleWriter.AddMessageLine("Generate Team not implemented!", "Orange");
        }
        #endregion

        private GenerationCrudData ExtractGenerationCrudData(FeatureData crudData, List<CrudProperty> crudDtoProperties, List<PropertyInfo> dtoRefProperties, string displayItem)
        {

            GenerationCrudData generationData = null;
            if (crudData.ExtractBlocks?.Count > 0)
            {
                generationData = new();
                foreach (ExtractBlock block in crudData.ExtractBlocks)
                {
                    switch (block.DataUpdateType)
                    {
                        case CRUDDataUpdateType.Properties:
                            generationData.PropertiesToAdd.AddRange(GeneratePropertiesToAdd((ExtractPropertiesBlock)block, crudDtoProperties));
                            break;
                        case CRUDDataUpdateType.Child:
                            generationData.IsChildrenToDelete |= block.BlockLines.Count > 0;
                            generationData.ChildrenName.Add(block.Name);
                            break;
                        case CRUDDataUpdateType.Option:
                            generationData.IsOptionToDelete |= block.BlockLines.Count > 0;
                            generationData.OptionsName.Add(block.Name);
                            break;
                        case CRUDDataUpdateType.Display:
                            string extractItem = ((ExtractDisplayBlock)block).ExtractItem;
                            string extractLine = ((ExtractDisplayBlock)block).ExtractLine;
                            string newDisplayLine = extractLine.Replace(extractItem, CommonTools.ConvertToCamelCase(displayItem));
                            generationData.DisplayToUpdate.Add(new KeyValuePair<string, string>(extractLine, newDisplayLine));
                            break;
                        default:
                            break;
                    }
                }

                // Generate new blocks to add
                if (crudData.ExtractBlocks.Any(b => b.DataUpdateType == CRUDDataUpdateType.Block))
                {
                    generationData.BlocksToAdd.AddRange(GenerateBlocks(crudData, crudDtoProperties, dtoRefProperties));
                }

                // Parents blocks to update
                List<ExtractBlock> parentBlocks = crudData.ExtractBlocks.Where(b => b.DataUpdateType == CRUDDataUpdateType.Parent).ToList();
                if (parentBlocks.Any())
                {
                    PrepareParentBlock(parentBlocks, generationData, crudDtoProperties, true);
                }
            }

            return generationData;
        }


        #region DotNet Files
        private void UpdateWebApiFile(string destDir, Project currentProject, WebApiFeatureData crudData, ClassDefinition dtoClassDefiniton, List<WebApiNamespace> crudNamespaceList, List<CrudProperty> crudDtoProperties, string displayItem)
        {
            string src = Path.Combine(crudData.ExtractDirPath, crudData.FilePath);
            string dest = ConvertPascalOldToNewCrudName(Path.Combine(destDir, crudData.FilePath), FeatureType.WebApi);
            dest = ReplaceCompagnyNameProjetName(dest, currentProject, dtoClassDefiniton);

            // Prepare destination folder
            CommonTools.CheckFolder(new FileInfo(dest).DirectoryName);

            GenerationCrudData generationData = ExtractGenerationCrudData(crudData, crudDtoProperties, null, displayItem);

            // Read file
            List<string> fileLinesContent = UpdateFileContent(generationData, src);

            for (int i = 0; i < fileLinesContent.Count; i++)
            {
                if (CommonTools.IsNamespaceOrUsingLine(fileLinesContent[i]))
                {
                    fileLinesContent[i] = UpdateNamespaceUsing(fileLinesContent[i], currentProject, dtoClassDefiniton, crudNamespaceList);
                }
                // Convert Crud Name (Plane to XXX)
                fileLinesContent[i] = ConvertPascalOldToNewCrudName(fileLinesContent[i], FeatureType.WebApi);
            }

            // Generate new file
            CommonTools.GenerateFile(dest, fileLinesContent);
        }

        private List<WebApiNamespace> ListCrudNamespaces(string destDir, List<FeatureData> featureDataList, Project currentProject, ClassDefinition dtoClassDefiniton)
        {
            List<WebApiNamespace> namespaceList = new();
            foreach (WebApiFeatureData crudData in featureDataList)
            {
                if (crudData.FileType == null || crudData.FileType == WebApiFileType.Partial)
                {
                    continue;
                }

                WebApiNamespace webApiNamespace = new(crudData.FileType.Value, crudData.Namespace);
                namespaceList.Add(webApiNamespace);

                // If Dto/Entity/Mapper file => file already exists => get namespace
                if (crudData.FileType == WebApiFileType.Dto ||
                    crudData.FileType == WebApiFileType.Entity ||
                    crudData.FileType == WebApiFileType.Mapper)
                {
                    // Get part of namespace before "plane" occurency
                    string partPath = GetNamespacePathBeforeOccurency(crudData.Namespace);

                    // Replace company + projet name on part path
                    partPath = ReplaceCompagnyNameProjetName(partPath, currentProject, dtoClassDefiniton);

                    // Replace "plane" file name with good "crud" value
                    string fileName = crudData.FileName.Replace(OldCrudNamePascalSingular, NewCrudNamePascalSingular);

                    // Search file on disk
                    string foundFile = Directory.EnumerateFiles(Path.Combine(destDir, partPath), fileName, SearchOption.AllDirectories).FirstOrDefault();

                    // Extract real namespace of file found
                    if (!string.IsNullOrWhiteSpace(foundFile))
                    {
                        // Read file
                        List<string> fileLinesContent = File.ReadAllLines(foundFile).ToList();
                        foreach (string line in fileLinesContent)
                        {
                            // Search namespace line
                            if (line.TrimStart().StartsWith("namespace"))
                            {
                                // Get namespace value
                                webApiNamespace.CrudNamespaceGenerated = CommonTools.GetNamespaceOrUsingValue(line);
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Generate namespace by default
                        consoleWriter.AddMessageLine($"File '{fileName}' not found on path '{Path.Combine(destDir, partPath)}' folder or children.", "Orange");
                        webApiNamespace.CrudNamespaceGenerated = ReplaceCompagnyNameProjetName(crudData.Namespace, currentProject, dtoClassDefiniton).Replace(OldCrudNamePascalSingular, NewCrudNamePascalSingular);
                    }
                }
                else
                {
                    // Generate namespace for generated files: Controller, (I)AppService ...
                    webApiNamespace.CrudNamespaceGenerated = ReplaceCompagnyNameProjetName(crudData.Namespace, currentProject, dtoClassDefiniton).Replace(OldCrudNamePascalSingular, NewCrudNamePascalSingular);
                }
            }
            return namespaceList;
        }

        private string UpdateNamespaceUsing(string line, Project currentProject, ClassDefinition dtoClassDefiniton, List<WebApiNamespace> crudNamespaceList)
        {
            string nmsp = CommonTools.GetNamespaceOrUsingValue(line);
            if (!string.IsNullOrWhiteSpace(nmsp))
            {
                WebApiNamespace nsp = crudNamespaceList.FirstOrDefault(x => x.CrudNamespace == nmsp);
                if (nsp != null)
                {
                    line = line.Replace(nsp.CrudNamespace, nsp.CrudNamespaceGenerated);
                }
                else
                {
                    if (line.Contains(dtoClassDefiniton.CompagnyName) ||
                        line.Contains(dtoClassDefiniton.ProjectName))
                    {
                        // Replace Compagny name and Project name if exists
                        line = ReplaceCompagnyNameProjetName(line, currentProject, dtoClassDefiniton);
                    }
                }
            }

            return line;
        }
        #endregion

        #region Partial Files
        private void UpdatePartialFile(string srcDir, string destDir, Project currentProject, FeatureData crudData, FeatureType type, ClassDefinition dtoClassDefiniton = null)
        {
            string markerBegin, markerEnd, suffix;
            List<string> contentToAdd;

            string fileName = crudData.FilePath.Replace(Constants.PartialFileSuffix, "");
            if (dtoClassDefiniton != null)
            {
                fileName = ReplaceCompagnyNameProjetName(fileName, currentProject, dtoClassDefiniton);
            }

            string srcFile = Path.Combine(srcDir, fileName);
            string destFile = Path.Combine(destDir, fileName);

            crudData.ExtractBlocks?.ForEach(b =>
            {
                contentToAdd = new();
                ExtractPartialBlock block = (ExtractPartialBlock)b;
                if (block.BlockLines != null)
                {
                    if (string.IsNullOrWhiteSpace(block.Index))
                    {
                        suffix = $"{block.DataUpdateType}";
                    }
                    else
                    {
                        suffix = $"{block.DataUpdateType} {block.Index}";
                    }

                    markerBegin = $"{ZipParserService.MARKER_BEGIN} {suffix}";
                    markerEnd = $"{ZipParserService.MARKER_END} {suffix}";

                    // Generate content to add
                    block.BlockLines.ForEach(line =>
                    {
                        string newline = ReplaceOldCamelToNewKebabPath(line);
                        newline = ConvertPascalOldToNewCrudName(newline, type);
                        if (dtoClassDefiniton != null)
                        {
                            newline = ReplaceCompagnyNameProjetName(newline, currentProject, dtoClassDefiniton);
                        }
                        contentToAdd.Add(newline);
                    });

                    // Update file
                    UpdatePartialCrudFile(srcFile, destFile, contentToAdd, markerBegin, markerEnd);
                }
            });
        }

        private void UpdatePartialCrudFile(string srcFile, string destFile, List<string> contentToAdd, string markerBegin, string markerEnd)
        {
            // Read file
            List<string> fileContent = File.ReadAllLines(srcFile).ToList();

            // Insert data on file content
            List<string> newContent = InsertContentBetweenMarkers(fileContent, contentToAdd, markerBegin, markerEnd);

            // Generate file with new content
            CommonTools.GenerateFile(destFile, newContent);
        }
        #endregion

        #region Angular Files
        private void GenerateAngularFile(FeatureType type, string fileName, string newFileName, GenerationCrudData generationData = null)
        {
            if (!File.Exists(fileName))
            {
                consoleWriter.AddMessageLine($"Error on generating angular CRUD: file not exist on disk: '{fileName}'", "Orange");
                return;
            }

            // Prepare destination folder
            CommonTools.CheckFolder(new FileInfo(newFileName).DirectoryName);

            // Read file
            List<string> fileLinesContent = UpdateFileContent(generationData, fileName);

            // Update file content
            UpdateFileLinesContent(fileLinesContent, type);

            // Generate new file
            CommonTools.GenerateFile(newFileName, fileLinesContent);
        }

        private List<string> UpdateFileContent(GenerationCrudData generationData, string fileName)
        {
            List<string> fileLinesContent = File.ReadAllLines(fileName).ToList();

            if (generationData != null)
            {
                // Replace properties
                if (generationData.PropertiesToAdd?.Count > 0)
                {
                    fileLinesContent = ReplaceProperties(fileName, fileLinesContent, generationData.PropertiesToAdd);
                }

                // Replace blocks
                if (generationData.BlocksToAdd?.Count > 0)
                {
                    fileLinesContent = ReplaceBlocks(fileName, fileLinesContent, generationData.BlocksToAdd);
                }

                // Replace display item
                if (generationData.DisplayToUpdate?.Count > 0)
                {
                    fileLinesContent = ReplaceDisplayItem(fileLinesContent, generationData.DisplayToUpdate);
                }

                // Remove children
                if (generationData.IsChildrenToDelete)
                {
                    fileLinesContent = DeleteChildrenBlocks(fileLinesContent, generationData.ChildrenName);
                }

                // Remove all options
                if (generationData.IsOptionToDelete)
                {
                    fileLinesContent = DeleteOptionsBlocks(fileLinesContent, generationData.OptionsName);
                }

                // Upate parent blocks
                if (generationData.ParentBlocks?.Count > 0)
                {
                    fileLinesContent = UpdateParentBlocks(fileName, fileLinesContent, generationData.ParentBlocks, generationData.IsParentToAdd);
                }
            }

            return fileLinesContent;
        }

        private List<string> ReplaceBlocks(string fileName, List<string> fileLinesContent, List<string> blockList)
        {
            string spaces = string.Empty;
            int indexBegin = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains($"{ZipParserService.MARKER_BEGIN} {CRUDDataUpdateType.Block}")).FirstOrDefault());
            if (indexBegin != -1)
            {
                spaces = CommonTools.GetSpacesBeginningLine(fileLinesContent[indexBegin]);
            }

            List<string> newBlockList = new();
            for (int i = 0; i < blockList.Count; i++)
            {
                newBlockList.Add(blockList[i]);
                if (i != blockList.Count - 1)
                    newBlockList.Add($"{spaces},");
            }

            return UpdateBlocks(fileName, fileLinesContent, newBlockList, CRUDDataUpdateType.Block);
        }

        private List<string> ReplaceProperties(string fileName, List<string> fileLinesContent, List<string> propertiesToAdd)
        {
            int indexBeginProperty = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains($"{ZipParserService.MARKER_BEGIN} {CRUDDataUpdateType.Properties}")).FirstOrDefault());
            int indexEndProperty = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains($"{ZipParserService.MARKER_END} {CRUDDataUpdateType.Properties}")).FirstOrDefault());
            if (indexBeginProperty != -1 && indexEndProperty != -1)
            {
                propertiesToAdd?.Insert(0, fileLinesContent[indexBeginProperty]);
                propertiesToAdd?.Add(fileLinesContent[indexEndProperty]);
            }

            return UpdateBlocks(fileName, fileLinesContent, propertiesToAdd, CRUDDataUpdateType.Properties);
        }

        private List<string> ReplaceDisplayItem(List<string> fileLinesContent, List<KeyValuePair<string, string>> displayToUpdate)
        {
            foreach (KeyValuePair<string, string> display in displayToUpdate)
            {
                int index = fileLinesContent.FindIndex(x => x.Contains(display.Key));
                if (index >= 0 && index < fileLinesContent.Count)
                {
                    fileLinesContent[index] = fileLinesContent[index].Replace(display.Key, display.Value);
                }
            }

            return fileLinesContent;
        }

        private List<string> DeleteChildrenBlocks(List<string> fileLinesContent, List<string> childrenName)
        {
            string beginMarker = $"{ZipParserService.MARKER_BEGIN} {CRUDDataUpdateType.Child}";
            string endMarker = $"{ZipParserService.MARKER_END} {CRUDDataUpdateType.Child}";

            childrenName?.ForEach(childName =>
            {
                string markerBegin, markerEnd;
                if (string.IsNullOrWhiteSpace(childName))
                {
                    markerBegin = $"<!-- {beginMarker} ";
                    markerEnd = $"{endMarker} -->";
                }
                else
                {
                    markerBegin = $"<!-- {beginMarker} {childName}";
                    markerEnd = $"{endMarker} {childName} -->";
                }
                fileLinesContent = DeleteBlocks(fileLinesContent, markerBegin, markerEnd);
            });

            return DeleteBlocks(fileLinesContent, CRUDDataUpdateType.Child);
        }

        private List<string> DeleteOptionsBlocks(List<string> fileLinesContent, List<string> optionsName)
        {
            string beginMarker = $"{ZipParserService.MARKER_BEGIN} {CRUDDataUpdateType.Option}";
            string endMarker = $"{ZipParserService.MARKER_END} {CRUDDataUpdateType.Option}";

            optionsName?.ForEach(optionName =>
            {
                string markerBegin, markerEnd;
                if (string.IsNullOrWhiteSpace(optionName))
                {
                    markerBegin = $"/* {beginMarker} ";
                    markerEnd = $"{endMarker} */";
                }
                else
                {
                    markerBegin = $"/* {beginMarker} {optionName}";
                    markerEnd = $"{endMarker} {optionName} */";
                }

                fileLinesContent = DeleteBlocks(fileLinesContent, markerBegin, markerEnd);
            });

            return DeleteBlocks(fileLinesContent, CRUDDataUpdateType.Option);
        }

        private List<string> UpdateParentBlocks(string fileName, List<string> fileLinesContent, List<List<string>> parentBlocks, bool isParentToAdd)
        {
            if (!isParentToAdd)
            {
                // Delete parents blocks
                return DeleteBlocks(fileLinesContent, CRUDDataUpdateType.Parent);
            }
            else
            {
                for (int i = 0; i < parentBlocks.Count; i++)
                {
                    fileLinesContent = UpdateBlocks(fileName, fileLinesContent, parentBlocks[i], CRUDDataUpdateType.Parent, i);
                }

                return fileLinesContent;
            }
        }

        private List<string> UpdateBlocks(string fileName, List<string> fileLinesContent, List<string> blockList, CRUDDataUpdateType type, int count = -1)
        {
            int indexBegin = -1;
            int indexEnd = -1;
            if (count == -1)
            {
                indexBegin = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains($"{ZipParserService.MARKER_BEGIN} {type}")).FirstOrDefault());
                indexEnd = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains($"{ZipParserService.MARKER_END} {type}")).FirstOrDefault());
            }
            else
            {
                indexBegin = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains($"{ZipParserService.MARKER_BEGIN} {type}")).ToArray()[count]);
                indexEnd = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains($"{ZipParserService.MARKER_END} {type}")).ToArray()[count]);
            }

            List<string> newFileLinesContent = new();
            if (indexBegin != -1 && indexEnd != -1)
            {
                // Write lines before first block
                for (int i = 0; i < indexBegin; i++)
                {
                    newFileLinesContent.Add(fileLinesContent[i]);
                }

                // Write blocks to add
                newFileLinesContent.AddRange(blockList);

                // Write lines after last block
                for (int i = indexEnd + 1; i < fileLinesContent.Count; i++)
                {
                    newFileLinesContent.Add(fileLinesContent[i]);
                }
            }
            else
            {
                // Error
                consoleWriter.AddMessageLine($"Update File '{fileName}', {type} index not found (begin={indexBegin}, end={indexEnd})", "Orange");
                newFileLinesContent = fileLinesContent;
            }

            return newFileLinesContent;
        }

        private List<string> DeleteBlocks(List<string> fileLinesContent, CRUDDataUpdateType type)
        {
            string markerBegin = $"{ZipParserService.MARKER_BEGIN} {type}";
            string markerEnd = $"{ZipParserService.MARKER_END} {type}";

            return DeleteBlocks(fileLinesContent, markerBegin, markerEnd);
        }

        private List<string> DeleteBlocks(List<string> fileLinesContent, string markerBegin, string markerEnd)
        {
            if (!CommonTools.IsFileContainsData(fileLinesContent, new List<string> { markerBegin }))
            {
                return fileLinesContent;
            }

            List<string> updateLines = new();
            for (int i = 0; i < fileLinesContent.Count; i++)
            {
                string line = fileLinesContent[i];

                if (line.Contains(markerBegin, StringComparison.InvariantCultureIgnoreCase) &&
                    line.Contains(markerEnd, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Get data before marker begin + data after marker end
                    string[] splitBegin = line.Split(markerBegin);
                    string[] splitEnd = line.Split(markerEnd);
                    string update = splitBegin[0] + splitEnd[1];
                    if (!string.IsNullOrWhiteSpace(update))
                    {
                        updateLines.Add(update);
                    }
                }
                else if (line.Contains(markerBegin, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Get data before marker begin
                    bool endFound = false;
                    if (!line.TrimStart().StartsWith("//"))
                    {
                        string[] splitBegin = line.Split(markerBegin);
                        if (!string.IsNullOrWhiteSpace(splitBegin[0]))
                        {
                            updateLines.Add(splitBegin[0]);
                        }
                    }
                    //else
                    //{
                    //    updateLines.Add(line);  // TODO: remove if don't want marker in final file
                    //}

                    for (int j = i; j < fileLinesContent.Count && !endFound; j++)
                    {
                        line = fileLinesContent[j];
                        if (line.Contains(markerEnd, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (!line.TrimStart().StartsWith("//"))
                            {
                                // Get data after marker end                               
                                string[] splitEnd = line.Split(markerEnd);
                                if (!string.IsNullOrWhiteSpace(splitEnd[1]))
                                {
                                    // Get firsts space characters (space + tabulations) to keep formating
                                    string match = CommonTools.GetMatchRegexValue(@"^([\s\t]+)(\w+)", splitEnd[0]);
                                    updateLines.Add(match + splitEnd[1]);
                                }
                            }
                            //else
                            //{
                            //    updateLines.Add(line); // TODO: remove if don't want marker in final file
                            //}

                            endFound = true;
                            i = j;
                        }
                        // ignore lines between begin marker and end marker
                    }
                }
                else
                {
                    // Keep data line without marker
                    updateLines.Add(line);
                }
            }

            return updateLines;
        }

        private List<List<string>> GenerateParentBlocks(List<ExtractBlock> blocksList, List<CrudProperty> crudDtoProperties, bool convertToCaml)
        {
            List<CrudProperty> parents = crudDtoProperties.Where(c => c.IsParent).ToList();
            if (!parents.Any())
            {
                return null;
            }

            List<List<string>> blocksToAdd = new();
            foreach (ExtractBlock block in blocksList)
            {
                // Generic parent block
                if (string.IsNullOrWhiteSpace(block.Name))
                {
                    blocksToAdd.Add(block.BlockLines);
                    continue;
                }

                // Specifics parents blocks
                foreach (CrudProperty parent in parents)
                {
                    string parentName = parent.Name;
                    if (convertToCaml)
                        parentName = CommonTools.ConvertToCamelCase(parent.Name);
                    block.BlockLines.ForEach(line => line.Replace(block.Name, parentName));
                    blocksToAdd.Add(block.BlockLines);
                }
            }

            return blocksToAdd;
        }

        private List<string> GeneratePropertiesToAdd(ExtractPropertiesBlock propertyBlock, List<CrudProperty> crudProperties)
        {
            string regex = $@"^(\s*)(\w+)(\s*{Constants.PropertySeparator}\s*)(\w+\W*\w*)(;\s*)$";
            List<string> propertiesToAdd = new();

            foreach (CrudProperty crudProperty in crudProperties)
            {
                CRUDPropertyType convertType = ConvertDotNetToAngularType(crudProperty);

                CRUDPropertyType propertyReference = propertyBlock.PropertiesList.FirstOrDefault(p => p.Type == convertType.Type);
                if (propertyReference == null)
                {
                    // if type not found verfiy with Simplified type
                    propertyReference = propertyBlock.PropertiesList.FirstOrDefault(p => p.Type == convertType.SimplifiedType);
                    if (propertyReference == null)
                    {
                        // in other cases (type and simplified not found), get first by default (string)
                        propertyReference = propertyBlock.PropertiesList.FirstOrDefault(p => p.Type == "string");
                    }
                }

                // Get property line as "model"
                string lineFound = propertyBlock.BlockLines.FirstOrDefault(x => x.TrimStart().StartsWith(propertyReference.Name));

                // Generate new property from model
                string newline = Regex.Replace(lineFound, regex, $"$1{CommonTools.ConvertToCamelCase(crudProperty.Name)}$3{convertType.Type}$5");
                propertiesToAdd.Add(newline);
            }

            return propertiesToAdd;
        }

        private List<string> GenerateBlocks(FeatureData crudData, List<CrudProperty> crudDtoProperties, List<PropertyInfo> dtoRefProperties)
        {
            if (crudDtoProperties == null || dtoRefProperties == null)
                return null;

            List<string> blocksToAdd = new();
            // Get block list and properties associated to blocks
            ExtractBlock propertyBlock = crudData.ExtractBlocks.FirstOrDefault(p => p.DataUpdateType == CRUDDataUpdateType.Properties);
            List<ExtractBlock> blocksList = crudData.ExtractBlocks.FindAll(b => b.DataUpdateType == CRUDDataUpdateType.Block);

            // Generate block based on dto model
            foreach (CrudProperty crudProperty in crudDtoProperties.Where(p => !p.IsParent))
            {
                ExtractBlock blockReferenceFound = GetReferenceBlock(crudProperty, blocksList, dtoRefProperties);

                if (blockReferenceFound != null)
                {
                    // Generate block to add
                    CheckRequiredLine(blockReferenceFound.BlockLines, crudProperty.IsRequired);
                    blocksToAdd.Add(ReplaceBlock(blockReferenceFound, crudProperty.Name));
                }
                else
                {
                    // Generate empty block to add
                    ExtractBlock defaultBlock = blocksList.First();
                    if (defaultBlock != null)
                    {
                        blocksToAdd.Add(CreateEmptyBlock(defaultBlock, crudProperty.Type, crudProperty.Name, crudProperty.IsRequired));
                    }
                }
            }

            return blocksToAdd;
        }

        private ExtractBlock GetReferenceBlock(CrudProperty crudProperty, List<ExtractBlock> blocksList, List<PropertyInfo> dtoRefProperties)
        {
            ExtractBlock blockReferenceFound = null;
            PropertyInfo pi = null;
            if (!string.IsNullOrEmpty(crudProperty.AnnotationType))
            {
                foreach (PropertyInfo dtoRefProperty in dtoRefProperties)
                {
                    if (dtoRefProperty.Annotations != null)
                    {
                        KeyValuePair<string, string> annotationFound = dtoRefProperty.Annotations.FirstOrDefault(a => a.Key == BIA_DTO_FIELD_TYPE && a.Value == crudProperty.AnnotationType);
                        if (annotationFound.Key != null)
                        {
                            pi = dtoRefProperty;
                            break;
                        }
                    }
                }
            }
            else
            {
                pi = dtoRefProperties.FirstOrDefault(p => p.Type == crudProperty.Type);
            }

            if (!string.IsNullOrWhiteSpace(pi?.Name))
            {
                blockReferenceFound = blocksList.FirstOrDefault(b => b.Name.Equals(pi.Name, StringComparison.InvariantCultureIgnoreCase));
            }

            return blockReferenceFound;
        }

        private void CheckRequiredLine(List<string> lines, bool isRequired)
        {
            bool contains = CommonTools.IsFileContainsData(lines, new List<string> { IS_REQUIRED_PROPERTY });
            if (contains && !isRequired)
            {
                // Remove
                string lineFound = lines.FirstOrDefault(line => line.Contains(IS_REQUIRED_PROPERTY));
                if (lineFound != null)
                {
                    lines.Remove(lineFound);
                }
            }
            if (!contains && isRequired)
            {
                // Add
                string spaces = CommonTools.GetSpacesBeginningLine(lines[0]);
                lines.Insert(2, $"{spaces}  {IS_REQUIRED_PROPERTY}true,");
            }
        }

        private string ReplaceBlock(ExtractBlock extractBlock, string crudAttributeName, string dtoAttributeName = null)
        {
            if (string.IsNullOrWhiteSpace(dtoAttributeName))
                dtoAttributeName = extractBlock.Name;

            dtoAttributeName = CommonTools.ConvertToCamelCase(dtoAttributeName);
            crudAttributeName = CommonTools.ConvertToCamelCase(crudAttributeName);

            StringBuilder sb = new();
            int count;
            for (count = 0; count < extractBlock.BlockLines.Count - 1; count++)
            {
                sb.AppendLine(extractBlock.BlockLines[count]);  // Add all lines except the last
            }
            sb.Append(extractBlock.BlockLines[count]);          // Add the last

            string newBlock = sb.ToString().Replace(dtoAttributeName, crudAttributeName);
            newBlock = newBlock.Replace(OldCrudNameCamelPlural, NewCrudNameCamelPlural);
            newBlock = newBlock.Replace(OldCrudNameCamelSingular, NewCrudNameCamelSingular);

            return newBlock;
        }

        private string CreateEmptyBlock(ExtractBlock extractBlock, string attributeType, string attributeName, bool isRequired)
        {
            List<string> newBlockLines = new();
            int length = extractBlock.BlockLines.Count;

            if (length > 2)
            {
                extractBlock.BlockLines.RemoveAll(line => string.IsNullOrEmpty(line));
                string startBLockComment = extractBlock.BlockLines.First();
                string endBLockComment = extractBlock.BlockLines.Last();

                newBlockLines.Add(ATTRIBUTE_TYPE_NOT_MANAGED.Replace(ATTRIBUTE_TYPE_NOT_MANAGED_FIELD, attributeName).Replace(ATTRIBUTE_TYPE_NOT_MANAGED_TYPE, attributeType));
                newBlockLines.Add(extractBlock.BlockLines[0]);                  // start block comment
                newBlockLines.Add(extractBlock.BlockLines[1]);                  // first block code line
                if (isRequired)
                {
                    string spaces = CommonTools.GetSpacesBeginningLine(extractBlock.BlockLines[0]);
                    newBlockLines.Add($"{spaces}  {IS_REQUIRED_PROPERTY}true,");  // IsRequired line
                }
                newBlockLines.Add(extractBlock.BlockLines[length - 2]);         // last block code line
                newBlockLines.Add(extractBlock.BlockLines[length - 1]);         // end block comment
            }

            ExtractBlock newBlock = new(CRUDDataUpdateType.Block, attributeName, newBlockLines);
            return ReplaceBlock(newBlock, attributeName, extractBlock.Name);
        }

        private void UpdateFileLinesContent(List<string> lines, FeatureType type)
        {
            if (lines == null) return;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("import"))
                {
                    lines[i] = ConvertOldCrudNameToNewCrudName(lines[i], type);
                }
                else
                {
                    lines[i] = ReplaceOldCamelToNewKebabPath(lines[i]);
                    lines[i] = ConvertPascalOldToNewCrudName(lines[i], type);
                }
            }
        }

        private void PrepareParentBlock(List<ExtractBlock> parentBlocks, GenerationCrudData generationData, List<CrudProperty> crudDtoProperties, bool convertToCamel)
        {
            List<List<string>> blocks = GenerateParentBlocks(parentBlocks, crudDtoProperties, convertToCamel);
            if (blocks == null)
            {
                generationData.IsParentToAdd = false;
                parentBlocks.ForEach(b => generationData.ParentBlocks.Add(b.BlockLines));
            }
            else
            {
                generationData.IsParentToAdd = true;
                generationData.ParentBlocks.AddRange(blocks);
            }
        }
        #endregion

        #region Rename CRUD
        private string ReplaceCompagnyNameProjetName(string value, Project currentProject, ClassDefinition classDef)
        {
            return value.Replace(classDef.CompagnyName, currentProject.CompanyName).Replace(classDef.ProjectName, currentProject.Name);
        }

        private string ConvertOldCrudNameToNewCrudName(string value, FeatureType type)
        {
            value = ConvertPascalOldToNewCrudName(value, type, false);
            value = ConvertCamelToKebabCrudName(value, type);

            return value;
        }

        private string ConvertPascalOldToNewCrudName(string value, FeatureType type, bool convertCamel = true)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            switch (type)
            {
                case FeatureType.WebApi:
                case FeatureType.CRUD:
                    value = ReplaceOldToNewPascalValue(value, OldCrudNamePascalPlural, NewCrudNamePascalPlural, OldCrudNamePascalSingular, NewCrudNamePascalSingular);
                    if (convertCamel)
                    {
                        value = ReplaceOldToNewPascalValue(value, OldCrudNameCamelPlural, NewCrudNameCamelPlural, OldCrudNameCamelSingular, NewCrudNameCamelSingular);
                    }
                    break;
                case FeatureType.Option:
                    value = ReplaceOldToNewPascalValue(value, OldOptionNamePascalPlural, NewCrudNamePascalPlural, OldOptionNamePascalSingular, NewCrudNamePascalSingular);
                    if (convertCamel)
                    {
                        value = ReplaceOldToNewPascalValue(value, OldOptionNameCamelPlural, NewCrudNameCamelPlural, OldOptionNameCamelSingular, NewCrudNameCamelSingular);
                    }
                    break;
                case FeatureType.Team:
                    value = ReplaceOldToNewPascalValue(value, OldTeamNamePascalPlural, NewCrudNamePascalPlural, OldTeamNamePascalSingular, NewCrudNamePascalSingular);
                    if (convertCamel)
                    {
                        value = ReplaceOldToNewPascalValue(value, OldTeamNameCamelPlural, NewCrudNameCamelPlural, OldTeamNameCamelSingular, NewCrudNameCamelSingular);
                    }
                    break;
            }

            return value;
        }

        private string ReplaceOldToNewPascalValue(string value, string oldValuePlurial, string newValuePlurial, string oldValueSingulier, string newValueSingulier)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            if (value.Contains(oldValuePlurial))
            {
                value = value.Replace(oldValuePlurial, newValuePlurial);
            }
            if (value.Contains(oldValueSingulier))
            {
                value = value.Replace(oldValueSingulier, newValueSingulier);
            }

            return value;
        }

        private string ReplaceOldCamelToNewKebabPath(string value)
        {
            if (value.Contains($"/{OldCrudNameCamelPlural}"))
            {
                value = value.Replace($"/{OldCrudNameCamelPlural}", $"/{NewCrudNameKebabPlural}");
            }
            if (value.Contains($"/{OldCrudNameCamelSingular}"))
            {
                value = value.Replace($"/{OldCrudNameCamelSingular}", $"/{NewCrudNameKebabSingular}");
            }

            return value;
        }

        /// <summary>
        /// Convert value form Camel case to Kebab case
        /// </summary>
        private string ConvertCamelToKebabCrudName(string value, FeatureType type)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            switch (type)
            {
                case FeatureType.CRUD:
                    if (value.Contains(OldCrudNameCamelPlural))
                    {
                        value = value.Replace(OldCrudNameCamelPlural, NewCrudNameKebabPlural);
                    }
                    if (value.Contains(OldCrudNameCamelSingular))
                    {
                        value = value.Replace(OldCrudNameCamelSingular, NewCrudNameKebabSingular);
                    }
                    break;
                case FeatureType.Option:
                    if (value.Contains(OldOptionNameCamelPlural))
                    {
                        value = value.Replace(OldOptionNameCamelPlural, NewCrudNameKebabPlural);
                    }
                    if (value.Contains(OldOptionNameCamelSingular))
                    {
                        value = value.Replace(OldOptionNameCamelSingular, NewCrudNameKebabSingular);
                    }
                    break;
                case FeatureType.Team:
                    if (value.Contains(OldTeamNameCamelPlural))
                    {
                        value = value.Replace(OldTeamNameCamelPlural, NewCrudNameKebabPlural);
                    }
                    if (value.Contains(OldTeamNameCamelSingular))
                    {
                        value = value.Replace(OldTeamNameCamelSingular, NewCrudNameKebabSingular);
                    }
                    break;
            }

            return value;
        }
        #endregion

        #region Tools
        private List<CrudProperty> GetDtoProperties(EntityInfo dtoEntity)
        {
            if (dtoEntity == null) { return null; }

            List<CrudProperty> properties = new();
            dtoEntity.Properties.ForEach(p =>
            {
                properties.Add(new CrudProperty(p.Name, p.Type, p.Annotations));
            });
            return properties;
        }

        private string GetGenerationFolder(Project currentProject, bool generateInCurrentProject = true)
        {
            string generatedFolder = Path.Combine(currentProject.Folder, currentProject.Name);

            if (!generateInCurrentProject)
                generatedFolder = Path.Combine(generatedFolder, Constants.FolderCrudGeneration);

            return generatedFolder;
        }

        private List<string> InsertContentBetweenMarkers(List<string> fileContent, List<string> contentToAdd, string markerBegin, string markerEnd)
        {
            List<string> newContent = new();
            List<string> contentBetweenMarker = new();

            // Get content to replace
            int indexBegin = fileContent.IndexOf(fileContent.FirstOrDefault(line => line.Contains(markerBegin)));
            int indexEnd = fileContent.IndexOf(fileContent.FirstOrDefault(line => line.Contains(markerEnd)));
            if (indexBegin < 0 || indexEnd < 0)
            {
                return fileContent;
            }
            // Check if previous generation is present
            List<string> contentToReplace = fileContent.ToArray()[(indexBegin + 1)..indexEnd].ToList();

            // Update content to replace
            if (contentToReplace.Count > 0)
            {
                int indexStart = GetMarkerIndexInFileContent(contentToAdd[0], fileContent, contentToReplace);
                int indexStop = GetMarkerIndexInFileContent(contentToAdd[^1], fileContent, contentToReplace);

                if (indexStart >= 0)
                {
                    for (int i = 0; i < indexStart; i++)
                    {
                        contentBetweenMarker.Add(contentToReplace[i]);
                    }

                    contentBetweenMarker.AddRange(contentToAdd);

                    for (int i = indexStop + 1; i < contentToReplace.Count; i++)
                    {
                        contentBetweenMarker.Add(contentToReplace[i]);
                    }
                }
                else
                {
                    contentBetweenMarker.AddRange(contentToReplace);
                    contentBetweenMarker.AddRange(contentToAdd);
                }
            }
            else
            {
                contentBetweenMarker = contentToAdd;
            }

            // Replace content
            newContent.AddRange(fileContent.ToArray()[0..++indexBegin].ToList());
            newContent.AddRange(contentBetweenMarker);
            newContent.AddRange(fileContent.ToArray()[indexEnd..fileContent.Count].ToList());

            return newContent;
        }

        private int GetMarkerIndexInFileContent(string marker, List<string> fileContent, List<string> contentToReplace)
        {
            string regex = @$"({marker.Replace('/', ' ').TrimStart()})$";
            string markerFound = CommonTools.GetMatchRegexValue(regex, marker);
            return contentToReplace.IndexOf(fileContent.FirstOrDefault(line => line.TrimEnd().EndsWith(markerFound.TrimEnd())));
        }

        private CRUDPropertyType ConvertDotNetToAngularType(CrudProperty crudProperty)
        {
            if (crudProperty.Type == null) { return null; }

            string angularType = crudProperty.Type;

            // In first : manage case of "Collection"
            string match = CommonTools.GetMatchRegexValue(@"<(\w+)>", angularType);
            if (!string.IsNullOrEmpty(match))
            {
                angularType = $"{match}[]";
            }

            // After verify types
            match = CommonTools.GetMatchRegexValue(@"(\w+)(\W*)", angularType);
            if (!string.IsNullOrEmpty(match))
            {
                // Integer
                if (match.ToLower() == "int" || match.ToLower() == "long" || match.ToLower() == "float" || match.ToLower() == "double")
                {
                    angularType = angularType.Replace(match, "number");
                }

                // Boolean
                else if (match.ToLower() == "bool")
                {
                    angularType = angularType.Replace(match, "boolean");
                }

                // Date
                else if (match == "DateTime")
                {
                    angularType = angularType.Replace(match, "Date");
                }

                // Time
                else if (match == "TimeSpan")
                {
                    angularType = angularType.Replace(match, "string");
                }

                // XXXDto
                else if (match.EndsWith("Dto"))
                {
                    angularType = angularType.Replace(match, "OptionDto");
                }
            }

            if (angularType.EndsWith('?') || !crudProperty.IsRequired)
            {
                angularType = $"{angularType.Replace("?", "")} | null";
            }

            return new CRUDPropertyType(crudProperty.Name, angularType);
        }

        private string GetNamespacePathBeforeOccurency(string line)
        {
            string partPath = string.Empty;
            foreach (string nmsp in line.Split('.'))
            {
                if (nmsp.Contains(OldCrudNamePascalSingular, StringComparison.OrdinalIgnoreCase))
                {
                    partPath = partPath.Remove(partPath.Length - 1);
                    break;
                }
                partPath += $"{nmsp}.";
            }

            return partPath;
        }
        #endregion
    }

    class GenerationCrudData
    {
        public List<string> BlocksToAdd { get; }
        public List<string> PropertiesToAdd { get; }
        public bool IsChildrenToDelete { get; set; }
        public List<string> ChildrenName { get; }
        public bool IsOptionToDelete { get; set; }
        public List<string> OptionsName { get; }
        public List<KeyValuePair<string, string>> DisplayToUpdate { get; }
        public bool IsParentToAdd { get; set; }
        public List<List<string>> ParentBlocks { get; }

        public GenerationCrudData()
        {
            this.IsChildrenToDelete = false;
            this.IsOptionToDelete = false;
            this.BlocksToAdd = new();
            this.PropertiesToAdd = new();
            this.ChildrenName = new();
            this.OptionsName = new();
            this.DisplayToUpdate = new();
            this.ParentBlocks = new();
        }
    }

    class CrudProperty
    {
        public string Name { get; }
        public string Type { get; }
        public bool IsRequired { get; private set; } = false;
        public string AnnotationType { get; private set; }
        public bool IsParent { get; private set; } = false;

        public CrudProperty(string name, string type, List<KeyValuePair<string, string>> annotations)
        {
            this.Name = name;
            this.Type = type;
            if (annotations != null)
            {
                PopulateAnnotation(annotations);
            }
        }

        private void PopulateAnnotation(List<KeyValuePair<string, string>> annotations)
        {
            foreach (KeyValuePair<string, string> annotation in annotations)
            {
                if (annotation.Key == GenerateCrudService.BIA_DTO_FIELD_TYPE)
                {
                    this.AnnotationType = annotation.Value;
                }
                else if (annotation.Key == GenerateCrudService.BIA_DTO_FIELD_REQUIRED)
                {
                    if (bool.TryParse(annotation.Value, out bool required))
                    {
                        this.IsRequired = required;
                    }
                }
                else if (annotation.Key == GenerateCrudService.BIA_DTO_FIELD_ISPARENT)
                {
                    if (bool.TryParse(annotation.Value, out bool parent))
                    {
                        this.IsParent = parent;
                    }
                }
                else
                {
                    throw new Exception($"Annotation '{annotation.Key}' not managed.");
                }
            }
        }
    }
}
