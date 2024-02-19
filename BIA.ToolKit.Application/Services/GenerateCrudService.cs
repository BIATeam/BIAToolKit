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
        private const string ATTRIBUTE_TYPE_NOT_MANAGED = "// CRUD GENERATOR FOR PLANE TO REVIEW : Field " + ATTRIBUTE_TYPE_NOT_MANAGED_FIELD + " or type " + ATTRIBUTE_TYPE_NOT_MANAGED_TYPE + " not managed.";
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

        public string GenerateCrudFiles(Project currentProject, EntityInfo dtoRefEntity, List<ZipFeatureType> zipFeatureTypeList, bool generateInProjectFolder)
        {
            string generationFolder = null;
            try
            {
                // Get generation folders
                generationFolder = GetGenerationFolder(currentProject, generateInProjectFolder);
                string dotnetDir = Path.Combine(generationFolder, Constants.FolderDotNet);
                string angularDir = Path.Combine(generationFolder, currentProject.BIAFronts);

                // Generate WebApi DotNet files
                ZipFeatureType backFeatureType = zipFeatureTypeList.Where(x => x.FeatureType == FeatureType.WebApi).FirstOrDefault();
                if (backFeatureType != null && backFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate DotNet files on '{dotnetDir}' ***", "Green");

                    GenerateWebApi(dotnetDir, backFeatureType.FeatureDataList, currentProject, dtoRefEntity);
                }

                // Generate CRUD angular files
                ZipFeatureType crudFeatureType = zipFeatureTypeList.Where(x => x.FeatureType == FeatureType.CRUD).FirstOrDefault();
                if (crudFeatureType != null && crudFeatureType.IsChecked)
                {
                    consoleWriter.AddMessageLine($"*** Generate Angular CRUD files on '{angularDir}' ***", "Green");

                    // Get CRUD dto properties
                    Dictionary<string, List<string>> crudDtoProperties = GetDtoProperties(dtoRefEntity);
                    if (crudDtoProperties == null)
                    {
                        consoleWriter.AddMessageLine($"Can't generate Angular CRUD files: Dto properties are empty.", "Red");
                    }
                    else
                    {
                        GenerateCRUD(angularDir, crudFeatureType.FeatureDataList, currentProject, crudDtoProperties);
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
        private void GenerateWebApi(string destDir, List<FeatureData> featureDataList, Project currentProject, EntityInfo dtoRefEntity)
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
                    UpdateWebApiFile(destDir, currentProject, crudData, dtoClassDefiniton, crudNamespaceList);
                }
                foreach (WebApiFeatureData crudData in featureDataList.Where(ft => ft.IsPartialFile))
                {
                    // Update with partial file
                    UpdatePartialFile(srcDir, destDir, currentProject, crudData, FeatureType.WebApi, dtoClassDefiniton);
                }

            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in DotNet CRUD generation process: {ex.Message}", "Red");
            }
        }

        private void GenerateCRUD(string angularDir, List<FeatureData> featureDataList, Project currentProject, Dictionary<string, List<string>> crudDtoProperties)
        {
            try
            {
                List<string> blocksToAdd;
                List<string> propertiesToAdd;
                bool isChildrenToDelete;
                bool isOptionToDelete;
                List<string> childrenName;
                List<string> optionsName;

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
                        blocksToAdd = new();
                        propertiesToAdd = new();
                        isChildrenToDelete = false;
                        isOptionToDelete = false;
                        childrenName = new();
                        optionsName = new();

                        if (crudData.ExtractBlocks?.Count > 0)
                        {
                            foreach (ExtractBlock block in crudData.ExtractBlocks)
                            {
                                if (block.DataUpdateType == CRUDDataUpdateType.Properties)
                                {
                                    // Generate new properties to add
                                    foreach (KeyValuePair<string, List<string>> crudDtoProperty in crudDtoProperties)
                                    {
                                        propertiesToAdd.AddRange(GeneratePropertiesToAdd((ExtractPropertiesBlock)block, crudDtoProperty));
                                    }
                                }
                                else if (block.DataUpdateType == CRUDDataUpdateType.Child)
                                {
                                    isChildrenToDelete |= block.BlockLines.Count > 0;
                                    childrenName.Add(block.Name);
                                }
                                else if (block.DataUpdateType == CRUDDataUpdateType.Option)
                                {
                                    isOptionToDelete |= block.BlockLines.Count > 0;
                                    optionsName.Add(block.Name);
                                }
                                // Blocks : do nothing (traitment done after)
                            }

                            // Generate new blocks to add
                            if (crudData.ExtractBlocks.Any(b => b.DataUpdateType == CRUDDataUpdateType.Block))
                            {
                                // Get block list and properties associated to blocks
                                ExtractBlock propertyBlock = crudData.ExtractBlocks.FirstOrDefault(p => p.DataUpdateType == CRUDDataUpdateType.Properties);
                                List<ExtractBlock> blocksList = crudData.ExtractBlocks.FindAll(b => b.DataUpdateType == CRUDDataUpdateType.Block);

                                foreach (KeyValuePair<string, List<string>> crudDtoProperty in crudDtoProperties)
                                {
                                    CRUDPropertyType dtoPropertyType = ConvertDotNetToAngularType(crudDtoProperty.Key);
                                    ExtractBlocksBlock blockReferenceFound = (ExtractBlocksBlock)blocksList.FirstOrDefault(b => ((ExtractBlocksBlock)b).PropertyType.SimplifiedType == dtoPropertyType.SimplifiedType);
                                    if (blockReferenceFound != null)
                                    {
                                        blocksToAdd.AddRange(GenerateBlockToAdd(blockReferenceFound, crudDtoProperty, dtoPropertyType.IsRequired));
                                    }
                                    else
                                    {
                                        blocksToAdd.AddRange(GenerateEmptyBlockToAdd((ExtractBlocksBlock)blocksList.First(), crudDtoProperty, dtoPropertyType.IsRequired));
                                    }
                                }
                            }
                        }

                        // Create file
                        string src = Path.Combine(crudData.ExtractDirPath, crudData.FilePath);
                        string dest = ConvertCamelToKebabCrudName(Path.Combine(angularDir, crudData.FilePath), FeatureType.CRUD);

                        // replace blocks !
                        GenerateAngularFile(FeatureType.CRUD, src, dest, propertiesToAdd, blocksToAdd, isChildrenToDelete, childrenName, isOptionToDelete, optionsName);
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

                    // replace blocks !
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

        #region DotNet Files
        private void UpdateWebApiFile(string destDir, Project currentProject, WebApiFeatureData crudData, ClassDefinition dtoClassDefiniton, List<WebApiNamespace> crudNamespaceList)
        {
            string src = Path.Combine(crudData.ExtractDirPath, crudData.FilePath);
            string dest = ConvertPascalOldToNewCrudName(Path.Combine(destDir, crudData.FilePath), FeatureType.WebApi);
            dest = ReplaceCompagnyNameProjetName(dest, currentProject, dtoClassDefiniton);

            // Prepare destination folder
            CommonTools.CheckFolder(new FileInfo(dest).DirectoryName);

            // Read file
            List<string> fileLinesContent = File.ReadAllLines(src).ToList();

            for (int i = 0; i < fileLinesContent.Count; i++)
            {
                if (IsNamespaceOrUsingLine(fileLinesContent[i]))
                {
                    fileLinesContent[i] = UpdateNamespaceUsing(fileLinesContent[i], currentProject, dtoClassDefiniton, crudNamespaceList);
                }
                // Convert Crud Name (Plane to XXX)
                fileLinesContent[i] = ConvertPascalOldToNewCrudName(fileLinesContent[i], FeatureType.WebApi);
            }

            // TODO NMA: manage option or children ???

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
                if (crudData.FileType == WebApiFileType.Dto || crudData.FileType == WebApiFileType.Entity || crudData.FileType == WebApiFileType.Mapper)
                {
                    // Get part of namespace before "plane" occurency
                    string partPath = string.Empty;
                    foreach (string nmsp in crudData.Namespace.Split('.'))
                    {
                        if (nmsp.Contains(OldCrudNamePascalSingular, StringComparison.OrdinalIgnoreCase))
                        {
                            partPath = partPath.Remove(partPath.Length - 1);
                            break;
                        }
                        partPath += $"{nmsp}.";
                    }

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
                                webApiNamespace.CrudNamespaceGenerated = GetNamespaceOrUsingValue(line);
                                break;
                            }
                        }
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
            string nmsp = GetNamespaceOrUsingValue(line);
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
        private void GenerateAngularFile(FeatureType type, string fileName, string newFileName,
            List<string> propertiesToAdd = null, List<string> blocksToAdd = null,
            bool isChildrenToDelete = false, List<string> childrenName = null,
            bool isOptionToDelete = false, List<string> optionsName = null)
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

            // Replace properties and blocks
            if (propertiesToAdd?.Count > 0 || blocksToAdd?.Count > 0)
            {
                fileLinesContent = ReplacePropertiesAndBlocks(fileName, fileLinesContent, propertiesToAdd, blocksToAdd);
            }

            // Remove children
            if (isChildrenToDelete)
            {
                fileLinesContent = DeleteChildrenBlocks(fileLinesContent, childrenName);
            }

            // Remove all options
            if (isOptionToDelete)
            {
                fileLinesContent = DeleteOptionsBlocks(fileLinesContent, optionsName);
            }

            // Update file content
            UpdateFileLinesContent(fileLinesContent, type);

            // Generate new file
            CommonTools.GenerateFile(newFileName, fileLinesContent);
        }

        private List<string> ReplacePropertiesAndBlocks(string fileName, List<string> fileLinesContent, List<string> propertiesToAdd, List<string> blocksToAdd)
        {
            if ((propertiesToAdd == null || propertiesToAdd.Count <= 0) && (blocksToAdd == null || blocksToAdd.Count <= 0))
                return fileLinesContent;

            int indexBeginProperty = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains(ZipParserService.MARKER_BEGIN_PROPERTIES)).FirstOrDefault());
            int indexEndProperty = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains(ZipParserService.MARKER_END_PROPERTIES)).FirstOrDefault());

            int indexBeginFirstBlock = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains(ZipParserService.MARKER_BEGIN_BLOCK)).FirstOrDefault());
            int indexEndLastBlock = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains(ZipParserService.MARKER_END_BLOCK)).LastOrDefault());

            List<string> newFileLinesContent = new();
            // Writes properties ?
            if (indexBeginProperty != -1 && indexEndProperty != -1)
            {
                // Write lines before properties
                for (int i = 0; i < indexBeginProperty; i++)
                {
                    newFileLinesContent.Add(fileLinesContent[i]);
                }

                // Write new properties to add
                newFileLinesContent.Add(fileLinesContent[indexBeginProperty]);
                for (int i = 0; i < propertiesToAdd.Count; i++)
                {
                    newFileLinesContent.Add(propertiesToAdd[i]);
                }
                newFileLinesContent.Add(fileLinesContent[indexEndProperty]);

                // Write blocks ?
                if (indexBeginFirstBlock != -1 && indexEndLastBlock != -1)
                {
                    string spaces = GetSpacesBeginningLine(newFileLinesContent.Last());

                    // Write lines between properties and first block
                    for (int i = indexEndProperty + 1; i < indexBeginFirstBlock; i++)
                    {
                        newFileLinesContent.Add(fileLinesContent[i]);
                    }

                    // Write blocks to add
                    for (int i = 0; i < blocksToAdd.Count; i++)
                    {
                        newFileLinesContent.Add(blocksToAdd[i]);
                        if (i != blocksToAdd.Count - 1)
                        {
                            newFileLinesContent.Add($"{spaces}  ,");
                        }
                    }

                    // Write lines after last block
                    for (int i = indexEndLastBlock + 1; i < fileLinesContent.Count; i++)
                    {
                        newFileLinesContent.Add(fileLinesContent[i]);
                    }
                }
                else
                {
                    // Writes lines until the end
                    for (int i = indexEndProperty + 1; i < fileLinesContent.Count; i++)
                    {
                        newFileLinesContent.Add(fileLinesContent[i]);
                    }
                }
            }
            else
            {
                // Write blocks ?
                if (indexBeginFirstBlock != -1 && indexEndLastBlock != -1)
                {
                    string spaces = GetSpacesBeginningLine(newFileLinesContent.Last());

                    // Write lines 
                    for (int i = 0; i < indexBeginFirstBlock; i++)
                    {
                        newFileLinesContent.Add(fileLinesContent[i]);
                    }

                    // Write blocks to add
                    for (int i = 0; i < blocksToAdd.Count; i++)
                    {
                        newFileLinesContent.Add(blocksToAdd[i]);
                        if (i != blocksToAdd.Count - 1)
                            newFileLinesContent.Add($"{spaces}  ,");
                    }

                    // Write lines after last block
                    for (int i = indexEndLastBlock + 1; i < fileLinesContent.Count; i++)
                    {
                        newFileLinesContent.Add(fileLinesContent[i]);
                    }
                }
                else
                {
                    // Error
                    consoleWriter.AddMessageLine($"Update File '{fileName}', index not found (Property: begin={indexBeginProperty}, end={indexEndProperty}; Block: begin={indexBeginFirstBlock}, end={indexEndLastBlock})", "Orange");

                    // Write lines 
                    for (int i = 0; i < fileLinesContent.Count; i++)
                    {
                        newFileLinesContent.Add(fileLinesContent[i]);
                    }
                }
            }

            return newFileLinesContent;
        }

        private List<string> DeleteChildrenBlocks(List<string> fileLinesContent, List<string> childrenName)
        {
            childrenName?.ForEach(c =>
            {
                string childName = c;
                string markerBegin, markerEnd;
                if (string.IsNullOrWhiteSpace(c))
                {
                    markerBegin = $"<!-- {ZipParserService.MARKER_BEGIN_CHILD} ";
                    markerEnd = $"{ZipParserService.MARKER_END_CHILD} -->";
                }
                else
                {
                    markerBegin = $"<!-- {ZipParserService.MARKER_BEGIN_CHILD} {childName}";
                    markerEnd = $"{ZipParserService.MARKER_END_CHILD} {childName} -->";
                }
                fileLinesContent = DeleteBlocks(fileLinesContent, markerBegin, markerEnd);
            });

            return DeleteBlocks(fileLinesContent, ZipParserService.MARKER_BEGIN_CHILD, ZipParserService.MARKER_END_CHILD);
        }

        private List<string> DeleteOptionsBlocks(List<string> fileLinesContent, List<string> optionsName)
        {
            optionsName?.ForEach(optionName =>
            {
                string markerBegin, markerEnd;
                if (string.IsNullOrWhiteSpace(optionName))
                {
                    markerBegin = $"/* {ZipParserService.MARKER_BEGIN_OPTION} ";
                    markerEnd = $"{ZipParserService.MARKER_END_OPTION} */";
                }
                else
                {
                    markerBegin = $"/* {ZipParserService.MARKER_BEGIN_OPTION} {optionName}";
                    markerEnd = $"{ZipParserService.MARKER_END_OPTION} {optionName} */";
                }

                fileLinesContent = DeleteBlocks(fileLinesContent, markerBegin, markerEnd);
            });

            return DeleteBlocks(fileLinesContent, ZipParserService.MARKER_BEGIN_OPTION, ZipParserService.MARKER_END_OPTION);
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

        private List<string> GeneratePropertiesToAdd(ExtractPropertiesBlock propertyBlock, KeyValuePair<string, List<string>> crudProperties)
        {
            List<string> propertiesToAdd = new();
            KeyValuePair<string, List<string>> propertyRef;

            CRUDPropertyType crudPropertyType = ConvertDotNetToAngularType(crudProperties.Key);
            if (propertyBlock.PropertiesList.ContainsKey(crudPropertyType.Type))
            {
                // if type found
                propertyRef = propertyBlock.PropertiesList.FirstOrDefault(p => p.Key == crudPropertyType.Type);
            }
            else
            {
                if (propertyBlock.PropertiesList.ContainsKey(crudPropertyType.SimplifiedType))
                {
                    // if Simplified type found
                    propertyRef = propertyBlock.PropertiesList.FirstOrDefault(p => p.Key == crudPropertyType.SimplifiedType);
                }
                else
                {
                    // in other cases (type and simplified not found), get first by default (string)
                    propertyRef = propertyBlock.PropertiesList.FirstOrDefault(p => p.Key == propertyBlock.PropertiesList.Keys.FirstOrDefault());
                }
            }

            string lineFound = propertyBlock.BlockLines.FirstOrDefault(x => x.TrimStart().StartsWith(propertyRef.Value[0]));

            // Generate new properties
            foreach (string attrName in crudProperties.Value)
            {
                string regex = $@"^(\s*)(\w+)(\s*{Constants.PropertySeparator}\s*)(\w+\W*\w*)(;\s*)$";
                string newline = Regex.Replace(lineFound, regex, $"$1{CommonTools.ConvertToCamelCase(attrName)}$3{crudPropertyType.Type}$5");

                propertiesToAdd.Add(newline);
            }

            return propertiesToAdd;
        }

        private List<string> GenerateBlockToAdd(ExtractBlocksBlock block, KeyValuePair<string, List<string>> crudDtoProperty, bool isRequired)
        {
            List<string> blocksToAdd = new();

            if (block.BlockLines == null || block.BlockLines.Count <= 0)
            {
                consoleWriter.AddMessageLine("Error 'extractBlock' (block) is empty.", "Red");
                return null;
            }

            CheckRequiredLine(block.BlockLines, isRequired);

            // Generate block based on dto model
            foreach (string attrName in crudDtoProperty.Value)
            {
                blocksToAdd.Add(ReplaceBlock(block, attrName));
            }

            return blocksToAdd;
        }

        private List<string> GenerateEmptyBlockToAdd(ExtractBlocksBlock defaultBlock, KeyValuePair<string, List<string>> crudDtoProperty, bool isRequired)
        {
            List<string> blocksToAdd = new();

            // Generate "empty" block
            foreach (string attrName in crudDtoProperty.Value)
            {
                if (defaultBlock != null)
                {
                    blocksToAdd.Add(CreateEmptyBlock(defaultBlock, crudDtoProperty.Key, attrName, isRequired));
                }
            }

            return blocksToAdd;
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
                string spaces = GetSpacesBeginningLine(lines[0]);
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
                    string spaces = GetSpacesBeginningLine(extractBlock.BlockLines[0]);
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

        private Dictionary<string, List<string>> GetDtoProperties(EntityInfo dtoEntity)
        {
            if (dtoEntity == null) { return null; }

            Dictionary<string, List<string>> dico = new();
            dtoEntity.Properties.ForEach(p =>
            {
                CommonTools.AddToDictionnary(dico, p.Type.ToString(), p.Name);
            });
            return dico;
        }
        #endregion

        #region Tools
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
                int indexStart = contentToReplace.IndexOf(fileContent.FirstOrDefault(line => line.Contains(contentToAdd[0])));
                if (indexStart >= 0)
                {
                    int indexStop = contentToReplace.IndexOf(fileContent.FirstOrDefault(line => line.Contains(contentToAdd[contentToAdd.Count - 1])));

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

        private CRUDPropertyType ConvertDotNetToAngularType(string dotnetType)
        {
            if (dotnetType == null) { return null; }

            bool isRequired = false;
            string angularType = dotnetType;

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
                    isRequired = true;
                }

                // Boolean
                else if (match.ToLower() == "bool")
                {
                    angularType = angularType.Replace(match, "boolean");
                    isRequired = false;
                }

                // Date
                else if (match == "DateTime")
                {
                    angularType = angularType.Replace(match, "Date");
                    isRequired = true;
                }

                // Time
                else if (match == "TimeSpan")
                {
                    angularType = angularType.Replace(match, "string");
                    isRequired = true;
                }

                // XXXDto
                else if (match.EndsWith("Dto"))
                {
                    angularType = angularType.Replace(match, "OptionDto");
                    isRequired = false;
                }

                // String
                else if (match.ToLower() == "string")
                {
                    isRequired = true;
                }
            }

            if (angularType.EndsWith('?'))
            {
                angularType = angularType.Replace("?", " | null");
                isRequired = false;
            }

            return new CRUDPropertyType(angularType, isRequired);
        }

        private string GetSpacesBeginningLine(string line)
        {
            const string regex = @"^(\s*).+$";
            return CommonTools.GetMatchRegexValue(regex, line, 1);
        }

        private bool IsNamespaceOrUsingLine(string line)
        {
            string regex = @"^\s*(?:namespace|using)\s(\w+)\.(\w+)\.";
            return CommonTools.IsMatchRegexValue(regex, line);
        }

        private string GetNamespaceOrUsingValue(string line)
        {
            string regex = @"^\s*(?:namespace|using)\s([\w.]*);*\s*$";
            return CommonTools.GetMatchRegexValue(regex, line);
        }
        #endregion
    }
}
