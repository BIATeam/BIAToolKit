namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Settings;
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

    public class GenerateCrudService
    {
        private readonly IConsoleWriter consoleWriter;
        private CRUDSettings crudSettings;

        private const string ATTRIBUTE_TYPE_NOT_MANAGED = "// CRUD GENERATOR FOR PLANE TO REVIEW : Field " + ATTRIBUTE_TYPE_NOT_MANAGED_FIELD + " or type " + ATTRIBUTE_TYPE_NOT_MANAGED_TYPE + " not managed.";
        private const string ATTRIBUTE_TYPE_NOT_MANAGED_FIELD = "XXXFieldXXX";
        private const string ATTRIBUTE_TYPE_NOT_MANAGED_TYPE = "YYYTypeYYY";
        private const string COMPAGNY_TAG = "XXXCompagnyXXX";
        private const string PROJECT_NAME_TAG = "YYYProjectNameYYY";

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

        public GenerateCrudService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void SetSettings(CRUDSettings settings)
        {
            this.crudSettings = settings;
            InitRenameValues();
        }

        private void InitRenameValues()
        {
            // Get Pascal case value
            this.OldCrudNamePascalSingular = string.IsNullOrWhiteSpace(this.crudSettings.CRUDReferenceSingular) ? this.OldCrudNamePascalSingular : this.crudSettings.CRUDReferenceSingular;
            this.OldCrudNamePascalPlural = string.IsNullOrWhiteSpace(this.crudSettings.CRUDReferencePlurial) ? this.OldCrudNamePascalPlural : this.crudSettings.CRUDReferencePlurial;
            this.OldOptionNamePascalSingular = string.IsNullOrWhiteSpace(this.crudSettings.OptionReferenceSingular) ? this.OldOptionNamePascalSingular : this.crudSettings.OptionReferenceSingular;
            this.OldOptionNamePascalPlural = string.IsNullOrWhiteSpace(this.crudSettings.OptionReferencePlurial) ? this.OldOptionNamePascalPlural : this.crudSettings.OptionReferencePlurial;
            this.OldTeamNamePascalSingular = string.IsNullOrWhiteSpace(this.crudSettings.TeamReferenceSingular) ? this.OldTeamNamePascalSingular : this.crudSettings.TeamReferenceSingular;
            this.OldTeamNamePascalPlural = string.IsNullOrWhiteSpace(this.crudSettings.TeamReferencePlurial) ? this.OldTeamNamePascalPlural : this.crudSettings.TeamReferencePlurial;

            // Convert value to Camel case
            this.OldCrudNameCamelSingular = CommonTools.ConvertToCamelCase(OldCrudNamePascalSingular);
            this.OldCrudNameCamelPlural = CommonTools.ConvertToCamelCase(OldCrudNamePascalPlural);
            this.OldOptionNameCamelSingular = CommonTools.ConvertToCamelCase(OldOptionNamePascalSingular);
            this.OldOptionNameCamelPlural = CommonTools.ConvertToCamelCase(OldOptionNamePascalPlural);
            this.OldTeamNameCamelSingular = CommonTools.ConvertToCamelCase(OldTeamNamePascalSingular);
            this.OldTeamNameCamelPlural = CommonTools.ConvertToCamelCase(OldTeamNamePascalPlural);
        }

        public void InitRenameValues(string newValueSingular, string newValuePlurial)
        {
            this.NewCrudNamePascalSingular = newValueSingular;
            this.NewCrudNamePascalPlural = newValuePlurial;
            this.NewCrudNameCamelSingular = CommonTools.ConvertToCamelCase(NewCrudNamePascalSingular);
            this.NewCrudNameCamelPlural = CommonTools.ConvertToCamelCase(NewCrudNamePascalPlural);
            this.NewCrudNameKebabSingular = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalSingular);
            this.NewCrudNameKebabPlural = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalPlural);
        }

        public string GenerateCrudFiles(Project currentProject, EntityInfo dtoEntity, List<ZipFilesContent> fileListFromZip)
        {
            string generationFolder = null;
            try
            {
                // Get generation folders
                generationFolder = GetGenerationFolder(currentProject, this.crudSettings.GenerateInProjectFolder);
                string dotnetDir = Path.Combine(generationFolder, Constants.FolderDotNet);
                string angularDir = Path.Combine(generationFolder, Constants.FolderAngular);

                // Generate CRUD DotNet files
                ZipFilesContent backFilesContent = fileListFromZip.Where(x => x.Type == FeatureType.Back).FirstOrDefault();
                if (backFilesContent != null)
                {
                    consoleWriter.AddMessageLine($"*** Generate DotNet files on '{dotnetDir}' ***", "Green");

                    GenerateBack(dotnetDir, backFilesContent, currentProject);
                }

                // Generate CRUD angular files
                ZipFilesContent crudFilesContent = fileListFromZip.Where(x => x.Type == FeatureType.CRUD).FirstOrDefault();
                if (crudFilesContent != null)
                {
                    consoleWriter.AddMessageLine($"*** Generate Angular CRUD files on '{angularDir}' ***", "Green");

                    // Get CRUD dto properties
                    Dictionary<string, List<string>> crudDtoProperties = GetDtoProperties(dtoEntity);
                    if (crudDtoProperties == null)
                    {
                        consoleWriter.AddMessageLine($"Can't generate Angular CRUD files: Dto properties are empty.", "Red");
                    }
                    else
                    {
                        GenerateCRUD(angularDir, crudDtoProperties, crudFilesContent, currentProject);
                    }
                }

                // Generate Option angular files
                ZipFilesContent optionFilesContent = fileListFromZip.Where(x => x.Type == FeatureType.Option).FirstOrDefault();
                if (optionFilesContent != null)
                {
                    consoleWriter.AddMessageLine($"*** Generate Angular Option files on '{angularDir}' ***", "Green");

                    GenerateOption(angularDir, optionFilesContent);
                }

                // Generate Team angular files
                ZipFilesContent teamFilesContent = fileListFromZip.Where(x => x.Type == FeatureType.Team).FirstOrDefault();
                if (teamFilesContent != null)
                {
                    consoleWriter.AddMessageLine($"Team generation not yet implemented!", "Orange");

                    GenerateTeam(angularDir, teamFilesContent);
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in CRUD generation process: {ex.Message}", "Red");
            }

            return generationFolder;
        }

        #region Back
        private void GenerateBack(string destDir, ZipFilesContent zipFilesContent, Project currentProject)
        {
            try
            {
                string srcDir = Path.Combine(GetGenerationFolder(currentProject), Constants.FolderDotNet);

                DotNetCRUDData featureData = (DotNetCRUDData)zipFilesContent.FeatureDataList.FirstOrDefault(x => ((DotNetCRUDData)x).FileType == BackFileType.Dto);
                ClassDefinition dtoClassDefiniton = featureData?.ClassFileDefinition;

                // Generate Crud files
                foreach (DotNetCRUDData crudData in zipFilesContent.FeatureDataList)
                {
                    if (crudData.FileType == BackFileType.Dto ||
                        crudData.FileType == BackFileType.Entity ||
                        crudData.FileType == BackFileType.Mapper)
                    {
                        // Ignore file : not necessary to regenerate it
                        continue;
                    }
                    else if (crudData.IsPartialFile)
                    {
                        // Update with partial file
                        UpdatePartialFile(srcDir, destDir, currentProject, crudData, dtoClassDefiniton);
                    }
                    else
                    {
                        GenerateDotNetCrudFile(destDir, currentProject, crudData, dtoClassDefiniton);
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in DotNet CRUD generation process: {ex.Message}", "Red");
            }
        }
        #endregion

        #region Front
        private void GenerateCRUD(string angularDir, Dictionary<string, List<string>> crudDtoProperties, ZipFilesContent zipFilesContent, Project currentProject)
        {
            try
            {
                List<string> blocksToAdd;
                List<string> propertiesToAdd;
                List<string> childrenToDelete;
                List<string> optionToDelete;

                foreach (AngularCRUDData crudData in zipFilesContent.FeatureDataList)
                {
                    if (crudData.IsPartialFile)
                    {
                        // Update with partial file
                        string srcDir = Path.Combine(GetGenerationFolder(currentProject), Constants.FolderAngular);
                        UpdatePartialFile(srcDir, angularDir, currentProject, crudData);
                    }
                    else
                    {
                        blocksToAdd = new();
                        propertiesToAdd = new();
                        childrenToDelete = new();
                        optionToDelete = new();
                        if (crudData.ExtractBlocks != null && crudData.ExtractBlocks.Count > 0)
                        {
                            foreach (KeyValuePair<string, List<string>> crudDtoProperty in crudDtoProperties)
                            {
                                // Generate new properties to add
                                propertiesToAdd.AddRange(GeneratePropertiesToAdd(crudData, crudDtoProperty));
                                // Generate new blocks to add
                                blocksToAdd.AddRange(GenerateBlocksToAdd(crudData, crudDtoProperty));
                            }

                            // Get Children line to delete
                            var childrenBlocs = crudData.ExtractBlocks.Where(l => l.DataUpdateType == CRUDDataUpdateType.Children);
                            if (childrenBlocs != null)
                            {
                                childrenBlocs.ToList().ForEach(b => childrenToDelete.AddRange(b.BlockLines));
                            }
                        }

                        if (crudData.OptionToDelete != null && crudData.OptionToDelete.Count > 0)
                        {
                            // Get lines to delete
                            optionToDelete = crudData.OptionToDelete;
                        }

                        // Create file
                        string src = Path.Combine(crudData.ExtractDirPath, crudData.FilePath);
                        string dest = ConvertCamelToKebabCrudName(Path.Combine(angularDir, crudData.FilePath), FeatureType.CRUD);

                        // replace blocks !
                        GenerateAngularFile(FeatureType.CRUD, src, dest, propertiesToAdd, blocksToAdd, childrenToDelete, optionToDelete);
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in Angular CRUD generation process: {ex.Message}", "Red");
            }
        }

        private void GenerateOption(string angularDir, ZipFilesContent zipFilesContent)
        {
            try
            {
                foreach (FeatureData angularFile in zipFilesContent.FeatureDataList)
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

        private void GenerateTeam(string angularDir, ZipFilesContent zipFilesContent)
        {
            // TODO
            consoleWriter.AddMessageLine("Generate Team not implemented!", "Orange");
        }
        #endregion

        #region DotNet Files
        private void GenerateDotNetCrudFile(string destDir, Project currentProject, DotNetCRUDData crudData, ClassDefinition dtoClassDefiniton)
        {
            string src = Path.Combine(crudData.ExtractDirPath, crudData.FilePath);
            string dest = ConvertPascalOldToNewCrudName(Path.Combine(destDir, crudData.FilePath), FeatureType.Back);
            dest = ReplaceCompagnyNameProjetName(dest, currentProject, dtoClassDefiniton);

            // Prepare destination folder
            CommonTools.CheckFolder(new FileInfo(dest).DirectoryName);

            // Read file
            List<string> fileLinesContent = File.ReadAllLines(src).ToList();

            // Replace Compagny name and Project name
            for (int i = 0; i < fileLinesContent.Count; i++)
            {
                fileLinesContent[i] = ReplaceCompagnyNameProjetName(fileLinesContent[i], currentProject, dtoClassDefiniton);
                fileLinesContent[i] = ConvertPascalOldToNewCrudName(fileLinesContent[i], FeatureType.Back);
            }

            // Generate new file
            CommonTools.GenerateFile(dest, fileLinesContent);
        }

        private void UpdatePartialFile(string srcDir, string destDir, Project currentProject, FeatureData crudData, ClassDefinition dtoClassDefiniton = null)
        {
            FeatureType type;
            string fileName, srcFile, destFile, markerBegin, markerEnd;
            List<string> contentToAdd = new();

            if (dtoClassDefiniton != null)
            {
                fileName = ReplaceCompagnyNameProjetName(crudData.FilePath, currentProject, dtoClassDefiniton).Replace(Constants.PartialFileSuffix, "");
            }
            else
            {
                fileName = crudData.FilePath.Replace(Constants.PartialFileSuffix, "");
            }

            srcFile = Path.Combine(srcDir, fileName);
            destFile = Path.Combine(destDir, fileName);

            if (crudData.ExtractBlocks != null &&
                crudData.ExtractBlocks.Count > 0 &&
                crudData.ExtractBlocks[0].BlockLines != null)
            {
                ExtractBlocks block = crudData.ExtractBlocks[0];

                switch (block.DataUpdateType)
                {
                    case CRUDDataUpdateType.Config:
                        markerBegin = ZipParserService.MARKER_BEGIN_CONFIG;
                        markerEnd = ZipParserService.MARKER_END_CONFIG;
                        type = FeatureType.Back;
                        break;
                    case CRUDDataUpdateType.Dependency:
                        markerBegin = ZipParserService.MARKER_BEGIN_DEPENDENCY;
                        markerEnd = ZipParserService.MARKER_END_DEPENDENCY;
                        type = FeatureType.Back;
                        break;
                    case CRUDDataUpdateType.Navigation:
                        markerBegin = ZipParserService.MARKER_BEGIN_NAVIGATION;
                        markerEnd = ZipParserService.MARKER_END_NAVIGATION;
                        type = FeatureType.CRUD;
                        break;
                    case CRUDDataUpdateType.Permission:
                        markerBegin = ZipParserService.MARKER_BEGIN_PERMISSION;
                        markerEnd = ZipParserService.MARKER_END_PERMISSION;
                        type = FeatureType.CRUD;
                        break;
                    case CRUDDataUpdateType.Rights:
                        markerBegin = ZipParserService.MARKER_BEGIN_RIGHTS;
                        markerEnd = ZipParserService.MARKER_END_RIGHTS;
                        type = FeatureType.Back;
                        break;
                    case CRUDDataUpdateType.Routing:
                        markerBegin = ZipParserService.MARKER_BEGIN_ROUTING;
                        markerEnd = ZipParserService.MARKER_END_ROUTING;
                        type = FeatureType.CRUD;
                        break;
                    default: return;
                }

                // Generate content to add
                block.BlockLines.ForEach(line => contentToAdd.Add(ConvertPascalOldToNewCrudName(line, type)));

                // Update file
                UpdateDotDetCrudFile(srcFile, destFile, contentToAdd, markerBegin, markerEnd);
            }
        }

        private void UpdateDotDetCrudFile(string srcFile, string destFile, List<string> contentToAdd, string markerBegin, string markerEnd)
        {
            // Read file
            List<string> fileContent = File.ReadAllLines(srcFile).ToList();

            // Insert data on file content
            List<string> newContent = InsertContentBetweenMarkers(fileContent, contentToAdd, markerBegin, markerEnd);

            // Generate file with new content
            CommonTools.GenerateFile(destFile, newContent);
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
        #endregion

        #region Angular Files
        private void GenerateAngularFile(FeatureType type, string fileName, string newFileName, List<string> propertiesToAdd = null,
            List<string> blocksToAdd = null, List<string> childrenToDelete = null, List<string> optionToDelete = null)
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
            fileLinesContent = ReplacePropertiesAndBlocks(fileName, fileLinesContent, propertiesToAdd, blocksToAdd);

            // Remove children
            fileLinesContent = DeleteChildrenBlocks(fileLinesContent);

            // Rmove all options
            if (optionToDelete != null)
            {
                optionToDelete.ForEach(line => fileLinesContent.Remove(line));
            }

            // Update file content
            UpdateFileLinesContent(fileLinesContent, type);

            // Generate new file
            CommonTools.GenerateFile(newFileName, fileLinesContent);
        }

        private List<string> ReplacePropertiesAndBlocks(string fileName, List<string> fileLinesContent, List<string> propertiesToAdd, List<string> blocksToAdd)
        {
            if ((propertiesToAdd == null || propertiesToAdd.Count <= 0) && (blocksToAdd == null || blocksToAdd.Count <= 0)) return fileLinesContent;


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
                newFileLinesContent.Add($"  /// {ZipParserService.MARKER_BEGIN_PROPERTIES}");
                for (int i = 0; i < propertiesToAdd.Count; i++)
                {
                    newFileLinesContent.Add(propertiesToAdd[i]);
                }
                newFileLinesContent.Add($"  /// {ZipParserService.MARKER_END_PROPERTIES}");

                // Write blocks ?
                if (indexBeginFirstBlock != -1 && indexEndLastBlock != -1)
                {
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
                            newFileLinesContent.Add("    ,");
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
                            newFileLinesContent.Add("    ,");
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

        private List<string> DeleteChildrenBlocks(List<string> fileLinesContent)
        {
            if (!fileLinesContent.Contains(ZipParserService.MARKER_BEGIN_CHILDREN))
            {
                return fileLinesContent;
            }

            List<string> updateLines = new();
            for (int i = 0; i < fileLinesContent.Count; i++)
            {
                string line = fileLinesContent[i];

                if (line.Contains(ZipParserService.MARKER_BEGIN_CHILDREN, StringComparison.InvariantCultureIgnoreCase) &&
                    line.Contains(ZipParserService.MARKER_END_CHILDREN, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Get data before marker begin + data after marker end
                    string[] splitBegin = line.Split(ZipParserService.MARKER_BEGIN_CHILDREN);
                    string[] splitEnd = line.Split(ZipParserService.MARKER_END_CHILDREN);
                    string update = splitBegin[0] + splitEnd[1];
                    if (!string.IsNullOrWhiteSpace(update))
                    {
                        updateLines.Add(update);
                    }
                }
                else if (line.Contains(ZipParserService.MARKER_BEGIN_CHILDREN, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Get data before marker begin
                    bool endFound = false;
                    string[] splitBegin = line.Split(ZipParserService.MARKER_BEGIN_CHILDREN);
                    if (!string.IsNullOrWhiteSpace(splitBegin[0]))
                    {
                        updateLines.Add(splitBegin[0]);
                    }

                    for (int j = i; j < fileLinesContent.Count && !endFound; j++)
                    {
                        line = fileLinesContent[j];
                        if (line.Contains(ZipParserService.MARKER_END_CHILDREN, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // Get data after marker end                               
                            string[] splitEnd = line.Split(ZipParserService.MARKER_END_CHILDREN);
                            if (!string.IsNullOrWhiteSpace(splitEnd[1]))
                            {
                                // Get firsts space characters (space + tabulations) to keep formating
                                string match = CommonTools.GetMatchRegexValue(@"^([\s\t]+)(\w+)", splitEnd[0]);
                                updateLines.Add(match + splitEnd[1]);
                            }

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

        private List<string> GeneratePropertiesToAdd(AngularCRUDData angularFile, KeyValuePair<string, List<string>> crudDtoProperty)
        {
            List<string> propertiesToAdd = new();

            List<ExtractBlocks> extractBlocksList = angularFile.ExtractBlocks.Where(x => x.DataUpdateType == CRUDDataUpdateType.Property).ToList();
            if (extractBlocksList != null && extractBlocksList.Count > 0)
            {
                string type = ConvertDotNetToAngularType(crudDtoProperty.Key);
                foreach (string attrName in crudDtoProperty.Value)
                {
                    string[] tmpType = type.Split('|'); // keep only type before '|' (ex: XXX | null)
                    ExtractBlocks block = extractBlocksList.FirstOrDefault(block => block.Type == tmpType[0].Trim());
                    if (block == null)
                    {
                        // Generate empty property
                        block = extractBlocksList[0];
                    }

                    string line = block.BlockLines.FirstOrDefault();
                    // Generate property
                    propertiesToAdd.Add(line.Replace(block.Name, CommonTools.ConvertToCamelCase(attrName)).Replace($" {block.Type}", $" {type}"));
                }
            }

            return propertiesToAdd;
        }

        private List<string> GenerateBlocksToAdd(AngularCRUDData angularFile, KeyValuePair<string, List<string>> crudDtoProperty)
        {
            List<string> blocksToAdd = new();

            ExtractBlocks extractBlock = angularFile.ExtractBlocks.Find(x => x.DataUpdateType == CRUDDataUpdateType.Block && x.Type == crudDtoProperty.Key);
            // If not found try type without '?' if is present
            extractBlock ??= angularFile.ExtractBlocks.Find(x => x.DataUpdateType == CRUDDataUpdateType.Block && x.Type == crudDtoProperty.Key.TrimEnd('?'));

            if (extractBlock != null)
            {
                if (extractBlock.BlockLines == null || extractBlock.BlockLines.Count <= 0)
                {
                    consoleWriter.AddMessageLine("Error 'extractBlock' (block) is empty.", "Red");
                    return null;
                }

                // Generate block based on dto model
                foreach (string attrName in crudDtoProperty.Value)
                {
                    blocksToAdd.Add(ReplaceBlock(extractBlock, attrName));
                }
            }
            else
            {
                // Generate "empty" block
                foreach (string attrName in crudDtoProperty.Value)
                {
                    ExtractBlocks defaultBlock = angularFile.ExtractBlocks.FirstOrDefault(x => x.DataUpdateType == CRUDDataUpdateType.Block);
                    if (defaultBlock != null)
                    {
                        blocksToAdd.Add(CreateEmptyBlock(defaultBlock, crudDtoProperty.Key, attrName));
                    }
                }
            }

            return blocksToAdd;
        }

        private string ConvertDotNetToAngularType(string dotnetType)
        {
            if (dotnetType == null) { return null; }

            string angularType = dotnetType;

            // In first : manage case of "Collection"
            string match = CommonTools.GetMatchRegexValue(@"<(\w+)>", angularType);
            if (!string.IsNullOrEmpty(match))
            {
                angularType = $"{match}[]";
            }

            // After verify other types
            match = CommonTools.GetMatchRegexValue(@"(\w+)(\W*)", angularType);
            if (!string.IsNullOrEmpty(match))
            {
                // Integer
                if (match.ToLower() == "int" || match.ToLower() == "long" || match.ToLower() == "float" || match.ToLower() == "double")
                    angularType = angularType.Replace(match, "number");

                // Boolean
                else if (match.ToLower() == "bool")
                    angularType = angularType.Replace(match, "boolean");

                // Date
                else if (match == "DateTime")
                    angularType = angularType.Replace(match, "Date");

                // XXXDto
                else if (match.EndsWith("Dto"))
                    angularType = angularType.Replace(match, "OptionDto");
            }

            if (angularType.EndsWith('?'))
                angularType = angularType.Replace("?", " | null");

            return angularType;
        }

        private string ReplaceBlock(ExtractBlocks extractBlock, string crudAttributeName, string dtoAttributeName = null)
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

        private string CreateEmptyBlock(ExtractBlocks extractBlock, string attributeType, string attributeName)
        {
            List<string> newBlockLines = new();
            int length = extractBlock.BlockLines.Count;

            if (length > 2)
            {
                extractBlock.BlockLines.RemoveAll(line => string.IsNullOrEmpty(line));
                string startBLockComment = extractBlock.BlockLines.First();
                string endBLockComment = extractBlock.BlockLines.Last();

                newBlockLines.Add(ATTRIBUTE_TYPE_NOT_MANAGED.Replace(ATTRIBUTE_TYPE_NOT_MANAGED_FIELD, attributeName).Replace(ATTRIBUTE_TYPE_NOT_MANAGED_TYPE, attributeType));
                newBlockLines.Add(extractBlock.BlockLines[0]);              // start block comment
                newBlockLines.Add(extractBlock.BlockLines[1]);              // first block code line
                newBlockLines.Add(extractBlock.BlockLines[length - 2]);     // last block code line
                newBlockLines.Add(extractBlock.BlockLines[length - 1]);     // end block comment
            }

            ExtractBlocks newBlock = new(CRUDDataUpdateType.Block, attributeType, attributeName, newBlockLines);
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
                case FeatureType.Back:
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
    }
}
