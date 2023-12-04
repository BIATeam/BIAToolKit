namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private string OldCrudNamePascalSingular = "Plane";
        private string OldCrudNamePascalPlural = "Planes";
        private string OldCrudNameCamelSingular = "plane";
        private string OldCrudNameCamelPlural = "planes";

        private string NewCrudNamePascalSingular = "Plane";
        private string NewCrudNamePascalPlural = "Planes";
        private string NewCrudNameCamelSingular = "plane";
        private string NewCrudNameCamelPlural = "planes";
        private string NewCrudNameKebabSingular;
        private string NewCrudNameKebabPlural;

        private string entityNameReference;
        private string entityNameGenerated;

        public GenerateCrudService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public string GenerateDotNetCrudFiles(string entityName, Project currentProject, EntityInfo dtoEntity, List<ClassDefinition> classDefinitionFromZip)
        {
#if DEBUG
            consoleWriter.AddMessageLine($"*** Generate DotNet CRUD files on '{Path.Combine(currentProject.Folder, Constants.FolderCrudGeneration)}' ***", "Green");
#endif

            string generatedFolder = null;
            try
            {
                this.entityNameReference = ExtractEntityNameReference(classDefinitionFromZip);
                this.entityNameGenerated = entityName;

                generatedFolder = Path.Combine(currentProject.Folder, currentProject.Name, Constants.FolderCrudGeneration);
                string dotnetDir = Path.Combine(generatedFolder, Constants.FolderDotNet);
                CommonTools.PrepareFolder(dotnetDir);

                // Application
                GenerateIApplicationFile(dotnetDir, currentProject, dtoEntity, classDefinitionFromZip.Where(x => x.FileType == FileType.IAppService).First());
                GenerateApplicationFile(dotnetDir, currentProject, dtoEntity, classDefinitionFromZip.Where(x => x.FileType == FileType.AppService).First());

                // Controller
                GenerateControllerFile(dotnetDir, currentProject, dtoEntity, classDefinitionFromZip.Where(x => x.FileType == FileType.Controller).First());

                // IocContainer
                CreateIocContainerFile(dotnetDir);

                // Rights
                CreateRightsFile(dotnetDir, currentProject);
                CreateBIANETConfigFile(dotnetDir, currentProject);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in DotNet CRUD generation process: {ex.Message}", "Red");
            }

            return generatedFolder;
        }

        public void GenerateAngularCrudFiles(string entityName, Project currentProject, EntityInfo dtoEntity, List<CRUDAngularData> angularFilesFromZip/*, ClassDefinition cd*/)
        {
#if DEBUG
            consoleWriter.AddMessageLine($"*** Generate Angular CRUD files on '{Path.Combine(currentProject.Folder, Constants.FolderCrudGeneration)}' ***", "Green");
#endif
            try
            {
                string generatedFolder = Path.Combine(currentProject.Folder, currentProject.Name, Constants.FolderCrudGeneration);
                string angularDir = Path.Combine(generatedFolder, Constants.FolderAngular);
                CommonTools.PrepareFolder(angularDir);

                Dictionary<string, List<string>> crudDtoProperties = GetDtoProperties(dtoEntity);

                foreach (CRUDAngularData angularFile in angularFilesFromZip)
                {
                    List<string> blocksToAdd = new();
                    List<string> propertiesToAdd = new();
                    if (angularFile.ExtractBlocks != null && angularFile.ExtractBlocks.Count > 0)
                    {
                        foreach (KeyValuePair<string, List<string>> crudDtoProperty in crudDtoProperties)
                        {
                            // Generate new properties to add
                            propertiesToAdd.AddRange(GeneratePropertiesToAdd(angularFile, crudDtoProperty));
                            // Generate new blocks to add
                            blocksToAdd.AddRange(GenerateBlocksToAdd(angularFile, crudDtoProperty));
                        }
                    }

                    // Create file
                    string src = Path.Combine(angularFile.TempDirPath, angularFile.FileName);
                    string dest = ConvertOldCrudNameToNewCrudName(Path.Combine(angularDir, angularFile.FilePathDest));

                    // replace blocks !
                    UpdateFile(src, dest, propertiesToAdd, blocksToAdd);
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in Angular CRUD generation process: {ex.Message}", "Red");
            }
        }

        #region DotNet Files
        #region Entity
        private void GenerateEntityFile(string destDir, Project currentProject, EntityInfo dtoEntity, ClassDefinition classDefinition)
        {
            //this.entityNameReference = classDefinition.Name.Text;
            //string fileName = $"{entityNameGenerated}.cs";
            //StringBuilder sb = new();

            //// Generate file header
            //GenerateFileHeader(sb, fileName, currentProject.CompanyName);

            //// Generate namespace + using
            //GenerateNamespaceUsing(sb, dtoEntity, classDefinition);

            //// Generate class declaration
            //GenerateClassDeclaration(sb, classDefinition, $"The {entityNameGenerated} entity");

            //// Generate primary key
            //if (dtoEntity.PrimaryKey.ToLower() == "int")
            //{
            //    var prop = classDefinition.PropertyList.Where(x => x.Identifier.Text.ToLower() == "id").First();
            //    if (prop != null)
            //    {
            //        GenerateProperty(sb, prop.Type.ToFullString(), prop.Identifier.ToFullString(), string.Join(' ', prop.Modifiers.ToList()));
            //    }
            //}

            //// Generate properties
            //foreach (PropertyInfo p in dtoEntity.Properties)
            //{
            //    if (p.Type == "int" && p.Name.ToLower() == "siteid")    // int SiteId
            //    {
            //        string newName = p.Name[..^2]; // Name without "Id" suffix
            //        GenerateProperty(sb, $"{newName}", $"{newName}", "public virtual");
            //        GenerateProperty(sb, p.Type, p.Name);
            //    }
            //    else if (p.Type == "OptionDto")                         // Type OptionDto  
            //    {
            //        GenerateProperty(sb, p.Type, p.Name);
            //        sb.AppendLine($"        //****************************************************************************************");
            //        sb.AppendLine($"        //****************** Rework OptionDto by 2 lines: type + id ******************************");
            //        sb.AppendLine($"        // public int {p.Name}Id {{ get; set; }}");
            //        sb.AppendLine($"        // public virtual {p.Name} {p.Name} {{ get; set; }}");
            //        sb.AppendLine($"        //****************************************************************************************");
            //        sb.AppendLine($"        //****************************************************************************************");
            //    }
            //    else if (p.Type.EndsWith("Dto"))                        // Type XXXDto
            //    {
            //        string newType = p.Type[..^3]; // Type without "Dto" suffix
            //        GenerateProperty(sb, $"{newType}", p.Name, "public virtual");
            //        GenerateProperty(sb, "int", $"{p.Name}Id");
            //    }
            //    else if (p.Type.EndsWith("Dto>"))                       // Collection of XXXDto
            //    {
            //        string newType = p.Type.Replace("Dto", ""); // Type without "Dto" suffix
            //        GenerateProperty(sb, $"{newType}", p.Name);
            //    }
            //    else                                                    // All others cases     
            //    {
            //        GenerateProperty(sb, p.Type, p.Name);
            //    }
            //}
            //sb.AppendLine($"    }}");
            //sb.AppendLine($"}}");

            //// Create generated file on disk            
            //CommonMethods.CreateFile(sb, Path.Combine(destDir, domainFolder, fileName));
        }
        #endregion

        #region Application
        private void GenerateIApplicationFile(string destDir, Project currentProject, EntityInfo dtoEntity, ClassDefinition classDefinition)
        {
            string fileName = GetFileName(classDefinition.Name.Text);
            string destinationPath = GetFinalFilePath(destDir, dtoEntity, classDefinition);
            StringBuilder sb = new();

            // Generate file header
            GenerateFileHeader(sb, fileName, currentProject.CompanyName);

            // Generate namespace + using
            GenerateNamespaceUsing(sb, dtoEntity, classDefinition);

            // Generate interface declaration
            GenerateClassDeclaration(sb, classDefinition, $"The interface defining the application service for {entityNameGenerated}");

            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, destinationPath);
        }

        private void GenerateApplicationFile(string destDir, Project currentProject, EntityInfo dtoEntity, ClassDefinition classDefinition)
        {
            string fileName = GetFileName(classDefinition.Name.Text);
            string destinationPath = GetFinalFilePath(destDir, dtoEntity, classDefinition);
            StringBuilder sb = new();

            // Generate file header
            GenerateFileHeader(sb, fileName, currentProject.CompanyName);

            // Generate namespace + using
            GenerateNamespaceUsing(sb, dtoEntity, classDefinition);

            // Generate class declaration
            GenerateClassDeclaration(sb, classDefinition, $"The application service used for {entityNameGenerated}");

            // Generate class fields
            foreach (FieldDeclarationSyntax fd in classDefinition.FieldList)
            {
                GenerateField(sb, fd.Declaration.ToString(), string.Join(' ', fd.Modifiers.ToList()));
            }

            // Generate class constructors
            foreach (ConstructorDeclarationSyntax cd in classDefinition.ConstructorList)
            {
                GenerateConstructor(sb, cd.Identifier.ToString(), string.Join(' ', cd.ParameterList),
                    cd.Initializer?.ToFullString(), cd.Body?.ToFullString(), string.Join(' ', cd.Modifiers.ToList()));
            }

            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, destinationPath);
        }
        #endregion

        #region Controller
        private void GenerateControllerFile(string destDir, Project currentProject, EntityInfo dtoEntity, ClassDefinition classDefinition)
        {
            string fileName = GetFileName(classDefinition.Name.Text);
            string destinationPath = GetFinalFilePath(destDir, dtoEntity, classDefinition);
            StringBuilder sb = new();

            // Generate file header
            GenerateFileHeader(sb, fileName, currentProject.CompanyName);

            // Generate namespace + using
            GenerateNamespaceUsing(sb, dtoEntity, classDefinition);

            // Generate class declaration
            GenerateClassDeclaration(sb, classDefinition, $"The API controller used to manage {entityNameGenerated}");

            // Generate class fields
            foreach (FieldDeclarationSyntax fd in classDefinition.FieldList)
            {
                GenerateField(sb, fd.Declaration.ToFullString(), string.Join(' ', fd.Modifiers.ToList()));
            }

            // Generate class constructors
            foreach (ConstructorDeclarationSyntax cd in classDefinition.ConstructorList)
            {
                GenerateConstructor(sb, cd.Identifier.ToFullString(), string.Join(' ', cd.ParameterList),
                    cd.Initializer?.ToFullString(), cd.Body?.ToFullString(), string.Join(' ', cd.Modifiers.ToList()));
            }

            foreach (MethodDeclarationSyntax md in classDefinition.MethodList)
            {
                GenerateMethod(sb, md.AttributeLists.ToList(), md.ReturnType.ToFullString(),
                    md.Identifier.ToFullString(), string.Join(' ', md.ParameterList), md.Body?.ToFullString(), string.Join(' ', md.Modifiers.ToList()));
            }

            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, destinationPath);
        }
        #endregion

        #region IocContainer
        private void CreateIocContainerFile(string destDir)
        {
            StringBuilder sb = new();
            sb.AppendLine("***** Add the following line(s) at the end of 'ConfigureApplicationContainer' method : ");
            sb.Append($"collection.AddTransient<I{entityNameGenerated}AppService, {entityNameGenerated}AppService>();");

            CommonMethods.CreateFile(sb, Path.Combine(destDir, "UPDATE__IocContainer.cs__.txt"));
        }
        #endregion

        #region Rights
        private void CreateRightsFile(string destDir, Project currentProject)
        {
            StringBuilder sb = new();
            sb.AppendLine($"***** Add the following line(s) at the end of '{currentProject.CompanyName}.{currentProject.Name}.Crosscutting.Common/Rights.cs' file:");

            sb.AppendLine($"/// <summary>");
            sb.AppendLine($"/// The {entityNameGenerated} rights.");
            sb.AppendLine($"/// </summary>");
            sb.AppendLine($"public static class {entityNameGenerated}");
            sb.AppendLine($"{{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to access to the list of {entityNameGenerated}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string List = \"{entityNameGenerated}_List\";{Environment.NewLine}");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to read {entityNameGenerated}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string Read = \"{entityNameGenerated}_Read\";{Environment.NewLine}");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to create {entityNameGenerated}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string Create = \"{entityNameGenerated}_Create\";{Environment.NewLine}");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to update {entityNameGenerated}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string Update = \"{entityNameGenerated}_Update\";{Environment.NewLine}");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to delete {entityNameGenerated}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string Delete = \"{entityNameGenerated}_Delete\";{Environment.NewLine}");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to save {entityNameGenerated}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string Save = \"{entityNameGenerated}_Save\";{Environment.NewLine}");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to access to the list of {entityNameGenerated}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string ListAccess = \"{entityNameGenerated}_List_Access\";");
            sb.AppendLine($"}}");

            CommonMethods.CreateFile(sb, Path.Combine(destDir, "UPDATE__Rights.cs__.txt"));
        }

        private void CreateBIANETConfigFile(string destDir, Project currentProject)
        {
            StringBuilder sb = new();
            sb.AppendLine($"***** Add the following line(s) at the end of '{currentProject.CompanyName}.{currentProject.Name}.Presentation.Api/bianetconfig.txt' file:");

            sb.AppendLine($"// {entityNameGenerated}");
            sb.AppendLine($"{{");
            sb.AppendLine($"\"Names\": [ \"{entityNameGenerated}_Create\", \"{entityNameGenerated}_Update\", \"{entityNameGenerated}_Delete\", \"{entityNameGenerated}_Save\", \"{entityNameGenerated}_List_Access\" ],");
            sb.AppendLine($"\"Roles\": [ \"Admin\", \"Site_Admin\" ]");
            sb.AppendLine($"}},");
            sb.AppendLine($"{{");
            sb.AppendLine($"\"Names\": [ \"{entityNameGenerated}_List\", \"{entityNameGenerated}_Read\" ]");
            sb.AppendLine($"\"Roles\": [ \"Admin\", \"Site_Admin\", \"User\" ]");
            sb.AppendLine($"}}");

            CommonMethods.CreateFile(sb, Path.Combine(destDir, "UPDATE__bianetconfig.txt__.txt"));
        }
        #endregion

        #region File Generation Tools
        private void GenerateFileHeader(StringBuilder sb, string fileName, string companyName)
        {
            sb.AppendLine($"// <copyright file=\"{fileName}\" company=\"{companyName}\">");
            sb.AppendLine($"//     Copyright (c) {companyName}. All rights reserved.");
            sb.AppendLine($"// </copyright>");
        }

        private void GenerateNamespaceUsing(StringBuilder sb, EntityInfo dtoEntity, ClassDefinition classDefinition)
        {
            // Generate namespace
            var @namespace = TransformCompleteValue(classDefinition.NamespaceSyntax.Name.ToFullString(), dtoEntity, classDefinition);
            sb.Append($"{classDefinition.NamespaceSyntax.NamespaceKeyword} {@namespace}");
            sb.Append("{{");

            // Generate using
            sb.AppendLine();
            classDefinition.NamespaceSyntax.Usings.ToList().ForEach(@using =>
            {
                var usingFormated = TransformCompleteValue(@using.ToFullString(), dtoEntity, classDefinition);
                sb.Append($"{usingFormated}");
            });

            sb.AppendLine();
        }

        private void GenerateClassDeclaration(StringBuilder sb, ClassDefinition classDefinition, string comment)
        {
            string baselist = TransformValue(classDefinition.BaseList.ToFullString());
            string className = TransformValue(classDefinition.Name.Text);

            string type = "class";
            switch (classDefinition.Type)
            {
                case SyntaxKind.ClassDeclaration:
                    type = "class";
                    break;
                case SyntaxKind.InterfaceDeclaration:
                    type = "interface";
                    break;
                case SyntaxKind.EnumDeclaration:
                    type = "enum";
                    break;
                case SyntaxKind.StructDeclaration:
                    type = "struct";
                    break;
            }

            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {comment}.");
            sb.AppendLine($"    /// </summary>");
            sb.Append($"    {classDefinition.VisibilityList} {type} {className} {baselist}");
            sb.AppendLine($"    {{");
        }

        private void GenerateProperty(StringBuilder sb, string type, string property, string modifier = "public")
        {
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Gets or Sets {property}.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        {modifier} {type} {property} {{ get; set; }}{Environment.NewLine}");
        }

        private void GenerateField(StringBuilder sb, string fieldDef, string modifier = "public")
        {
            var tmp = fieldDef.Split(' ');
            string type = TransformValue(tmp[0]);
            string field = TransformValue(tmp[1]);

            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// The {field}.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        {modifier} {type} {field}{Environment.NewLine}");
        }

        private void GenerateConstructor(StringBuilder sb, string constructorName, string parameters, string baseList, string body, string modifier = "public")
        {
            constructorName = TransformValue(constructorName);
            parameters = TransformValue(parameters);
            baseList = TransformValue(baseList);
            body = TransformValue(body);

            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Initializes a new instance of the <see cref=\"{constructorName}\"/> class.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        {modifier} {constructorName}{parameters}");
            sb.AppendLine($"{baseList}{body}{Environment.NewLine}");
        }

        private void GenerateMethod(StringBuilder sb, List<AttributeListSyntax> attributeList, string type, string methodName, string parameters, string body, string modifier = "public")
        {
            parameters = TransformValue(parameters);
            body = TransformValue(body);

            //sb.AppendLine($"        /// <summary>");
            //sb.AppendLine($"        /// XXXX");
            //sb.AppendLine($"        /// </summary>");
            attributeList.ForEach(attr =>
            {
                string attrFormated = TransformValue(attr.ToFullString());
                sb.Append($"{attrFormated}");
            });
            sb.AppendLine($"        {modifier} {type}{methodName}{parameters}");
            sb.AppendLine($"{body}{Environment.NewLine}");
        }
        #endregion

        #region Tools
        private string ExtractEntityNameReference(List<ClassDefinition> classDefinitionFromZip)
        {
            ClassDefinition classNameDef = classDefinitionFromZip.Where(x => x.FileType == FileType.Entity).FirstOrDefault();
            if (classNameDef != null)
            {
                return classNameDef.Name.Text;
            }

            classNameDef = classDefinitionFromZip.Where(x => x.FileType == FileType.Dto).FirstOrDefault();
            if (classNameDef != null)
            {
                return GetEntityNameFromDto(classNameDef.Name.Text);
            }

            throw new Exception("Zip not contains Entity or Dto file.");
        }

        private string GetFinalFilePath(string destDir, EntityInfo dtoEntity, ClassDefinition classDefinition)
        {
            // Get final path
            string pathOnZip = TransformCompleteValue(classDefinition.PathOnZip, dtoEntity, classDefinition);
            string destinationPath = Path.Combine(destDir, pathOnZip);

            // Get parent folder and create it
            Directory.CreateDirectory(new FileInfo(destinationPath).DirectoryName);

            return destinationPath;
        }

        private string TransformCompleteValue(string value, EntityInfo dtoEntity, ClassDefinition classDefinition)
        {
            string formatedValue = value.Replace(classDefinition.CompagnyName, dtoEntity.CompagnyName).
                Replace(classDefinition.ProjectName, dtoEntity.ProjectName);

            return TransformValue(formatedValue);
        }

        private string TransformValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            string referenceFormated = CommonTools.ConvertToCamelCase(entityNameReference);
            string generatedFormated = CommonTools.ConvertToCamelCase(entityNameGenerated);

            return value.Replace(entityNameReference, entityNameGenerated).Replace(referenceFormated, generatedFormated);
        }

        private string GetFileName(string value)
        {
            string className = TransformValue(value);
            return $"{className}.cs";
        }
        #endregion

        public string GetEntityNameFromDto(string dtoFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(dtoFileName);
            if (!string.IsNullOrWhiteSpace(fileName) && fileName.ToLower().EndsWith("dto"))
            {
                return fileName[..^3];   // name without 'dto' suffix
            }

            return fileName;
        }
        #endregion

        #region Angular Files
        private void UpdateFile(string fileName, string newFileName, List<string> propertiesToAdd, List<string> blocksToAdd)
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

            // Replace blocks
            if (propertiesToAdd.Count > 0 || blocksToAdd.Count > 0)
            {
                int indexBeginProperty = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains(ZipParserService.ANGULAR_MARKER_BEGIN_PROPERTIES)).FirstOrDefault());
                int indexEndProperty = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains(ZipParserService.ANGULAR_MARKER_END_PROPERTIES)).FirstOrDefault());

                int indexBeginFirstBlock = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains(ZipParserService.ANGULAR_MARKER_BEGIN_BLOCK)).FirstOrDefault());
                int indexEndLastBlock = fileLinesContent.IndexOf(fileLinesContent.Where(l => l.Contains(ZipParserService.ANGULAR_MARKER_END_BLOCK)).LastOrDefault());

                StringBuilder sb = new();
                // Writes properties ?
                if (indexBeginProperty != -1 && indexEndProperty != -1)
                {
                    // Write lines before properties
                    for (int i = 0; i < indexBeginProperty; i++)
                    {
                        sb.AppendLine(fileLinesContent[i]);
                    }

                    // Write new properties to add
                    sb.AppendLine($"  /// {ZipParserService.ANGULAR_MARKER_BEGIN_PROPERTIES}");
                    for (int i = 0; i < propertiesToAdd.Count; i++)
                    {
                        sb.AppendLine(propertiesToAdd[i]);
                    }
                    sb.AppendLine($"  /// {ZipParserService.ANGULAR_MARKER_END_PROPERTIES}");

                    // Write blocks ?
                    if (indexBeginFirstBlock != -1 && indexEndLastBlock != -1)
                    {
                        // Write lines between properties and first block
                        for (int i = indexEndProperty + 1; i < indexBeginFirstBlock; i++)
                        {
                            sb.AppendLine(fileLinesContent[i]);
                        }

                        // Write blocks to add
                        for (int i = 0; i < blocksToAdd.Count; i++)
                        {
                            sb.Append(blocksToAdd[i]);
                            if (i != blocksToAdd.Count - 1)
                                sb.AppendLine("    ,");
                        }

                        // Write lines after last block
                        for (int i = indexEndLastBlock + 1; i < fileLinesContent.Count; i++)
                        {
                            sb.AppendLine(fileLinesContent[i]);
                        }
                    }
                    else
                    {
                        // Writes lines until the end
                        for (int i = indexEndProperty + 1; i < fileLinesContent.Count; i++)
                        {
                            sb.AppendLine(fileLinesContent[i]);
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
                            sb.AppendLine(fileLinesContent[i]);
                        }

                        // Write blocks to add
                        for (int i = 0; i < blocksToAdd.Count; i++)
                        {
                            sb.Append(blocksToAdd[i]);
                            if (i != blocksToAdd.Count - 1)
                                sb.AppendLine("    ,");
                        }

                        // Write lines after last block
                        for (int i = indexEndLastBlock + 1; i < fileLinesContent.Count; i++)
                        {
                            sb.AppendLine(fileLinesContent[i]);
                        }
                    }
                    else
                    {
                        // Error
                        consoleWriter.AddMessageLine($"Update File '{fileName}', index not found (Property: begin={indexBeginProperty}, end={indexEndProperty}; Block: begin={indexBeginFirstBlock}, end={indexEndLastBlock})", "Orange");

                        // Write lines 
                        for (int i = 0; i < fileLinesContent.Count; i++)
                        {
                            sb.AppendLine(fileLinesContent[i]);
                        }
                    }
                }

                // Create file with all lines
                File.WriteAllText(fileName, sb.ToString());
            }

            // Replace on file content
            string fileContent = File.ReadAllText(fileName);
            fileContent = ConvertOldCrudNameToNewCrudName(fileContent);

            // Generate new file
            File.WriteAllText(newFileName, fileContent);
        }

        private List<string> GeneratePropertiesToAdd(CRUDAngularData angularFile, KeyValuePair<string, List<string>> crudDtoProperty)
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

        private List<string> GenerateBlocksToAdd(CRUDAngularData angularFile, KeyValuePair<string, List<string>> crudDtoProperty)
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

        public string ConvertDotNetToAngularType(string dotnetType)
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
            extractBlock.BlockLines.ForEach(line => sb.AppendLine(line));

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

        #region Rename CRUD
        public void InitRenameValues(string newValueSingular, string newValuePlurial, string oldValueSingular = "Plane", string oldValuePlurial = "Planes")
        {
            this.OldCrudNamePascalSingular = oldValueSingular;
            this.OldCrudNamePascalPlural = oldValuePlurial;
            this.OldCrudNameCamelSingular = CommonTools.ConvertToCamelCase(OldCrudNamePascalSingular);
            this.OldCrudNameCamelPlural = CommonTools.ConvertToCamelCase(OldCrudNamePascalPlural);

            this.NewCrudNamePascalSingular = newValueSingular;
            this.NewCrudNamePascalPlural = newValuePlurial;
            this.NewCrudNameCamelSingular = CommonTools.ConvertToCamelCase(NewCrudNamePascalSingular);
            this.NewCrudNameCamelPlural = CommonTools.ConvertToCamelCase(NewCrudNamePascalPlural);
            this.NewCrudNameKebabSingular = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalSingular);
            this.NewCrudNameKebabPlural = CommonTools.ConvertPascalToKebabCase(NewCrudNamePascalPlural);
        }

        private string ConvertOldCrudNameToNewCrudName(string value)
        {
            if (value.Contains(OldCrudNamePascalPlural))
            {
                value = value.Replace(OldCrudNamePascalPlural, NewCrudNamePascalPlural);
            }
            if (value.Contains(OldCrudNameCamelPlural))
            {
                value = value.Replace(OldCrudNameCamelPlural, NewCrudNameKebabPlural);
            }
            if (value.Contains(OldCrudNamePascalSingular))
            {
                value = value.Replace(OldCrudNamePascalSingular, NewCrudNamePascalSingular);
            }
            if (value.Contains(OldCrudNameCamelSingular))
            {
                value = value.Replace(OldCrudNameCamelSingular, NewCrudNameKebabSingular);
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
        #endregion
    }
}
