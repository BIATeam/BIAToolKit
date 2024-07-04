﻿namespace BIA.ToolKit.Application.Services
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

        public void InitRenameValues(string newValueSingular, string newValuePlural,
                                    string crudNameSingular, string crudNamePlural,
                                    string optionNameSingular, string optionNamePlural,
                                    string teamNameSingular, string teamNamePlural)
        {
            this.NewCrudNamePascalSingular = newValueSingular;
            this.NewCrudNamePascalPlural = newValuePlural;
            this.NewCrudNameCamelSingular = CommonTools.ConvertToCamelCase(NewCrudNamePascalSingular);
            this.NewCrudNameCamelPlural = CommonTools.ConvertToCamelCase(NewCrudNamePascalPlural);
            this.NewCrudNameKebabSingular = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalSingular);
            this.NewCrudNameKebabPlural = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalPlural);

            // Get Pascal case value
            this.OldCrudNamePascalSingular = string.IsNullOrWhiteSpace(crudNameSingular) ? this.OldCrudNamePascalSingular : crudNameSingular;
            this.OldCrudNamePascalPlural = string.IsNullOrWhiteSpace(crudNamePlural) ? this.OldCrudNamePascalPlural : crudNamePlural;
            this.OldOptionNamePascalSingular = string.IsNullOrWhiteSpace(optionNameSingular) ? this.OldOptionNamePascalSingular : optionNameSingular;
            this.OldOptionNamePascalPlural = string.IsNullOrWhiteSpace(optionNamePlural) ? this.OldOptionNamePascalPlural : optionNamePlural;
            this.OldTeamNamePascalSingular = string.IsNullOrWhiteSpace(teamNameSingular) ? this.OldTeamNamePascalSingular : teamNameSingular;
            this.OldTeamNamePascalPlural = string.IsNullOrWhiteSpace(teamNamePlural) ? this.OldTeamNamePascalPlural : teamNamePlural;

            // Convert value to Camel case
            this.OldCrudNameCamelSingular = CommonTools.ConvertToCamelCase(OldCrudNamePascalSingular);
            this.OldCrudNameCamelPlural = CommonTools.ConvertToCamelCase(OldCrudNamePascalPlural);
            this.OldOptionNameCamelSingular = CommonTools.ConvertToCamelCase(OldOptionNamePascalSingular);
            this.OldOptionNameCamelPlural = CommonTools.ConvertToCamelCase(OldOptionNamePascalPlural);
            this.OldTeamNameCamelSingular = CommonTools.ConvertToCamelCase(OldTeamNamePascalSingular);
            this.OldTeamNameCamelPlural = CommonTools.ConvertToCamelCase(OldTeamNamePascalPlural);
        }

        public string GenerateCrudFiles(Project currentProject, EntityInfo crudDtoEntity, List<ZipFeatureType> zipFeatureTypeList, string displayItem, List<string> options, bool generateInProjectFolder = true)
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

                // *** Generate DotNet files ***
                // Generate CRUD DotNet files
                ZipFeatureType crudBackFeatureType = zipFeatureTypeList.Where(x => x.GenerationType == GenerationType.WebApi && x.FeatureType == FeatureType.CRUD).FirstOrDefault();
                if (crudBackFeatureType != null && crudBackFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate DotNet files on '{dotnetDir}' ***", "Green");
                    GenerateBack(dotnetDir, crudBackFeatureType.FeatureDataList, currentProject, crudDtoProperties, displayItem, FeatureType.CRUD, crudDtoEntity);
                }

                // Generate Option DotNet files
                ZipFeatureType optionBackFeatureType = zipFeatureTypeList.Where(x => x.GenerationType == GenerationType.WebApi && x.FeatureType == FeatureType.Option).FirstOrDefault();
                if (optionBackFeatureType != null && optionBackFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate DotNet Option files on '{angularDir}' ***", "Green");
                    GenerateBack(dotnetDir, optionBackFeatureType.FeatureDataList, currentProject, crudDtoProperties, displayItem, FeatureType.Option, crudDtoEntity);
                }

                // *** Generate Angular files ***
                // Generate CRUD Angular files
                ZipFeatureType crudFrontFeatureType = zipFeatureTypeList.Where(x => x.GenerationType == GenerationType.Front && x.FeatureType == FeatureType.CRUD).FirstOrDefault();
                if (crudFrontFeatureType != null && crudFrontFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate Angular CRUD files on '{angularDir}' ***", "Green");

                    if (crudDtoProperties == null)
                    {
                        consoleWriter.AddMessageLine($"Can't generate Angular CRUD files: Dto properties are empty.", "Red");
                    }
                    else
                    {
                        WebApiFeatureData dtoRefFeature = (WebApiFeatureData)crudBackFeatureType?.FeatureDataList?.FirstOrDefault(f => ((WebApiFeatureData)f).FileType == WebApiFileType.Dto);
                        GenerateFrontCRUD(angularDir, crudFrontFeatureType.FeatureDataList, currentProject, crudDtoProperties, crudDtoEntity, dtoRefFeature?.PropertiesInfos, displayItem, options);
                    }
                }

                // Generate Option Angular files
                ZipFeatureType optionFrontFeatureType = zipFeatureTypeList.Where(x => x.GenerationType == GenerationType.Front && x.FeatureType == FeatureType.Option).FirstOrDefault();
                if (optionFrontFeatureType != null && optionFrontFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate Angular Option files on '{angularDir}' ***", "Green");
                    GenerateFrontOption(angularDir, optionFrontFeatureType.FeatureDataList, crudDtoEntity);
                }

                // Generate Team Angular files
                ZipFeatureType teamFeatureType = zipFeatureTypeList.Where(x => x.FeatureType == FeatureType.Team).FirstOrDefault();
                if (teamFeatureType != null && teamFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"Team generation not yet implemented!", "Orange");
                    // GenerateTeam(angularDir, teamFeatureType.FeatureDataList);
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in CRUD generation process: {ex.Message}", "Red");
            }

            return generationFolder;
        }

        #region Feature
        private void GenerateBack(string destDir, List<FeatureData> featureDataList, Project currentProject, List<CrudProperty> crudDtoProperties,
            string displayItem, FeatureType type, EntityInfo crudDtoEntity)
        {
            try
            {
                string srcDir = Path.Combine(GetGenerationFolder(currentProject), Constants.FolderDotNet);
                ClassDefinition classDefiniton = ((WebApiFeatureData)featureDataList.FirstOrDefault(x => ((WebApiFeatureData)x).FileType == WebApiFileType.Controller))?.ClassFileDefinition;
                List<WebApiNamespace> crudNamespaceList = ListCrudNamespaces(destDir, featureDataList, currentProject, classDefiniton, type);

                // Generate files
                foreach (WebApiFeatureData crudData in featureDataList.Where(ft => !ft.IsPartialFile))
                {
                    if (crudData.FileType == WebApiFileType.Dto ||
                        crudData.FileType == WebApiFileType.Entity ||
                        crudData.FileType == WebApiFileType.Mapper)
                    {
                        // Ignore file : not necessary to regenerate it
                        continue;
                    }

                    // Update files (not partial)
                    UpdateBackFile(destDir, currentProject, crudData, classDefiniton, crudNamespaceList, crudDtoProperties, displayItem, type, crudDtoEntity);
                }

                // Update partial files
                foreach (WebApiFeatureData crudData in featureDataList.Where(ft => ft.IsPartialFile))
                {
                    // Update with partial value
                    UpdatePartialFile(srcDir, destDir, currentProject, crudData, type, classDefiniton);
                }

            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in DotNet CRUD generation process: {ex.Message}", "Red");
            }
        }

        private void GenerateFrontCRUD(string angularDir, List<FeatureData> featureDataList, Project currentProject, List<CrudProperty> crudDtoProperties, EntityInfo crudDtoEntity,
            List<PropertyInfo> dtoRefProperties, string displayItem, List<string> optionItem)
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
                        GenerationCrudData generationData = ExtractGenerationCrudData(crudData, crudDtoProperties, dtoRefProperties, displayItem, optionItem);

                        // Create file
                        string src = Path.Combine(crudData.ExtractDirPath, crudData.FilePath);
                        string dest = ConvertCamelToKebabCrudName(Path.Combine(angularDir, crudData.FilePath), FeatureType.CRUD);

                        // Replace blocks
                        GenerateAngularFile(FeatureType.CRUD, src, dest, crudDtoEntity, generationData);
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in Angular CRUD generation process: {ex.Message}", "Red");
            }
        }

        private void GenerateFrontOption(string angularDir, List<FeatureData> featureDataList, EntityInfo crudDtoEntity)
        {
            try
            {
                foreach (FeatureData angularFile in featureDataList)
                {
                    // Create file
                    string src = Path.Combine(angularFile.ExtractDirPath, angularFile.FilePath);
                    string dest = ConvertCamelToKebabCrudName(Path.Combine(angularDir, angularFile.FilePath), FeatureType.Option);

                    // replace blocks 
                    GenerateAngularFile(FeatureType.Option, src, dest, crudDtoEntity);
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

        private GenerationCrudData ExtractGenerationCrudData(FeatureData crudData, List<CrudProperty> crudDtoProperties, List<PropertyInfo> dtoRefProperties,
            string displayItem, List<string> optionItem = null)
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
                            generationData.IsOptionFound |= block.BlockLines.Count > 0;
                            if (!string.IsNullOrWhiteSpace(block.Name) && !generationData.OptionsName.Contains(block.Name))
                                generationData.OptionsName.Add(block.Name);
                            generationData.OptionToAdd = optionItem;
                            break;
                        case CRUDDataUpdateType.Display:
                            string extractItem = ((ExtractDisplayBlock)block).ExtractItem;
                            string extractLine = ((ExtractDisplayBlock)block).ExtractLine;
                            string newDisplayLine = extractLine.Replace(extractItem, CommonTools.ConvertToCamelCase(displayItem));
                            generationData.DisplayToUpdate.Add(new KeyValuePair<string, string>(extractLine, newDisplayLine));
                            break;
                        case CRUDDataUpdateType.AncestorTeam:
                            generationData.IsAncestorFound |= block.BlockLines.Count > 0;
                            generationData.AncestorName.Add(block.Name);
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
                    PrepareParentBlock(parentBlocks, generationData, crudDtoProperties);
                }
            }

            return generationData;
        }


        #region DotNet Files
        private void UpdateBackFile(string destDir, Project currentProject, WebApiFeatureData crudData, ClassDefinition dtoClassDefiniton, List<WebApiNamespace> crudNamespaceList,
            List<CrudProperty> crudDtoProperties, string displayItem, FeatureType type, EntityInfo crudDtoEntity)
        {
            string src = Path.Combine(crudData.ExtractDirPath, crudData.FilePath);

            string fileName = Path.GetFileName(crudData.FilePath);
            string partFilePath = crudData.FilePath.Remove(crudData.FilePath.LastIndexOf(fileName));
            partFilePath = ReplaceCompagnyNameProjetName(partFilePath, currentProject, dtoClassDefiniton);

            string tmpDestDir = ConvertPascalOldToNewCrudName(destDir, type, false);
            string tmpPartFilePath = ConvertPascalOldToNewCrudName(partFilePath, FeatureType.CRUD, false);
            string tmpFileName = ConvertPascalOldToNewCrudName(fileName, type, false);
            string dest = Path.Combine(tmpDestDir, tmpPartFilePath, tmpFileName);

            //string dest = Path.Combine(destDir, partFilePath, fileName);
            //dest = ConvertPascalOldToNewCrudName(dest, type, false);

            // Prepare destination folder
            CommonTools.CheckFolder(new FileInfo(dest).DirectoryName);

            GenerationCrudData generationData = ExtractGenerationCrudData(crudData, crudDtoProperties, null, displayItem);

            // Read file
            List<string> fileLinesContent = File.ReadAllLines(src).ToList();

            fileLinesContent = UpdateFileContent(fileLinesContent, generationData, src, crudDtoEntity);

            for (int i = 0; i < fileLinesContent.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(fileLinesContent[i])) continue;

                if (CommonTools.IsNamespaceOrUsingLine(fileLinesContent[i]))
                {
                    fileLinesContent[i] = UpdateNamespaceUsing(fileLinesContent[i], currentProject, dtoClassDefiniton, crudNamespaceList);
                }
                else
                {
                    // Convert Crud Name (Plane to XXX)
                    fileLinesContent[i] = ConvertPascalOldToNewCrudName(fileLinesContent[i], type, false);
                }
            }

            // Generate new file
            CommonTools.GenerateFile(dest, fileLinesContent);
        }

        private List<WebApiNamespace> ListCrudNamespaces(string destDir, List<FeatureData> featureDataList, Project currentProject, ClassDefinition classDefiniton, FeatureType type)
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
                    partPath = ReplaceCompagnyNameProjetName(partPath, currentProject, classDefiniton);

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
                        webApiNamespace.CrudNamespaceGenerated = ReplaceCompagnyNameProjetName(crudData.Namespace, currentProject, classDefiniton).Replace(OldCrudNamePascalSingular, NewCrudNamePascalSingular);
                    }
                }
                else
                {
                    // Generate namespace for generated files: Controller, (I)AppService ...

                    webApiNamespace.CrudNamespaceGenerated = ReplaceCompagnyNameProjetName(crudData.Namespace, currentProject, classDefiniton).Replace(OldCrudNamePascalSingular, NewCrudNamePascalSingular);
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
                        line = line.Replace(OldCrudNamePascalSingular, NewCrudNamePascalSingular);
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

            string partialName = "";
            switch (type)
            {
                case FeatureType.CRUD:
                    partialName = OldCrudNamePascalSingular;
                    break;
                case FeatureType.Option:
                    partialName = OldOptionNamePascalSingular;
                    break;
                case FeatureType.Team:
                    partialName = OldTeamNamePascalSingular;
                    break;
            }
            foreach (ExtractPartialBlock block in crudData.ExtractBlocks.Where(b => b.Name == partialName))
            {
                contentToAdd = new();
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
                        string newline = ReplaceOldCamelToNewKebabPath(line, type);
                        if (dtoClassDefiniton != null)
                        {
                            newline = ReplaceCompagnyNameProjetName(newline, currentProject, dtoClassDefiniton);
                        }
                        contentToAdd.Add(newline);
                    });

                    // Update file
                    UpdatePartialCrudFile(srcFile, destFile, contentToAdd, markerBegin, markerEnd);
                }
            }
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
        private void GenerateAngularFile(FeatureType type, string fileName, string newFileName, EntityInfo crudDtoEntity, GenerationCrudData generationData = null)
        {
            if (!File.Exists(fileName))
            {
                consoleWriter.AddMessageLine($"Error on generating angular CRUD: file not exist on disk: '{fileName}'", "Orange");
                return;
            }

            // Prepare destination folder
            CommonTools.CheckFolder(new FileInfo(newFileName).DirectoryName);

            // Read file
            List<string> fileLinesContent = File.ReadAllLines(fileName).ToList();

            // Update file content
            fileLinesContent = UpdateFileLinesContent(fileLinesContent, type);
            fileLinesContent = UpdateFileContent(fileLinesContent, generationData, fileName, crudDtoEntity);

            // Generate new file
            CommonTools.GenerateFile(newFileName, fileLinesContent);
        }

        private List<string> UpdateFileContent(List<string> fileLinesContent, GenerationCrudData generationData, string fileName, EntityInfo crudDtoEntity)
        {
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

                // Update options blocks
                if (generationData.IsOptionFound)
                {
                    fileLinesContent = ManageOptionsBlocks(fileLinesContent, generationData.OptionsName, generationData.OptionToAdd);
                }

                // Update parent blocks
                if (generationData.ParentBlocks?.Count > 0)
                {
                    fileLinesContent = UpdateParentBlocks(fileName, fileLinesContent, generationData.ParentBlocks, generationData.IsParentToAdd);
                }

                // Remove Ancestor
                if (generationData.IsAncestorFound)
                {
                    fileLinesContent = ManageAncestorBlocks(fileLinesContent, generationData.AncestorName, crudDtoEntity.ClassAnnotations);
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
            }

            return UpdateBlocks(fileName, fileLinesContent, newBlockList, CRUDDataUpdateType.Block);
        }

        private List<string> ReplaceProperties(string fileName, List<string> fileLinesContent, List<string> propertiesToAdd)
        {
            int indexBeginProperty = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains($"{ZipParserService.MARKER_BEGIN} {CRUDDataUpdateType.Properties}")).FirstOrDefault());
            int indexEndProperty = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains($"{ZipParserService.MARKER_END} {CRUDDataUpdateType.Properties}")).LastOrDefault());
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

        private List<string> ManageAncestorBlocks(List<string> fileLinesContent, List<string> ancestorName, List<KeyValuePair<string, string>> classAnnotations)
        {
            string beginMarker = $"{ZipParserService.MARKER_BEGIN} {CRUDDataUpdateType.AncestorTeam}";
            string endMarker = $"{ZipParserService.MARKER_END} {CRUDDataUpdateType.AncestorTeam}";

            if (classAnnotations == null)
            {
                // delete
                ancestorName?.ForEach(ancestor =>
                {
                    fileLinesContent = DeleteBlocks(fileLinesContent, beginMarker, endMarker);
                });
            }
            else
            {
                string ancestorAnnotation = classAnnotations.Where(c => c.Key == CRUDDataUpdateType.AncestorTeam.ToString())?.FirstOrDefault().Value;
                if (!string.IsNullOrWhiteSpace(ancestorAnnotation))
                {
                    // delete
                    ancestorName?.ForEach(ancestor =>
                    {
                        if (ancestor != null && ancestor != ancestorAnnotation)
                        {
                            string markerBegin = $"{beginMarker} {ancestor}";
                            string markerEnd = $"{endMarker} {ancestor}";
                            fileLinesContent = DeleteBlocks(fileLinesContent, beginMarker, endMarker);
                        }
                    });
                }
            }

            return fileLinesContent;
        }

        private List<string> ManageOptionsBlocks(List<string> fileLinesContent, List<string> optionsName, List<string> newOptionsName)
        {
            // Delete Options
            if (newOptionsName == null || !newOptionsName.Any())
            {
                return DeleteOptionsBlocks(fileLinesContent, optionsName);
            }

            // Keep only "newOptionName"
            foreach (string optionName in optionsName)
            {
                string markerBegin = $"{ZipParserService.MARKER_BEGIN} {CRUDDataUpdateType.Option} {optionName}";
                string markerEnd = $"{ZipParserService.MARKER_END} {CRUDDataUpdateType.Option} {optionName}";

                if (optionName == OldOptionNamePascalSingular)
                {
                    // replace
                    fileLinesContent = ReplaceOptions(fileLinesContent, optionName, newOptionsName, markerBegin, markerEnd);
                }
                else
                {
                    // delete
                    fileLinesContent = DeleteBlocks(fileLinesContent, markerBegin, markerEnd);
                }
            }

            return fileLinesContent;
        }

        private List<string> DeleteOptionsBlocks(List<string> fileLinesContent, List<string> optionsName)
        {
            string beginMarker = $"{ZipParserService.MARKER_BEGIN} {CRUDDataUpdateType.Option}";
            string endMarker = $"{ZipParserService.MARKER_END} {CRUDDataUpdateType.Option}";

            optionsName?.ForEach(optionName =>
            {
                string markerBegin = $"/* {beginMarker} ";
                string markerEnd = $"{endMarker} */";

                fileLinesContent = DeleteBlocks(fileLinesContent, markerBegin, markerEnd);
            });

            return DeleteBlocks(fileLinesContent, CRUDDataUpdateType.Option);
        }

        private List<string> ReplaceOptions(List<string> fileLinesContent, string oldOptionName, List<string> newOptions, string markerBegin, string markerEnd)
        {
            string line;
            bool endFound;
            List<string> newLines = new();

            for (int i = 0; i < fileLinesContent.Count; i++)
            {
                line = fileLinesContent[i];

                if (line.Contains(markerBegin, StringComparison.InvariantCultureIgnoreCase))
                {
                    endFound = false;
                    List<string> optionBlock = new();

                    for (int j = i; j < fileLinesContent.Count && !endFound; j++)
                    {
                        line = fileLinesContent[j];
                        optionBlock.Add(line);

                        if (line.Contains(markerEnd, StringComparison.InvariantCultureIgnoreCase))
                        {
                            endFound = true;
                            i = j;
                        }
                    }

                    foreach (string newOptionName in newOptions)
                    {
                        string oldOptionNameKebab = CommonTools.ConvertPascalToKebabCase(oldOptionName);
                        string newOptionNameKebab = CommonTools.ConvertPascalToKebabCase(newOptionName);
                        foreach (string optionLine in optionBlock)
                        {
                            string newLine = optionLine.Replace(oldOptionName, newOptionName).Replace(oldOptionNameKebab, newOptionNameKebab);
                            newLines.Add(newLine);
                        }
                    }
                }
                else
                {
                    newLines.Add(line);
                }
            }
            if (newLines.Any())
            {
                fileLinesContent = newLines;
            }

            return fileLinesContent;
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
                indexEnd = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains($"{ZipParserService.MARKER_END} {type}")).LastOrDefault());
            }
            else
            {
                indexBegin = CommonTools.IndexOfOccurence(fileLinesContent, $"{ZipParserService.MARKER_BEGIN} {type}", count);
                indexEnd = CommonTools.IndexOfOccurence(fileLinesContent, $"{ZipParserService.MARKER_END} {type}", count);
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

        private List<List<string>> GenerateParentBlocks(List<ExtractBlock> blocksList, List<CrudProperty> crudDtoProperties)
        {
            List<CrudProperty> parents = crudDtoProperties.Where(c => c.IsParent).ToList();
            if (!parents.Any())
            {
                return null;
            }

            string parentName = blocksList.Where(b => !string.IsNullOrWhiteSpace(b.Name)).FirstOrDefault()?.Name;
            string newParentName = parents.Where(p => p.IsParent).FirstOrDefault()?.Name;
            string parentNamePascal = CommonTools.ConvertToPascalCase(parentName);
            string parentNameCamel = CommonTools.ConvertToCamelCase(parentName);
            string newParentNamePascal = CommonTools.ConvertToPascalCase(newParentName);
            string newParentNameCamel = CommonTools.ConvertToCamelCase(newParentName);

            List<List<string>> blocksToAdd = new();
            foreach (ExtractBlock block in blocksList)
            {
                List<string> newBlock = new();
                block.BlockLines.ForEach(l =>
                {
                    string line = l;
                    if (!string.IsNullOrWhiteSpace(parentNamePascal))
                    {
                        line = line.Replace(parentNamePascal, newParentNamePascal);
                    }
                    if (!string.IsNullOrWhiteSpace(parentNameCamel))
                    {
                        line = line.Replace(parentNameCamel, newParentNameCamel);
                    }
                    newBlock.Add(line);
                });
                blocksToAdd.Add(newBlock);
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

        private List<string> UpdateFileLinesContent(List<string> lines, FeatureType type)
        {
            const string startImportRegex = @"^\s*[\/\*]*\s*import\s+";
            const string regexComponent = @"^\s*[\/\*]*\s*import\s+{([\s\w,]*)}";
            const string regexComponentAs = @"^\s*[\/\*]*\s*import\s+\*\s+as\s+([\w,]*)\s+from";
            const string regexPath = @"\s*from\s*\'([\S]*)\';$";

            bool markerFound = false;
            for (int i = 0; i < lines?.Count; i++)
            {
                if (lines[i].Contains(ZipParserService.MARKER_BEGIN))
                {
                    markerFound = true;
                    continue;
                }
                else if (lines[i].Contains(ZipParserService.MARKER_END))
                {
                    markerFound = false;
                    continue;
                }
                else if (markerFound)
                {
                    // ignore lines between markers
                    continue;
                }

                if (CommonTools.IsMatchRegexValue(startImportRegex, lines[i]))
                {
                    // Update part between "import" and "from"
                    string componentValue = CommonTools.GetMatchRegexValue(regexComponent, lines[i], 1);
                    if (string.IsNullOrEmpty(componentValue))
                    {
                        componentValue = CommonTools.GetMatchRegexValue(regexComponentAs, lines[i], 1);
                    }
                    if (!string.IsNullOrEmpty(componentValue))
                    {
                        string newComponentValue = ConvertPascalOldToNewCrudName(componentValue, type);
                        lines[i] = lines[i].Replace(componentValue, newComponentValue);
                    }

                    // Update part after "from"
                    string pathValue = CommonTools.GetMatchRegexValue(regexPath, lines[i], 1);
                    if (!string.IsNullOrEmpty(pathValue))
                    {
                        string newPathValue = ReplaceOldCamelToNewKebabPath(pathValue, type);
                        lines[i] = lines[i].Replace(pathValue, newPathValue);
                    }
                }
                else
                {
                    lines[i] = ReplaceOldCamelToNewKebabPath(lines[i], type);
                }
            }

            return lines;
        }

        private void PrepareParentBlock(List<ExtractBlock> parentBlocks, GenerationCrudData generationData, List<CrudProperty> crudDtoProperties)
        {
            List<List<string>> blocks = GenerateParentBlocks(parentBlocks, crudDtoProperties);
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

        private string ConvertPascalOldToNewCrudName(string value, FeatureType type, bool convertCamel = true)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            switch (type)
            {
                //case FeatureType.WebApi:
                case FeatureType.CRUD:
                    value = ReplaceOldToNewValue(value, OldCrudNamePascalPlural, NewCrudNamePascalPlural, OldCrudNamePascalSingular, NewCrudNamePascalSingular);
                    if (convertCamel)
                    {
                        value = ReplaceOldToNewValue(value, OldCrudNameCamelPlural, NewCrudNameCamelPlural, OldCrudNameCamelSingular, NewCrudNameCamelSingular);
                    }
                    break;
                case FeatureType.Option:
                    value = ReplaceOldToNewValue(value, OldOptionNamePascalPlural, NewCrudNamePascalPlural, OldOptionNamePascalSingular, NewCrudNamePascalSingular);
                    if (convertCamel)
                    {
                        value = ReplaceOldToNewValue(value, OldOptionNameCamelPlural, NewCrudNameCamelPlural, OldOptionNameCamelSingular, NewCrudNameCamelSingular);
                    }
                    break;
                case FeatureType.Team:
                    value = ReplaceOldToNewValue(value, OldTeamNamePascalPlural, NewCrudNamePascalPlural, OldTeamNamePascalSingular, NewCrudNamePascalSingular);
                    if (convertCamel)
                    {
                        value = ReplaceOldToNewValue(value, OldTeamNameCamelPlural, NewCrudNameCamelPlural, OldTeamNameCamelSingular, NewCrudNameCamelSingular);
                    }
                    break;
            }

            return value;
        }

        private string ReplaceOldToNewValue(string value, string oldValuePlural, string newValuePlural, string oldValueSingular, string newValueSingular)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            List<int> nbAllOccurOldValue = FindOccurences(value, oldValueSingular);
            List<int> nbOccurOldValuePlural = FindOccurences(value, oldValuePlural);
            List<int> nbOccurOldValueSingular = nbAllOccurOldValue.Except(nbOccurOldValuePlural).ToList();

            foreach (int index in nbAllOccurOldValue.OrderByDescending(i => i))
            {
                string before = value[..index];
                if (nbOccurOldValuePlural.Contains(index))
                {
                    string after = value[(index + oldValuePlural.Length)..];
                    value = $"{before}{newValuePlural}{after}";
                }
                else if (nbOccurOldValueSingular.Contains(index))
                {
                    string after = value[(index + oldValueSingular.Length)..];
                    value = $"{before}{newValueSingular}{after}";
                }
            }

            return value;
        }


        private List<int> FindOccurences(string line, string search)
        {
            int lastIndex = 0;
            List<int> indexList = new();
            for (int count = 0; line.Length > 0; count++)
            {
                int index = line.IndexOf(search);
                if (index < 0)
                    break;
                else
                {
                    indexList.Add(lastIndex + index);
                    lastIndex += index + search.Length;
                    line = line.Substring(index + search.Length);
                }
            }

            return indexList;
        }

        private string ReplaceOldCamelToNewKebabPath(string value, FeatureType type)
        {
            if (value.Contains($"/{OldCrudNameCamelPlural}") || value.Contains($"/{OldCrudNameCamelSingular}"))
            {
                value = ReplaceOldToNewValue(value, OldCrudNameCamelPlural, NewCrudNameKebabPlural, OldCrudNameCamelSingular, NewCrudNameKebabSingular);
                value = ConvertPascalOldToNewCrudName(value, type, false);
            }
            else
            {
                value = ConvertPascalOldToNewCrudName(value, type);
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
                    value = ReplaceOldToNewValue(value, this.OldCrudNameCamelPlural, this.NewCrudNameKebabPlural, this.OldCrudNameCamelSingular, this.NewCrudNameKebabSingular);
                    break;
                case FeatureType.Option:
                    value = ReplaceOldToNewValue(value, this.OldOptionNameCamelPlural, this.NewCrudNameKebabPlural, this.OldOptionNameCamelSingular, this.NewCrudNameKebabSingular);
                    break;
                case FeatureType.Team:
                    value = ReplaceOldToNewValue(value, this.OldTeamNameCamelPlural, this.NewCrudNameKebabPlural, this.OldTeamNameCamelSingular, this.NewCrudNameKebabSingular);
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
        public bool IsOptionFound { get; set; }
        public List<string> OptionsName { get; }
        public List<string> OptionToAdd { get; set; }
        public List<KeyValuePair<string, string>> DisplayToUpdate { get; }
        public bool IsParentToAdd { get; set; }
        public List<List<string>> ParentBlocks { get; }
        public bool IsAncestorFound { get; set; }
        public List<string> AncestorName { get; }

        public GenerationCrudData()
        {
            this.IsChildrenToDelete = false;
            this.IsOptionFound = false;
            this.IsAncestorFound = false;
            this.BlocksToAdd = new();
            this.PropertiesToAdd = new();
            this.ChildrenName = new();
            this.OptionsName = new();
            this.DisplayToUpdate = new();
            this.ParentBlocks = new();
            this.AncestorName = new();
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
