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
        private readonly IConsoleWriter consoleWriter;

        private const string ATTRIBUTE_TYPE_NOT_MANAGED = "// CRUD GENERATOR FOR PLANE TO REVIEW : Field " + ATTRIBUTE_TYPE_NOT_MANAGED_FIELD + " or type " + ATTRIBUTE_TYPE_NOT_MANAGED_TYPE + " not managed.";
        private const string ATTRIBUTE_TYPE_NOT_MANAGED_FIELD = "XXX";
        private const string ATTRIBUTE_TYPE_NOT_MANAGED_TYPE = "YYY";

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

        public string GenerateCrudFiles(Project currentProject, EntityInfo dtoEntity, List<ZipFilesContent> fileListFromZip, bool generateInCurrentProject = true)
        {
            string generationFolder = null;
            try
            {
                generationFolder = GetGenerationFolder(currentProject, generateInCurrentProject);

                string dotnetDir = Path.Combine(generationFolder, Constants.FolderDotNet);
                string angularDir = Path.Combine(generationFolder, Constants.FolderAngular);
                if (!generateInCurrentProject)
                {
                    // If generation not in current project, clean folders
                    CommonTools.PrepareFolder(dotnetDir);
                    CommonTools.PrepareFolder(angularDir);
                }

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

                    GenerateCRUD(angularDir, crudDtoProperties, crudFilesContent);
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

        public void InitRenameValues(string newValueSingular, string newValuePlurial,
            string oldCrudValueSingular = "Plane", string oldCrudValuePlurial = "Planes",
            string oldOptionValueSingular = "Airport", string oldOptionValuePlurial = "Airports",
            string oldTeamValueSingular = "XXX", string oldTeamValuePlurial = "XXXs")
        {
            this.NewCrudNamePascalSingular = newValueSingular;
            this.NewCrudNamePascalPlural = newValuePlurial;
            this.NewCrudNameCamelSingular = CommonTools.ConvertToCamelCase(NewCrudNamePascalSingular);
            this.NewCrudNameCamelPlural = CommonTools.ConvertToCamelCase(NewCrudNamePascalPlural);
            this.NewCrudNameKebabSingular = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalSingular);
            this.NewCrudNameKebabPlural = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalPlural);

            this.OldCrudNamePascalSingular = oldCrudValueSingular;
            this.OldCrudNamePascalPlural = oldCrudValuePlurial;
            this.OldOptionNamePascalSingular = oldOptionValueSingular;
            this.OldOptionNamePascalPlural = oldOptionValuePlurial;
            this.OldTeamNamePascalSingular = oldTeamValueSingular;
            this.OldTeamNamePascalPlural = oldTeamValuePlurial;

            this.OldCrudNameCamelSingular = CommonTools.ConvertToCamelCase(OldCrudNamePascalSingular);
            this.OldCrudNameCamelPlural = CommonTools.ConvertToCamelCase(OldCrudNamePascalPlural);
            this.OldOptionNameCamelSingular = CommonTools.ConvertToCamelCase(OldOptionNamePascalSingular);
            this.OldOptionNameCamelPlural = CommonTools.ConvertToCamelCase(OldOptionNamePascalPlural);
            this.OldTeamNameCamelSingular = CommonTools.ConvertToCamelCase(OldTeamNamePascalSingular);
            this.OldTeamNameCamelPlural = CommonTools.ConvertToCamelCase(OldTeamNamePascalPlural);
        }

        //        public string GenerateDotNetCrudFiles(Project currentProject, EntityInfo dtoEntity, List<ZipFilesContent> dotNetFilesFromZip, bool generateInCurrentProject = true)
        //        {
        //#if DEBUG
        //            consoleWriter.AddMessageLine($"*** Generate DotNet CRUD files on '{Path.Combine(currentProject.Folder, Constants.FolderCrudGeneration)}' ***", "Green");
        //#endif
        //            string generatedFolder = null;
        //            try
        //            {
        //                this.entityNameReference = ExtractEntityNameReference(classDefinitionFromZip);

        //                generatedFolder = GetGenerationFolder(currentProject, generateInCurrentProject);
        //                string dotnetDir = Path.Combine(generatedFolder, Constants.FolderDotNet);
        //                if (!generateInCurrentProject)
        //                    CommonTools.PrepareFolder(dotnetDir);

        //                // Application
        //                var cdias = classDefinitionFromZip.Where(x => x.FileType == FileType.IAppService).FirstOrDefault();
        //                if (cdias != null)
        //                {
        //                    GenerateIApplicationFile(dotnetDir, currentProject, dtoEntity, cdias);
        //                }

        //                var cdas = classDefinitionFromZip.Where(x => x.FileType == FileType.AppService).FirstOrDefault();
        //                if (cdas != null)
        //                {
        //                    GenerateApplicationFile(dotnetDir, currentProject, dtoEntity, cdas);
        //                }

        //                // Controller
        //                var cdc = classDefinitionFromZip.Where(x => x.FileType == FileType.Controller).FirstOrDefault();
        //                if (cdc != null)
        //                {
        //                    GenerateControllerFile(dotnetDir, currentProject, dtoEntity, cdc);
        //                }

        //                // IocContainer
        //                CreateIocContainerFile(dotnetDir);

        //                // Rights
        //                CreateRightsFile(dotnetDir, currentProject);
        //                CreateBIANETConfigFile(dotnetDir, currentProject);
        //            }
        //            catch (Exception ex)
        //            {
        //                consoleWriter.AddMessageLine($"An error has occurred in DotNet CRUD generation process: {ex.Message}", "Red");
        //            }

        //            return generatedFolder;
        //        }

        //        public void GenerateAngularCrudFiles(Project currentProject, EntityInfo dtoEntity, List<ZipFilesContent> angularFilesFromZip, bool generateInCurrentProject = true)
        //        {
        //#if DEBUG
        //            consoleWriter.AddMessageLine($"*** Generate Angular CRUD files on '{Path.Combine(currentProject.Folder, Constants.FolderCrudGeneration)}' ***", "Green");
        //#endif
        //            string generatedFolder = null;
        //            try
        //            {
        //                generatedFolder = GetGenerationFolder(currentProject, generateInCurrentProject);
        //                string angularDir = Path.Combine(generatedFolder, Constants.FolderAngular);
        //                if (!generateInCurrentProject)
        //                    CommonTools.PrepareFolder(angularDir);

        //                // Generate CRUD angular files
        //                ZipFilesContent crudFilesContent = angularFilesFromZip.Where(x => x.Type == FeatureType.CRUD).FirstOrDefault();
        //                if (crudFilesContent != null)
        //                {
        //                    // Get CRUD dto properties
        //                    Dictionary<string, List<string>> crudDtoProperties = GetDtoProperties(dtoEntity);

        //                    GenerateCRUD(angularDir, crudDtoProperties, crudFilesContent, crudFilesContent.Type);
        //                }

        //                // Generate Option angular files
        //                ZipFilesContent optionFilesContent = angularFilesFromZip.Where(x => x.Type == FeatureType.Option).FirstOrDefault();
        //                if (optionFilesContent != null)
        //                {
        //                    GenerateOption(angularDir, optionFilesContent, optionFilesContent.Type);
        //                }

        //                // TODO NMA
        //                // Generate Team angular files
        //                //AngularZipFilesContent teamFilesContent = angularFilesFromZip.Where(x => x.Type == FeatureType.Team).FirstOrDefault();
        //                //if (crudFilesContent != null)
        //                //{
        //                //    toto(angularDir, crudDtoProperties, crudFilesContent);
        //                //}

        //            }
        //            catch (Exception ex)
        //            {
        //                consoleWriter.AddMessageLine($"An error has occurred in Angular CRUD generation process: {ex.Message}", "Red");
        //            }
        //        }


        private void GenerateBack(string dotNetDir, ZipFilesContent zipFilesContent, Project currentProject)
        {
            try
            {
                foreach (DotNetCRUDData crudData in zipFilesContent.FeatureDataList)
                {
                    // Ignore Dto file : not necessary to regenerate it
                    if (crudData.ClassFileDefinition.FileType == FileType.Dto) { continue; }

                    GenerateDotNetCrudFile(dotNetDir, currentProject, crudData);
                }

                // IocContainer
                CreateIocContainerFile(dotNetDir);

                // Rights
                CreateRightsFile(dotNetDir, currentProject);
                CreateBIANETConfigFile(dotNetDir, currentProject);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in DotNet CRUD generation process: {ex.Message}", "Red");
            }
        }

        #region Angular
        private void GenerateCRUD(string angularDir, Dictionary<string, List<string>> crudDtoProperties, ZipFilesContent zipFilesContent)
        {
            try
            {
                List<string> blocksToAdd;
                List<string> propertiesToAdd;
                List<string> childrenToDelete;
                List<string> optionToDelete;

                foreach (AngularCRUDData angularFile in zipFilesContent.FeatureDataList)
                {
                    blocksToAdd = new();
                    propertiesToAdd = new();
                    childrenToDelete = new();
                    optionToDelete = new();
                    if (angularFile.ExtractBlocks != null && angularFile.ExtractBlocks.Count > 0)
                    {
                        foreach (KeyValuePair<string, List<string>> crudDtoProperty in crudDtoProperties)
                        {
                            // Generate new properties to add
                            propertiesToAdd.AddRange(GeneratePropertiesToAdd(angularFile, crudDtoProperty));
                            // Generate new blocks to add
                            blocksToAdd.AddRange(GenerateBlocksToAdd(angularFile, crudDtoProperty));
                        }

                        // Get Children line to delete
                        var childrenBlocs = angularFile.ExtractBlocks.Where(l => l.DataUpdateType == CRUDDataUpdateType.Children);
                        if (childrenBlocs != null)
                        {
                            childrenBlocs.ToList().ForEach(b => childrenToDelete.AddRange(b.BlockLines));
                        }
                    }

                    if (angularFile.OptionToDelete != null && angularFile.OptionToDelete.Count > 0)
                    {
                        // Get lines to delete
                        optionToDelete = angularFile.OptionToDelete;
                    }

                    // Create file
                    string src = Path.Combine(angularFile.ExtractDirPath, angularFile.FilePath);
                    string dest = ConvertCamelToKebabCrudName(Path.Combine(angularDir, angularFile.FilePath), FeatureType.CRUD);

                    // replace blocks !
                    GenerateAngularFile(FeatureType.CRUD, src, dest, propertiesToAdd, blocksToAdd, childrenToDelete, optionToDelete);
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
            // TODO NMA
        }
        #endregion

        #region DotNet Files
        private void GenerateDotNetCrudFile(string destDir, Project currentProject, DotNetCRUDData crudData)
        {
            string src = Path.Combine(crudData.ExtractDirPath, crudData.FilePath);
            string dest = ConvertPascalOldToNewCrudName(Path.Combine(destDir, crudData.FilePath), FeatureType.Back);
            dest = ReplaceCompagnyNameProjetName(dest, currentProject, crudData.ClassFileDefinition);

            // Prepare destination folder
            CommonTools.CheckFolder(new FileInfo(dest).DirectoryName);

            // Read file
            List<string> fileLinesContent = File.ReadAllLines(src).ToList();

            // Replace Compagny name and Project name
            for (int i = 0; i < fileLinesContent.Count; i++)
            {
                fileLinesContent[i] = ReplaceCompagnyNameProjetName(fileLinesContent[i], currentProject, crudData.ClassFileDefinition);
                fileLinesContent[i] = ConvertPascalOldToNewCrudName(fileLinesContent[i], FeatureType.Back);
            }

            // Generate new file
            GenerateFile(dest, fileLinesContent);
        }
        #endregion

        #region IocContainer
        private void CreateIocContainerFile(string destDir)
        {
            StringBuilder sb = new();
            sb.AppendLine("***** Add the following line(s) at the end of 'ConfigureApplicationContainer' method : ");
            sb.Append($"collection.AddTransient<I{this.NewCrudNamePascalSingular}AppService, {this.NewCrudNamePascalSingular}AppService>();");

            CommonMethods.CreateFile(sb, Path.Combine(destDir, "UPDATE__IocContainer.cs__.txt"));
        }
        #endregion

        #region Rights
        private void CreateRightsFile(string destDir, Project currentProject)
        {
            StringBuilder sb = new();
            sb.AppendLine($"***** Add the following line(s) at the end of '{currentProject.CompanyName}.{currentProject.Name}.Crosscutting.Common/Rights.cs' file:");

            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// The {this.NewCrudNamePascalSingular} rights.");
            sb.AppendLine("/// </summary>");
            sb.AppendLine($"public static class {this.NewCrudNamePascalPlural}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// The right to access to the list of {this.NewCrudNamePascalPlural}.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public const string List = \"{this.NewCrudNamePascalSingular}_List\";{Environment.NewLine}");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// The right to read {this.NewCrudNamePascalPlural}.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public const string Read = \"{this.NewCrudNamePascalSingular}_Read\";{Environment.NewLine}");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// The right to create {this.NewCrudNamePascalPlural}.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public const string Create = \"{this.NewCrudNamePascalSingular}_Create\";{Environment.NewLine}");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// The right to update {this.NewCrudNamePascalPlural}.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public const string Update = \"{this.NewCrudNamePascalSingular}_Update\";{Environment.NewLine}");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// The right to delete {this.NewCrudNamePascalPlural}.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public const string Delete = \"{this.NewCrudNamePascalSingular}_Delete\";{Environment.NewLine}");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// The right to save {this.NewCrudNamePascalPlural}.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public const string Save = \"{this.NewCrudNamePascalSingular}_Save\";{Environment.NewLine}");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// The right to access to the list of {this.NewCrudNamePascalPlural}.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public const string ListAccess = \"{this.NewCrudNamePascalSingular}_List_Access\";");
            sb.AppendLine("}");

            CommonMethods.CreateFile(sb, Path.Combine(destDir, "UPDATE__Rights.cs__.txt"));
        }

        private void CreateBIANETConfigFile(string destDir, Project currentProject)
        {
            StringBuilder sb = new();
            sb.AppendLine($"***** Add the following line(s) at the end of '{currentProject.CompanyName}.{currentProject.Name}.Presentation.Api/bianetconfig.json' file:");

            sb.AppendLine($"// {this.NewCrudNamePascalSingular}");
            sb.AppendLine("{");
            sb.AppendLine($"\"Names\": [ \"{this.NewCrudNamePascalSingular}_Create\", \"{this.NewCrudNamePascalSingular}_Update\", \"{this.NewCrudNamePascalSingular}_Delete\", \"{this.NewCrudNamePascalSingular}_Save\", \"{this.NewCrudNamePascalSingular}_List_Access\" ],");
            sb.AppendLine("\"Roles\": [ \"Admin\", \"Site_Admin\" ]");
            sb.AppendLine("},");
            sb.AppendLine("{");
            sb.AppendLine($"\"Names\": [ \"{this.NewCrudNamePascalSingular}_List\", \"{this.NewCrudNamePascalSingular}_Read\" ]");
            sb.AppendLine("\"Roles\": [ \"Admin\", \"Site_Admin\", \"User\" ]");
            sb.AppendLine("}");

            CommonMethods.CreateFile(sb, Path.Combine(destDir, "UPDATE__bianetconfig.json__.txt"));
        }
        #endregion

        #region Tools
        private string GetGenerationFolder(Project currentProject, bool generateInCurrentProject)
        {
            string generatedFolder = Path.Combine(currentProject.Folder, currentProject.Name);

            if (!generateInCurrentProject)
                generatedFolder = Path.Combine(generatedFolder, Constants.FolderCrudGeneration);

            return generatedFolder;
        }

        private void GenerateFile(string fileName, List<string> fileLinesContent)
        {
            // Generate new file
            StringBuilder sb = new();
            fileLinesContent.ForEach(line => sb.AppendLine(line));

            File.WriteAllText(fileName, sb.ToString());
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
            GenerateFile(newFileName, fileLinesContent);
        }

        private List<string> ReplacePropertiesAndBlocks(string fileName, List<string> fileLinesContent, List<string> propertiesToAdd, List<string> blocksToAdd)
        {
            if ((propertiesToAdd == null || propertiesToAdd.Count <= 0) && (blocksToAdd == null || blocksToAdd.Count <= 0)) return fileLinesContent;


            int indexBeginProperty = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains(ZipParserService.ANGULAR_MARKER_BEGIN_PROPERTIES)).FirstOrDefault());
            int indexEndProperty = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains(ZipParserService.ANGULAR_MARKER_END_PROPERTIES)).FirstOrDefault());

            int indexBeginFirstBlock = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains(ZipParserService.ANGULAR_MARKER_BEGIN_BLOCK)).FirstOrDefault());
            int indexEndLastBlock = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains(ZipParserService.ANGULAR_MARKER_END_BLOCK)).LastOrDefault());

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
                newFileLinesContent.Add($"  /// {ZipParserService.ANGULAR_MARKER_BEGIN_PROPERTIES}");
                for (int i = 0; i < propertiesToAdd.Count; i++)
                {
                    newFileLinesContent.Add(propertiesToAdd[i]);
                }
                newFileLinesContent.Add($"  /// {ZipParserService.ANGULAR_MARKER_END_PROPERTIES}");

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
            if (!fileLinesContent.Contains(ZipParserService.ANGULAR_MARKER_BEGIN_CHILDREN))
            {
                return fileLinesContent;
            }

            List<string> updateLines = new();
            for (int i = 0; i < fileLinesContent.Count; i++)
            {
                string line = fileLinesContent[i];

                if (line.Contains(ZipParserService.ANGULAR_MARKER_BEGIN_CHILDREN, StringComparison.InvariantCultureIgnoreCase) &&
                    line.Contains(ZipParserService.ANGULAR_MARKER_END_CHILDREN, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Get data before marker begin + data after marker end
                    string[] splitBegin = line.Split(ZipParserService.ANGULAR_MARKER_BEGIN_CHILDREN);
                    string[] splitEnd = line.Split(ZipParserService.ANGULAR_MARKER_END_CHILDREN);
                    string update = splitBegin[0] + splitEnd[1];
                    if (!string.IsNullOrWhiteSpace(update))
                    {
                        updateLines.Add(update);
                    }
                }
                else if (line.Contains(ZipParserService.ANGULAR_MARKER_BEGIN_CHILDREN, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Get data before marker begin
                    bool endFound = false;
                    string[] splitBegin = line.Split(ZipParserService.ANGULAR_MARKER_BEGIN_CHILDREN);
                    if (!string.IsNullOrWhiteSpace(splitBegin[0]))
                    {
                        updateLines.Add(splitBegin[0]);
                    }

                    for (int j = i; j < fileLinesContent.Count && !endFound; j++)
                    {
                        line = fileLinesContent[j];
                        if (line.Contains(ZipParserService.ANGULAR_MARKER_END_CHILDREN, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // Get data after marker end                               
                            string[] splitEnd = line.Split(ZipParserService.ANGULAR_MARKER_END_CHILDREN);
                            if (!string.IsNullOrWhiteSpace(splitEnd[1]))
                            {
                                // Get firsts space characters (space + tabulations) to keep formating
                                string match = GetMatchRegexValue(@"^([\s\t]+)(\w+)", splitEnd[0]);
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

        private List<string> GeneratePropertiesToAdd(FeatureData angularFile, KeyValuePair<string, List<string>> crudDtoProperty)
        {
            List<string> propertiesToAdd = new();

            List<ExtractBlocks> extractBlocksList = angularFile.ExtractBlocks.Where(x => x.DataUpdateType == CRUDDataUpdateType.Property).ToList();
            if (extractBlocksList != null && extractBlocksList.Count > 0)
            {
                string type = ConvertDotNetToAngularType(crudDtoProperty.Key);
                foreach (string attrName in crudDtoProperty.Value)
                {
                    ExtractBlocks block = extractBlocksList.FirstOrDefault(block => block.Type == type);
                    if (block == null)
                    {
                        // Generate empty property
                        block = extractBlocksList[0];
                    }

                    string line = block.BlockLines.FirstOrDefault();
                    // Generate property
                    propertiesToAdd.Add(line.Replace(block.Name, CommonTools.ConvertToCamelCase(attrName)).Replace(block.Type, type));
                }
            }

            return propertiesToAdd;
        }

        private List<string> GenerateBlocksToAdd(FeatureData angularFile, KeyValuePair<string, List<string>> crudDtoProperty)
        {
            List<string> blocksToAdd = new();

            ExtractBlocks extractBlock = angularFile.ExtractBlocks.Find(x => x.DataUpdateType == CRUDDataUpdateType.Block && x.Type == crudDtoProperty.Key);
            if (extractBlock != null)
            {
                if (extractBlock.BlockLines == null || extractBlock.BlockLines.Count <= 0)
                {
                    // TODO NMA!
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
            string match = GetMatchRegexValue(@"<(\w+)>", angularType);
            if (!string.IsNullOrEmpty(match))
            {
                angularType = $"{match}[]";
            }

            // After verify other types
            match = GetMatchRegexValue(@"(\w+)(\W*)", angularType);
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

        private string GetMatchRegexValue(string pattern, string data)
        {
            MatchCollection matches = new Regex(pattern).Matches(data);
            if (matches != null && matches.Count > 0)
            {
                GroupCollection groups = matches[0].Groups;
                if (groups.Count > 0)
                {
                    return groups[1].Value;
                }
            }
            return null;
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
