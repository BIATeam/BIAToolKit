namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
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
        internal const string BIA_DTO_FIELD_ITEMTYPE = "ItemType";

        private const string ATTRIBUTE_TYPE_NOT_MANAGED = $"// CRUD GENERATOR FOR {NEW_CRUD_NAME_SINGULAR_UPPER} TO REVIEW : Field {ATTRIBUTE_TYPE_NOT_MANAGED_FIELD} or type {ATTRIBUTE_TYPE_NOT_MANAGED_TYPE} not managed.";
        private const string NEW_CRUD_NAME_SINGULAR_UPPER = "WWWNameWWW";
        private const string ATTRIBUTE_TYPE_NOT_MANAGED_FIELD = "XXXFieldXXX";
        private const string ATTRIBUTE_TYPE_NOT_MANAGED_TYPE = "YYYTypeYYY";
        private const string IS_REQUIRED_PROPERTY = "isRequired:";

        private readonly IConsoleWriter consoleWriter;
        private string DotNetFolderGeneration;
        private string AngularFolderGeneration;

        public CrudNames CrudNames { get; set; }

        private Project currentProject;
        public Project CurrentProject
        {
            get => currentProject;
            set
            {
                currentProject = value;
                string generationFolder = GetGenerationFolder(currentProject);
                this.DotNetFolderGeneration = Path.Combine(generationFolder, Constants.FolderDotNet);
                this.AngularFolderGeneration = Path.Combine(generationFolder, CurrentProject.BIAFronts);
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GenerateCrudService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public bool GenerateFiles(EntityInfo crudDtoEntity, List<ZipFeatureType> zipFeatureTypeList,
                                        string displayItem, List<string> options)
        {
            try
            {
                // Get CRUD dto properties
                List<CrudProperty> crudDtoProperties = GetDtoProperties(crudDtoEntity);

                // *** Generate DotNet files ***
                // Generate CRUD DotNet files
                ZipFeatureType crudBackFeatureType = zipFeatureTypeList.Where(x => x.GenerationType == GenerationType.WebApi && x.FeatureType == FeatureType.CRUD).FirstOrDefault();
                if (crudBackFeatureType != null && crudBackFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate DotNet files on '{DotNetFolderGeneration}' ***", "Green");
                    GenerateWebApi(crudBackFeatureType.FeatureDataList, currentProject, crudDtoProperties, displayItem, FeatureType.CRUD, crudDtoEntity);
                }

                // Generate Option DotNet files
                ZipFeatureType optionBackFeatureType = zipFeatureTypeList.Where(x => x.GenerationType == GenerationType.WebApi && x.FeatureType == FeatureType.Option).FirstOrDefault();
                if (optionBackFeatureType != null && optionBackFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate DotNet Option files on '{DotNetFolderGeneration}' ***", "Green");
                    GenerateWebApi(optionBackFeatureType.FeatureDataList, currentProject, crudDtoProperties, displayItem, FeatureType.Option, crudDtoEntity);
                }

                // Generate Team DotNet files
                ZipFeatureType teamFeatureType = zipFeatureTypeList.Where(x => x.FeatureType == FeatureType.Team && x.GenerationType == GenerationType.WebApi).FirstOrDefault();
                if (teamFeatureType != null && teamFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"Team DotNet generation implementation WIP !", "Orange");
                    GenerateWebApi(teamFeatureType.FeatureDataList, currentProject, crudDtoProperties, displayItem, FeatureType.Team, crudDtoEntity);
                }

                // *** Generate Angular files ***
                // Generate CRUD Angular files
                ZipFeatureType crudFrontFeatureType = zipFeatureTypeList.Where(x => x.GenerationType == GenerationType.Front && x.FeatureType == FeatureType.CRUD).FirstOrDefault();
                if (crudFrontFeatureType != null && crudFrontFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate Angular CRUD files on '{AngularFolderGeneration}' ***", "Green");

                    if (crudDtoProperties == null)
                    {
                        consoleWriter.AddMessageLine($"Can't generate Angular CRUD files: Dto properties are empty.", "Red");
                    }
                    else
                    {
                        WebApiFeatureData dtoRefFeature = (WebApiFeatureData)crudBackFeatureType?.FeatureDataList?.FirstOrDefault(f => ((WebApiFeatureData)f).FileType == WebApiFileType.Dto);
                        GenerateFrontCRUD(crudFrontFeatureType.FeatureDataList, currentProject, crudDtoProperties, crudDtoEntity, dtoRefFeature?.PropertiesInfos, displayItem, options);
                    }
                }

                // Generate Option Angular files
                ZipFeatureType optionFrontFeatureType = zipFeatureTypeList.Where(x => x.GenerationType == GenerationType.Front && x.FeatureType == FeatureType.Option).FirstOrDefault();
                if (optionFrontFeatureType != null && optionFrontFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate Angular Option files on '{AngularFolderGeneration}' ***", "Green");
                    GenerateFrontOption(optionFrontFeatureType.FeatureDataList, crudDtoEntity);
                }

            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in CRUD generation process: {ex.Message}", "Red");
                return false;
            }

            return true;
        }

        public void DeleteLastGeneration(List<ZipFeatureType> zipFeatureTypeList, Project currentProject, CRUDGenerationHistory generationHistory, List<CRUDGenerationHistory> optionsGenerationHistory)
        {
            DeleteCrudOptionsGenerated(zipFeatureTypeList, currentProject, generationHistory);

            DeleteCrudOptionsGenerated(zipFeatureTypeList, generationHistory, optionsGenerationHistory);
        }

        public void DeleteBIAToolkitAnnotations(List<string> folders)
        {
            string markerBegin = $"{ZipParserService.MARKER_BEGIN}";
            string markerEnd = $"{ZipParserService.MARKER_END}";
            string markerPartialBegin = $"{ZipParserService.MARKER_BEGIN_PARTIAL}";
            string markerPartialEnd = $"{ZipParserService.MARKER_END_PARTIAL}";

            foreach (string parentFolder in folders)
            {
                // Filter directories to explore
                List<string> subFolders = Directory.GetDirectories(parentFolder, "*", SearchOption.TopDirectoryOnly).Where(f => !new DirectoryInfo(f).Name.StartsWith('.')).ToList();
                foreach (string folder in subFolders)
                {
                    // Get all files in directories
                    foreach (string file in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
                    {
                        // Read file lines
                        List<string> content = File.ReadAllLines(file).ToList();
                        if (content.Any(line => line.Contains(markerBegin)))
                        {
                            List<string> newContent = content;
                            newContent = newContent.Where(line => !line.Contains(markerBegin) || line.Contains(markerBegin) && !line.Contains(markerPartialBegin)).ToList();
                            newContent = newContent.Where(line => !line.Contains(markerEnd) || line.Contains(markerEnd) && !line.Contains(markerPartialEnd)).ToList();
                            CommonTools.GenerateFile(file, newContent);
                        }
                    }
                }
            }
        }

        #region Feature
        private void GenerateWebApi(List<FeatureData> featureDataList, Project currentProject, List<CrudProperty> crudDtoProperties, string displayItem,
            FeatureType type, EntityInfo crudDtoEntity)
        {
            try
            {
                ClassDefinition classDefiniton = ((WebApiFeatureData)featureDataList.FirstOrDefault(x => ((WebApiFeatureData)x).FileType == WebApiFileType.Controller))?.ClassFileDefinition;
                List<WebApiNamespace> crudNamespaceList = ListCrudNamespaces(this.DotNetFolderGeneration, featureDataList, currentProject, classDefiniton, type);

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
                    UpdateBackFile(currentProject, crudData, classDefiniton, crudNamespaceList, crudDtoProperties, displayItem, type, crudDtoEntity);
                }

                // Update partial files
                foreach (WebApiFeatureData crudData in featureDataList.Where(ft => ft.IsPartialFile))
                {
                    // Update with partial value
                    UpdatePartialFile(this.DotNetFolderGeneration, currentProject, crudData, type, classDefiniton);
                }

            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in DotNet CRUD generation process: {ex.Message}", "Red");
            }
        }

        private void GenerateFrontCRUD(List<FeatureData> featureDataList, Project currentProject, List<CrudProperty> crudDtoProperties, EntityInfo crudDtoEntity,
            List<PropertyInfo> dtoRefProperties, string displayItem, List<string> optionItem)
        {
            const FeatureType type = FeatureType.CRUD;
            try
            {
                List<CRUDPropertyType> propertyList = ((ExtractPropertiesBlock)featureDataList.
                    FirstOrDefault(f => f.IsPropertyFile)?.ExtractBlocks.
                    FirstOrDefault(b => b.DataUpdateType == CRUDDataUpdateType.Properties))?.PropertiesList;

                foreach (FeatureData crudData in featureDataList.OrderByDescending(f => f.IsPropertyFile))
                {
                    if (crudData.IsPartialFile)
                    {
                        // Update with partial file
                        UpdatePartialFile(this.AngularFolderGeneration, currentProject, crudData, type);
                    }
                    else
                    {
                        GenerationCrudData generationData = ExtractGenerationCrudData(crudData, crudDtoProperties, dtoRefProperties, displayItem, type, optionItem);

                        // Create file
                        (string src, string dest) = GetAngularFilesPath(crudData, type);

                        // Replace blocks
                        GenerateAngularFile(type, src, dest, crudDtoEntity, generationData, crudDtoProperties, propertyList);
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in Angular CRUD generation process: {ex.Message}", "Red");
            }
        }

        private void GenerateFrontOption(List<FeatureData> featureDataList, EntityInfo crudDtoEntity)
        {
            const FeatureType type = FeatureType.Option;
            try
            {
                foreach (FeatureData featureData in featureDataList)
                {
                    // Create file
                    (string src, string dest) = GetAngularFilesPath(featureData, type);

                    // replace blocks 
                    GenerateAngularFile(type, src, dest, crudDtoEntity);
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

        #region DotNet Files
        private void UpdateBackFile(Project currentProject, WebApiFeatureData crudData, ClassDefinition dtoClassDefiniton, List<WebApiNamespace> crudNamespaceList,
            List<CrudProperty> crudDtoProperties, string displayItem, FeatureType type, EntityInfo crudDtoEntity)
        {
            (string src, string dest) = GetDotNetFilesPath(currentProject, crudData, dtoClassDefiniton, type);

            // Prepare destination folder
            CommonTools.CheckFolder(new FileInfo(dest).DirectoryName);

            GenerationCrudData generationData = ExtractGenerationCrudData(crudData, crudDtoProperties, null, displayItem, type);

            // Read file
            List<string> fileLinesContent = File.ReadAllLines(src).ToList();

            fileLinesContent = UpdateFileContent(fileLinesContent, generationData, src, crudDtoEntity);

            for (int i = 0; i < fileLinesContent.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(fileLinesContent[i])) continue;

                if (CommonTools.IsNamespaceOrUsingLine(fileLinesContent[i]))
                {
                    fileLinesContent[i] = UpdateNamespaceUsing(fileLinesContent[i], currentProject, dtoClassDefiniton, crudNamespaceList);
                    continue;
                }

                // Convert Crud Name (Plane to Xxx and plane to xxx)
                fileLinesContent[i] = this.CrudNames.ConvertPascalOldToNewCrudName(this.CrudNames.ConvertPascalOldToNewCrudName(fileLinesContent[i], type, false), type, true);
            }

            // Generate new file
            CommonTools.GenerateFile(dest, fileLinesContent);
        }

        private (string, string) GetDotNetFilesPath(Project currentProject, WebApiFeatureData featureData, ClassDefinition dtoClassDefiniton, FeatureType type)
        {
            string src = Path.Combine(featureData.ExtractDirPath, featureData.FilePath);

            string fileName = Path.GetFileName(featureData.FilePath);
            string filePartPath = featureData.FilePath.Remove(featureData.FilePath.LastIndexOf(fileName));
            filePartPath = ReplaceCompagnyNameProjetName(filePartPath, currentProject, dtoClassDefiniton);

            string newFilePartPath = this.CrudNames.ConvertPascalOldToNewCrudName(filePartPath, FeatureType.CRUD, false);
            string newFileName = this.CrudNames.ConvertPascalOldToNewCrudName(fileName, type, false);
            string dest = Path.Combine(this.DotNetFolderGeneration, newFilePartPath, newFileName);

            return (src, dest);
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
                    string partPath = GetNamespacePathBeforeOccurency(crudData.Namespace, type);

                    // Replace company + projet name on part path
                    partPath = ReplaceCompagnyNameProjetName(partPath, currentProject, classDefiniton);

                    // Replace "plane" file name with good "crud" value
                    string fileName = crudData.FileName.Replace(this.CrudNames.OldCrudNamePascalSingular, this.CrudNames.NewCrudNamePascalSingular);

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
                        webApiNamespace.CrudNamespaceGenerated =
                            ReplaceCompagnyNameProjetName(crudData.Namespace, currentProject, classDefiniton).
                            Replace(this.CrudNames.OldCrudNamePascalSingular, this.CrudNames.NewCrudNamePascalSingular);
                    }
                }
                else
                {
                    // Generate namespace for generated files: Controller, (I)AppService ...

                    webApiNamespace.CrudNamespaceGenerated =
                        ReplaceCompagnyNameProjetName(crudData.Namespace, currentProject, classDefiniton).
                        Replace(this.CrudNames.OldCrudNamePascalSingular, this.CrudNames.NewCrudNamePascalSingular);
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
                        line = line.Replace(this.CrudNames.OldCrudNamePascalSingular, this.CrudNames.NewCrudNamePascalSingular);
                    }
                }
            }

            return line;
        }
        #endregion

        #region Partial Files
        private void UpdatePartialFile(string workingFolder, Project currentProject, FeatureData crudData, FeatureType type, ClassDefinition dtoClassDefiniton = null)
        {
            string markerBegin, markerEnd, suffix;
            List<string> contentToAdd;

            string partialFilePath = GetPartialFilePath(crudData, dtoClassDefiniton, workingFolder);

            string partialName = "";
            switch (type)
            {
                case FeatureType.CRUD:
                    partialName = this.CrudNames.OldCrudNamePascalSingular;
                    break;
                case FeatureType.Option:
                    partialName = this.CrudNames.OldOptionNamePascalSingular;
                    break;
                case FeatureType.Team:
                    partialName = this.CrudNames.OldTeamNamePascalSingular;
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
                        string newline = UpdateFrontLines(line, type);
                        if (dtoClassDefiniton != null)
                        {
                            newline = ReplaceCompagnyNameProjetName(newline, currentProject, dtoClassDefiniton);
                        }
                        contentToAdd.Add(newline);
                    });

                    // Update file
                    UpdatePartialCrudFile(partialFilePath, contentToAdd, markerBegin, markerEnd);
                }
            }
        }

        private string GetPartialFilePath(FeatureData featureData, ClassDefinition classDefiniton, string workingFolder)
        {
            string fileName = featureData.FilePath.Replace(Constants.PartialFileSuffix, "");
            if (classDefiniton != null)
            {
                fileName = ReplaceCompagnyNameProjetName(fileName, currentProject, classDefiniton);
            }

            return Path.Combine(workingFolder, fileName);
        }

        private void UpdatePartialCrudFile(string partialFile, List<string> contentToAdd, string markerBegin, string markerEnd)
        {
            // Read file
            List<string> fileContent = File.ReadAllLines(partialFile).ToList();

            // Insert data on file content
            List<string> newContent = InsertContentBetweenMarkers(fileContent, contentToAdd, markerBegin, markerEnd);

            // Generate file with new content
            CommonTools.GenerateFile(partialFile, newContent);
        }
        #endregion

        #region Angular Files
        private void GenerateAngularFile(FeatureType type, string fileName, string newFileName, EntityInfo crudDtoEntity,
            GenerationCrudData generationData = null, List<CrudProperty> crudDtoProperties = null, List<CRUDPropertyType> propertyList = null)
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
            fileLinesContent = UpdateFileContent(fileLinesContent, generationData, fileName, crudDtoEntity, crudDtoProperties, propertyList);
            fileLinesContent = UpdateFileLinesContent(fileLinesContent, type);

            // Generate new file
            CommonTools.GenerateFile(newFileName, fileLinesContent);
        }

        private (string, string) GetAngularFilesPath(FeatureData featureData, FeatureType type)
        {
            string src = Path.Combine(featureData.ExtractDirPath, featureData.FilePath);
            string dest = this.CrudNames.ConvertCamelToKebabCrudName(Path.Combine(this.AngularFolderGeneration, featureData.FilePath), type);

            return (src, dest);
        }

        private List<string> UpdateFileContent(List<string> fileLinesContent, GenerationCrudData generationData, string fileName, EntityInfo crudDtoEntity,
            List<CrudProperty> crudDtoProperties = null, List<CRUDPropertyType> propertyList = null)
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
                    fileLinesContent = ManageOptionsBlocks(fileLinesContent, generationData.OptionsName, generationData.OptionsToAdd, generationData.OptionsFields, crudDtoProperties, propertyList);
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

        private List<string> DeleteOptionFieldBlocks(List<string> fileLinesContent, List<string> fieldToDelete = null)
        {
            string markerBegin = $"{ZipParserService.MARKER_BEGIN} {CRUDDataUpdateType.OptionField}";
            string markerEnd = $"{ZipParserService.MARKER_END} {CRUDDataUpdateType.OptionField}";

            if (fieldToDelete != null)
            {
                string beginMarker, endMarker;
                foreach (string fieldName in fieldToDelete)
                {
                    beginMarker = $"{markerBegin} {fieldName}";
                    endMarker = $"{markerEnd} {fieldName}";

                    fileLinesContent = DeleteBlocks(fileLinesContent, beginMarker, endMarker);
                }
            }
            else
            {
                fileLinesContent = DeleteBlocks(fileLinesContent, markerBegin, markerEnd);
            }

            return fileLinesContent;
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

        private List<string> ManageOptionsBlocks(List<string> fileLinesContent, List<string> optionsName, List<string> newOptionsName, List<string> optionsFields,
            List<CrudProperty> crudDtoProperties, List<CRUDPropertyType> propertyList)
        {
            if (newOptionsName == null || !newOptionsName.Any())
            {
                // Delete options blocks
                fileLinesContent = DeleteOptionsBlocks(fileLinesContent, optionsName);
                // Delete optionsFields blocks
                fileLinesContent = DeleteOptionFieldBlocks(fileLinesContent, optionsFields);
            }
            else
            {
                // Manage Options
                fileLinesContent = UpdateOptions(fileLinesContent, optionsName, newOptionsName, CRUDDataUpdateType.Option);
                // Manage OptionsFields
                fileLinesContent = UpdateOptions(fileLinesContent, optionsFields, newOptionsName, CRUDDataUpdateType.OptionField, crudDtoProperties, propertyList);
            }

            return fileLinesContent;
        }


        private List<string> UpdateOptions(List<string> fileLinesContent, List<string> options, List<string> newOptionsName, CRUDDataUpdateType crudType,
            List<CrudProperty> crudDtoProperties = null, List<CRUDPropertyType> propertyList = null)
        {
            foreach (string optionName in options)
            {
                string markerBegin = $"{ZipParserService.MARKER_BEGIN} {crudType} {optionName}";
                string markerEnd = $"{ZipParserService.MARKER_END} {crudType} {optionName}";

                if (optionName == this.CrudNames.OldOptionNamePascalSingular)
                {
                    // replace options blocks
                    fileLinesContent = ReplaceOptions(fileLinesContent, optionName, newOptionsName, markerBegin, markerEnd, crudDtoProperties, propertyList);
                }
                else
                {
                    // delete options blocks
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

        private List<string> ReplaceOptions(List<string> fileLinesContent, string oldOptionName, List<string> newOptions, string markerBegin, string markerEnd,
            List<CrudProperty> crudDtoProperties, List<CRUDPropertyType> propertyList)
        {
            string line;
            bool endFound;
            List<string> newLines = new();
            string regex = $@"{markerBegin} ([a-zA-Z]+)";

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
                        // Manage OptionField
                        if (crudDtoProperties != null && propertyList != null)
                        {
                            string newProperty = null;
                            string fieldName = CommonTools.GetMatchRegexValue(regex, optionBlock[0]);
                            CRUDPropertyType propBase = propertyList.FirstOrDefault(p => p.Name == fieldName);
                            List<CrudProperty> crudDtoProp = crudDtoProperties.FindAll(c => string.Equals(c.AnnotationItemType, newOptionName, StringComparison.InvariantCultureIgnoreCase));

                            foreach (CrudProperty crudProp in crudDtoProp)
                            {
                                // Search corresponding property
                                CRUDPropertyType prop = ConvertDotNetToAngularType(crudProp);
                                if (prop?.SimplifiedType == propBase?.SimplifiedType)
                                {
                                    newProperty = crudProp.Name;

                                    // Update blocks lines
                                    foreach (string optionLine in optionBlock)
                                    {
                                        string newLine = optionLine;
                                        if (!string.IsNullOrWhiteSpace(newProperty))
                                            newLine = newLine.Replace(CommonTools.ConvertToCamelCase(fieldName), CommonTools.ConvertToCamelCase(newProperty));
                                        newLine = UpdateOptionFrontLine(newLine, oldOptionName, newOptionName);
                                        newLines.Add(newLine);
                                    }
                                }
                            }
                        }
                        else  // Manage Options
                        {
                            foreach (string optionLine in optionBlock)
                            {
                                string newLine = UpdateOptionFrontLine(optionLine, oldOptionName, newOptionName);
                                newLines.Add(newLine);
                            }
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
            newBlock = newBlock.Replace(this.CrudNames.OldCrudNameCamelPlural, this.CrudNames.NewCrudNameCamelPlural);
            newBlock = newBlock.Replace(this.CrudNames.OldCrudNameCamelSingular, this.CrudNames.NewCrudNameCamelSingular);

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

                newBlockLines.Add(ATTRIBUTE_TYPE_NOT_MANAGED.Replace(NEW_CRUD_NAME_SINGULAR_UPPER, CrudNames.NewCrudNamePascalSingular.ToUpper()).Replace(ATTRIBUTE_TYPE_NOT_MANAGED_FIELD, attributeName).Replace(ATTRIBUTE_TYPE_NOT_MANAGED_TYPE, attributeType));
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

                lines[i] = UpdateFrontLines(lines[i], type);
            }

            return lines;
        }

        private string UpdateFrontLines(string line, FeatureType type)
        {
            const string startImportRegex = @"^\s*[\/\*]*\s*import\s*[{(*]";
            const string regexComponent = @"^\s*[\/\*]*\s*import\s+{([\s\w,]*)}";
            const string regexComponentAs = @"^\s*[\/\*]*\s*import\s+\*\s+as\s+([\w,]*)\s+from";
            const string regexPath = @"\s*from\s*\'([\S]*)\';$";
            const string regexPathThen = @"^\s*[\/\*]*\s*import\s*\('([\w.\/]*)'\)";
            const string regexComponentPath = @"'([\.\/A-Za-z-]+)'[^\)]";
            const string regexComponentHtml = @"<\/?app-([a-z-]+)>?";

            string newLine = line;
            if (CommonTools.IsMatchRegexValue(startImportRegex, newLine))
            {
                // Update part between "import" and "from"
                string componentValue = CommonTools.GetMatchRegexValue(regexComponent, newLine, 1);
                if (string.IsNullOrEmpty(componentValue))
                {
                    componentValue = CommonTools.GetMatchRegexValue(regexComponentAs, newLine, 1);
                }
                if (!string.IsNullOrEmpty(componentValue))
                {
                    string newComponentValue = this.CrudNames.ConvertPascalOldToNewCrudName(componentValue, type);
                    newLine = newLine.Replace(componentValue, newComponentValue);
                }

                // Update part after "from"
                string pathValue = CommonTools.GetMatchRegexValue(regexPath, newLine, 1);
                if (string.IsNullOrEmpty(pathValue))
                {
                    pathValue = CommonTools.GetMatchRegexValue(regexPathThen, newLine, 1);
                }
                if (!string.IsNullOrEmpty(pathValue))
                {
                    string newPathValue = this.CrudNames.ConvertCamelToKebabCrudName(pathValue, type);
                    newLine = newLine.Replace(pathValue, newPathValue);
                }
                return newLine;
            }
            else if (CommonTools.IsMatchRegexValue(regexComponentPath, newLine))
            {
                string pathValue = CommonTools.GetMatchRegexValue(regexComponentPath, newLine, 1);
                if (!string.IsNullOrEmpty(pathValue))
                {
                    string newPathValue = this.CrudNames.ConvertCamelToKebabCrudName(pathValue, type);
                    return newLine.Replace(pathValue, newPathValue);
                }
            }
            else if (CommonTools.IsMatchRegexValue(regexComponentHtml, newLine))
            {
                string compValue = CommonTools.GetMatchRegexValue(regexComponentHtml, newLine, 1);
                if (!string.IsNullOrEmpty(compValue))
                {
                    string newPathValue = this.CrudNames.ConvertCamelToKebabCrudName(compValue, type);
                    return newLine.Replace(compValue, newPathValue);
                }
            }

            return this.CrudNames.ConvertPascalOldToNewCrudName(newLine, type, true);
        }

        private string UpdateOptionFrontLine(string line, string oldOptionName, string newOptionName)
        {
            const string startImportRegex = @"^\s*[\/\*]*\s*import\s*[{(*]";
            const string regexComponent = @"^\s*[\/\*]*\s*import\s+{([\s\w,]*)}";
            const string regexComponentAs = @"^\s*[\/\*]*\s*import\s+\*\s+as\s+([\w,]*)\s+from";
            const string regexPath = @"\s*from\s*\'([\S]*)\';$";
            const string regexPathThen = @"^\s*[\/\*]*\s*import\s*\('([\w.\/]*)'\)";

            string newLine = line;
            if (CommonTools.IsMatchRegexValue(startImportRegex, newLine))
            {
                // Update part between "import" and "from"
                string componentValue = CommonTools.GetMatchRegexValue(regexComponent, newLine, 1);
                if (string.IsNullOrEmpty(componentValue))
                {
                    componentValue = CommonTools.GetMatchRegexValue(regexComponentAs, newLine, 1);
                }
                if (!string.IsNullOrEmpty(componentValue))
                {
                    string newComponentValue = componentValue.Replace(oldOptionName, newOptionName);
                    newLine = newLine.Replace(componentValue, newComponentValue);
                }

                // Update part after "from"
                string pathValue = CommonTools.GetMatchRegexValue(regexPath, newLine, 1);
                if (string.IsNullOrEmpty(pathValue))
                {
                    pathValue = CommonTools.GetMatchRegexValue(regexPathThen, newLine, 1);
                }
                if (!string.IsNullOrEmpty(pathValue))
                {
                    string newPathValue = pathValue.Replace(CommonTools.ConvertPascalToKebabCase(oldOptionName), CommonTools.ConvertPascalToKebabCase(newOptionName));
                    newLine = newLine.Replace(pathValue, newPathValue);
                }
                return newLine;
            }

            return newLine.Replace(oldOptionName, newOptionName).Replace(CommonTools.ConvertToCamelCase(oldOptionName), CommonTools.ConvertToCamelCase(newOptionName));
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
        #endregion

        #region Delete Generation
        public void DeleteCrudOptionsGenerated(List<ZipFeatureType> zipFeatureTypeList, Project currentProject, CRUDGenerationHistory generationHistory)
        {
            CrudNames.InitRenameValues(generationHistory.EntityNameSingular, generationHistory.EntityNamePlural);
            foreach (Generation generation in generationHistory?.Generation.OrderBy(x => x.Feature))
            {
                GenerationType generationType = CommonTools.GetEnumValue<GenerationType>(generation.GenerationType);
                FeatureType featureType = CommonTools.GetEnumValue<FeatureType>(generation.Feature);

                ZipFeatureType zipFeature = zipFeatureTypeList.FirstOrDefault(f => f.GenerationType == generationType && f.FeatureType == featureType);
                if (zipFeature == null)
                {
                    // Error !
                    continue;
                }

                ClassDefinition classDefiniton = null;
                if (generationType == GenerationType.WebApi)
                    classDefiniton = ((WebApiFeatureData)zipFeature.FeatureDataList.FirstOrDefault(x => ((WebApiFeatureData)x).FileType == WebApiFileType.Controller))?.ClassFileDefinition;

                foreach (FeatureData featureData in zipFeature.FeatureDataList)
                {
                    // Get file path
                    string dest = null;
                    switch (generationType)
                    {
                        case GenerationType.WebApi:
                            WebApiFeatureData webApiFeatureData = (WebApiFeatureData)featureData;

                            if (webApiFeatureData.FileType == WebApiFileType.Entity ||
                                webApiFeatureData.FileType == WebApiFileType.Dto ||
                                webApiFeatureData.FileType == WebApiFileType.Mapper)
                                continue;

                            if (featureData.IsPartialFile)
                            {
                                dest = GetPartialFilePath(webApiFeatureData, classDefiniton, this.DotNetFolderGeneration);
                            }
                            else
                            {
                                (_, dest) = GetDotNetFilesPath(currentProject, webApiFeatureData, webApiFeatureData.ClassFileDefinition, featureType);
                            }
                            break;
                        case GenerationType.Front:
                            if (featureData.IsPartialFile)
                            {
                                dest = GetPartialFilePath(featureData, null, this.AngularFolderGeneration);
                            }
                            else
                            {
                                (_, dest) = GetAngularFilesPath(featureData, featureType);
                            }
                            break;
                        default:
                            break;
                    }

                    if (string.IsNullOrWhiteSpace(dest))
                    {
                        // Error !
                        continue;
                    }

                    if (featureData.IsPartialFile)
                    {
                        // Delete block part on file
                        DeletePartialBlock(dest, generationHistory.EntityNameSingular);
                    }
                    else
                    {
                        // Delete file
                        File.Delete(dest);
                    }
                }
            }
        }

        public void DeleteCrudOptionsGenerated(List<ZipFeatureType> zipFeatureTypeList, CRUDGenerationHistory generationHistory, List<CRUDGenerationHistory> optionsGenerationHistory)
        {
            foreach (CRUDGenerationHistory optionGenerationHistory in optionsGenerationHistory)
            {
                string dest = null;
                GenerationType generationType = GenerationType.Front;
                FeatureType featureType = FeatureType.CRUD;

                CrudNames.InitRenameValues(optionGenerationHistory.EntityNameSingular, optionGenerationHistory.EntityNamePlural);

                ZipFeatureType zipFeature = zipFeatureTypeList.FirstOrDefault(f => f.GenerationType == generationType && f.FeatureType == featureType);
                if (zipFeature == null)
                {
                    // Error !
                    continue;
                }

                foreach (FeatureData featureData in zipFeature.FeatureDataList)
                {
                    if (featureData.IsPartialFile)
                    {
                        continue;
                    }

                    (_, dest) = GetAngularFilesPath(featureData, featureType);
                    if (string.IsNullOrWhiteSpace(dest))
                    {
                        // Error !
                        continue;
                    }

                    List<string> newfileLinesContent = new();
                    List<string> fileLinesContent = File.ReadAllLines(dest).ToList();

                    // Delete Option XXX
                    string markerBegin = $"{ZipParserService.MARKER_BEGIN} {CRUDDataUpdateType.Option} {generationHistory.EntityNameSingular}";
                    string markerEnd = $"{ZipParserService.MARKER_END} {CRUDDataUpdateType.Option} {generationHistory.EntityNameSingular}";
                    newfileLinesContent = DeleteBlocks(fileLinesContent, markerBegin, markerEnd);
                    // Delete OptionField XXX
                    markerBegin = $"{ZipParserService.MARKER_BEGIN} {CRUDDataUpdateType.OptionField} {generationHistory.EntityNameSingular}";
                    markerEnd = $"{ZipParserService.MARKER_END} {CRUDDataUpdateType.OptionField} {generationHistory.EntityNameSingular}";
                    newfileLinesContent = DeleteBlocks(newfileLinesContent, markerBegin, markerEnd);

                    if (fileLinesContent.Count != newfileLinesContent.Count)
                        CommonTools.GenerateFile(dest, newfileLinesContent);
                }
            }
        }

        private void DeletePartialBlock(string file, string crudName)
        {
            bool partialBlockFound = false;
            List<string> newfileLinesContent = new();
            string markerBegin = @$"{ZipParserService.MARKER_BEGIN_PARTIAL} [a-zA-Z]+\s\d?\s?{crudName}";
            string markerEnd = @$"{ZipParserService.MARKER_END_PARTIAL} [a-zA-Z]+\s\d?\s?{crudName}";

            // Read partial file
            List<string> fileLinesContent = File.ReadAllLines(file).ToList();

            // Detele specifics partial blocks
            foreach (string line in fileLinesContent)
            {
                // Marker partial begin block found
                if (CommonTools.IsMatchRegexValue(markerBegin, line))
                {
                    partialBlockFound = true;
                    continue;
                }

                // Marker partial end block found
                if (CommonTools.IsMatchRegexValue(markerEnd, line))
                {
                    partialBlockFound = false;
                    continue;
                }

                // Add lines without partial block
                if (!partialBlockFound)
                {
                    newfileLinesContent.Add(line);
                }
            }

            // Generate new file
            CommonTools.GenerateFile(file, newfileLinesContent);
        }
        #endregion

        #region Tools
        private GenerationCrudData ExtractGenerationCrudData(FeatureData crudData, List<CrudProperty> crudDtoProperties, List<PropertyInfo> dtoRefProperties,
            string displayItem, FeatureType type, List<string> optionItem = null)
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
                            generationData.OptionsToAdd = optionItem;
                            break;
                        case CRUDDataUpdateType.OptionField:
                            if (!string.IsNullOrWhiteSpace(block.Name) && !generationData.OptionsFields.Contains(block.Name))
                                generationData.OptionsFields.Add(block.Name);
                            break;
                        case CRUDDataUpdateType.Display:
                            string extractItem = ((ExtractDisplayBlock)block).ExtractItem;
                            string extractLine = ((ExtractDisplayBlock)block).ExtractLine;
                            string newDisplayLine = this.CrudNames.ConvertPascalOldToNewCrudName(extractLine, type).Replace(extractItem, CommonTools.ConvertToCamelCase(displayItem));
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
            string generatedFolder = currentProject.Folder;

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
                if (match.ToLower() == "int" || match.ToLower() == "long")
                {
                    angularType = angularType.Replace(match, "number");
                }

                // Decimal
                if (match.ToLower() == "float" || match.ToLower() == "double")
                {
                    angularType = angularType.Replace(match, "decimal");
                }

                // Boolean
                else if (match.ToLower() == "bool")
                {
                    angularType = angularType.Replace(match, "boolean");
                }

                // Date
                else if (match == nameof(DateTime))
                {
                    angularType = angularType.Replace(match, "Date");
                }

                // Time
                else if (match == nameof(TimeSpan))
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

        private string GetNamespacePathBeforeOccurency(string line, FeatureType featureType)
        {
            var oldNamePascalSingular = featureType switch
            {
                FeatureType.CRUD => this.CrudNames.OldCrudNamePascalSingular,
                FeatureType.Team => this.CrudNames.OldTeamNamePascalSingular,
                _ => throw new NotImplementedException()
            };

            string partPath = string.Empty;
            foreach (string nmsp in line.Split('.'))
            {
                if (nmsp.Contains(oldNamePascalSingular, StringComparison.OrdinalIgnoreCase))
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
        public bool IsChildrenToDelete { get; set; } = false;
        public bool IsOptionFound { get; set; } = false;
        public bool IsParentToAdd { get; set; } = false;
        public bool IsAncestorFound { get; set; } = false;
        public List<string> BlocksToAdd { get; }
        public List<string> PropertiesToAdd { get; }
        public List<string> ChildrenName { get; }
        public List<string> OptionsName { get; }
        public List<string> OptionsToAdd { get; set; }
        public List<string> OptionsFields { get; }
        public List<string> AncestorName { get; }
        public List<KeyValuePair<string, string>> DisplayToUpdate { get; }
        public List<List<string>> ParentBlocks { get; }

        public GenerationCrudData()
        {
            this.BlocksToAdd = new();
            this.PropertiesToAdd = new();
            this.ChildrenName = new();
            this.OptionsName = new();
            this.OptionsFields = new();
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
        public bool IsParent { get; private set; } = false;
        public string AnnotationType { get; private set; }
        public string AnnotationItemType { get; private set; }

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
                else if (annotation.Key == GenerateCrudService.BIA_DTO_FIELD_ITEMTYPE)
                {
                    this.AnnotationItemType = annotation.Value;
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
