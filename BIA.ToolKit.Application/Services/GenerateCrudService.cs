namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
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

    public class GenerateCrudService
    {
        private const string GENERATED_FOLDER = "GeneratedCRUD";
        private readonly IConsoleWriter consoleWriter;

        private string applicationFolder;
        private string domainFolder;
        //private string domainDtoFolder;
        private string controllerFolder;


        public GenerateCrudService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void GenerateCrudFiles(Project currentProject, EntityInfo dtoEntity, List<ClassDefinition> classDefinitionFromZip)
        {
#if DEBUG
            consoleWriter.AddMessageLine($"*** Generate CRUD files on '{Path.Combine(currentProject.Folder, GENERATED_FOLDER)}' ***", "Green");
#endif

            try
            {
                string generatedFolder = Path.Combine(currentProject.Folder, currentProject.Name, GENERATED_FOLDER);
                string dotnetDir = Path.Combine(generatedFolder, Constants.FolderDotNet);
                string angularDir = Path.Combine(generatedFolder, Constants.FolderAngular);
                string entityName = GetEntityNameFromDto(dtoEntity.Name);

                InitPath(entityName, currentProject.CompanyName, currentProject.Name);
                PrepareFolders(dotnetDir, angularDir);

                // Entity
                GenerateEntityFile(entityName, dotnetDir, currentProject, dtoEntity, classDefinitionFromZip.Where(x => x.FileType == FileType.Entity).First());
                //GenerateEntityMapperFile(dotnetDir, currentProject, dtoEntity);

                // Application
                GenerateIApplicationFile(entityName, dotnetDir, currentProject, dtoEntity, classDefinitionFromZip.Where(x => x.FileType == FileType.IAppService).First());
                GenerateApplicationFile(entityName, dotnetDir, currentProject, dtoEntity, classDefinitionFromZip.Where(x => x.FileType == FileType.AppService).First());

                // Controller
                GenerateControllerFile(entityName, dotnetDir, currentProject, dtoEntity, classDefinitionFromZip.Where(x => x.FileType == FileType.Controller).First());

                // IocContainer
                CreateIocContainerFile(dotnetDir, entityName);

                // Rights
                CreateRightsFile(dotnetDir, currentProject, dtoEntity);
                CreateBIANETConfigFile(dotnetDir, currentProject, dtoEntity);

                System.Diagnostics.Process.Start("explorer.exe", generatedFolder);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in CRUD generation process: {ex.Message}", "Red");
            }
        }

        private string GetEntityNameFromDto(string dtoFileName)
        {
            if (dtoFileName.ToLower().EndsWith("dto"))
            {
                return dtoFileName[..^3];   // name without 'dto' suffix
            }

            return dtoFileName;
        }

        private void InitPath(string entityName, string compagnyName, string projectName)
        {
            applicationFolder = $@"{compagnyName}.{projectName}.Application/{entityName}";
            domainFolder = $@"{compagnyName}.{projectName}.Domain/{entityName}Module/Aggregate";
            //domainDtoFolder = $@"{compagnyName}.{projectName}.Domain.Dto/{entityName}";
            controllerFolder = $@"{compagnyName}.{projectName}.Presentation.Api/Controllers/{entityName}";
        }

        private void PrepareFolders(string dotnetDir, string angularDir)
        {
            // Clean destination directory if already exist
            if (Directory.Exists(dotnetDir))
            {
                Directory.Delete(dotnetDir, true);
            }
            Directory.CreateDirectory(Path.Combine(dotnetDir, applicationFolder));
            Directory.CreateDirectory(Path.Combine(dotnetDir, domainFolder));
            //Directory.CreateDirectory(Path.Combine(dotnetDir, domainDtoFolder));
            Directory.CreateDirectory(Path.Combine(dotnetDir, controllerFolder));

            // Clean destination directory if already exist
            if (Directory.Exists(angularDir))
            {
                Directory.Delete(angularDir, true);
            }
            Directory.CreateDirectory(angularDir);
        }

        #region Entity
        private void GenerateEntityFile(string entityName, string destDir, Project currentProject, EntityInfo dtoEntity, ClassDefinition classDefinition)
        {
            string fileName = $"{entityName}.cs";
            StringBuilder sb = new();

            // Generate file header
            GenerateFileHeader(sb, fileName, currentProject.CompanyName);

            // Generate namespace + using
            GenerateNamespaceUsing(sb, entityName, dtoEntity, classDefinition);

            // Generate class declaration
            GenerateClassDeclaration(sb, entityName, classDefinition, entityName, $"The {entityName} entity");

            // Generate primary key
            if (dtoEntity.PrimaryKey.ToLower() == "int")
            {
                var prop = classDefinition.PropertyList.Where(x => x.Identifier.Text.ToLower() == "id").First();
                if (prop != null)
                {
                    GenerateProperty(sb, prop.Type.ToString(), prop.Identifier.ToString(), string.Join(' ', prop.Modifiers.ToList()));
                }
            }

            // Generate properties
            foreach (PropertyInfo p in dtoEntity.Properties)
            {
                if (p.Type == "int" && p.Name.ToLower() == "siteid")    // int SiteId
                {
                    string newName = p.Name[..^2]; // Name without "Id" suffix
                    GenerateProperty(sb, $"{newName}", $"{newName}", "public virtual");
                    GenerateProperty(sb, p.Type, p.Name);
                }
                else if (p.Type == "OptionDto")                         // Type OptionDto  
                {
                    GenerateProperty(sb, p.Type, p.Name);
                    sb.AppendLine($"        //****************************************************************************************");
                    sb.AppendLine($"        //****************** Rework OptionDto by 2 lines: type + id ******************************");
                    sb.AppendLine($"        // public int {p.Name}Id {{ get; set; }}");
                    sb.AppendLine($"        // public virtual {p.Name} {p.Name} {{ get; set; }}");
                    sb.AppendLine($"        //****************************************************************************************");
                    sb.AppendLine($"        //****************************************************************************************");
                }
                else if (p.Type.EndsWith("Dto"))                        // Type XXXDto
                {
                    string newType = p.Type[..^3]; // Type without "Dto" suffix
                    GenerateProperty(sb, $"{newType}", p.Name, "public virtual");
                    GenerateProperty(sb, "int", $"{p.Name}Id");
                }
                else if (p.Type.EndsWith("Dto>"))                       // Collection of XXXDto
                {
                    string newType = p.Type.Replace("Dto", ""); // Type without "Dto" suffix
                    GenerateProperty(sb, $"{newType}", p.Name);
                }
                else                                                    // All others cases     
                {
                    GenerateProperty(sb, p.Type, p.Name);
                }
            }
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, domainFolder, fileName));
        }

        private void GenerateEntityMapperFile(string entityName, string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            string fileName = $"{entityName}Mapper.cs";
            StringBuilder sb = new();

            // Generate file header
            GenerateFileHeader(sb, fileName, currentProject.CompanyName);

            // TODO NMA

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, domainFolder, fileName));
        }
        #endregion

        #region Application
        private void GenerateIApplicationFile(string entityName, string destDir, Project currentProject, EntityInfo dtoEntity, ClassDefinition classDefinition)
        {
            string className = $"I{entityName}AppService";
            string fileName = $"{className}.cs";
            StringBuilder sb = new();

            // Generate file header
            GenerateFileHeader(sb, fileName, currentProject.CompanyName);

            // Generate namespace + using
            GenerateNamespaceUsing(sb, entityName, dtoEntity, classDefinition);

            // Generate interface declaration
            GenerateClassDeclaration(sb, entityName, classDefinition, className, $"The interface defining the application service for {entityName}");

            // TODO NMA

            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, applicationFolder, fileName));
        }

        private void GenerateApplicationFile(string entityName, string destDir, Project currentProject, EntityInfo dtoEntity, ClassDefinition classDefinition)
        {
            string className = $"{entityName}AppService";
            string fileName = $"{className}.cs";
            StringBuilder sb = new();

            // Generate file header
            GenerateFileHeader(sb, fileName, currentProject.CompanyName);

            // Generate namespace + using
            GenerateNamespaceUsing(sb, entityName, dtoEntity, classDefinition);

            // Generate class declaration
            GenerateClassDeclaration(sb, entityName, classDefinition, className, $"The application service used for {entityName}");

            // Generate class fields
            foreach (FieldDeclarationSyntax fd in classDefinition.FieldList)
            {
                GenerateField(sb, fd.Declaration.ToString(), entityName, classDefinition.EntityName, string.Join(' ', fd.Modifiers.ToList()));
            }

            // Generate class constructors
            foreach (ConstructorDeclarationSyntax cd in classDefinition.ConstructorList)
            {
                GenerateConstructor(sb, entityName, classDefinition.EntityName, cd.Identifier.ToString(), string.Join(' ', cd.ParameterList),
                    cd.Initializer?.ToFullString(), cd.Body?.ToFullString(), string.Join(' ', cd.Modifiers.ToList()));
            }

            // TODO NMA

            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, applicationFolder, fileName));
        }
        #endregion

        #region Controller
        private void GenerateControllerFile(string entityName, string destDir, Project currentProject, EntityInfo dtoEntity, ClassDefinition classDefinition)
        {
            string className = $"{entityName}sController";
            string fileName = $"{className}.cs";
            StringBuilder sb = new();

            // Generate file header
            GenerateFileHeader(sb, fileName, currentProject.CompanyName);

            // Generate namespace + using
            GenerateNamespaceUsing(sb, entityName, dtoEntity, classDefinition);

            // Generate class declaration
            GenerateClassDeclaration(sb, entityName, classDefinition, className, $"The API controller used to manage {entityName}");

            // Generate class fields
            foreach (FieldDeclarationSyntax fd in classDefinition.FieldList)
            {
                GenerateField(sb, fd.Declaration.ToString(), entityName, classDefinition.EntityName, string.Join(' ', fd.Modifiers.ToList()));
            }

            // Generate class constructors
            foreach (ConstructorDeclarationSyntax cd in classDefinition.ConstructorList)
            {
                // TODO NMA
                GenerateConstructor(sb, entityName, classDefinition.EntityName, cd.Identifier.ToString(), string.Join(' ', cd.ParameterList),
                    cd.Initializer?.ToFullString(), cd.Body?.ToString(), string.Join(' ', cd.Modifiers.ToList()));
            }

            foreach (MethodDeclarationSyntax md in classDefinition.MethodList)
            {
                GenerateMethod(sb, entityName, classDefinition.EntityName, md.AttributeLists.ToList(), md.ReturnType.ToString(),
                    md.Identifier.ToString(), string.Join(' ', md.ParameterList), md.Body?.ToString(), string.Join(' ', md.Modifiers.ToList()));
            }

            // TODO NMA

            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, controllerFolder, fileName));
        }
        #endregion

        #region IocContainer
        private void CreateIocContainerFile(string destDir, string entityName)
        {
            StringBuilder sb = new();
            sb.AppendLine("***** Add the following line(s) at the end of 'ConfigureApplicationContainer' method : ");
            sb.Append($"collection.AddTransient<I{entityName}AppService, {entityName}AppService>();");

            CommonMethods.CreateFile(sb, Path.Combine(destDir, "UPDATE__IocContainer.cs__.txt"));
        }
        #endregion

        #region Rights
        private void CreateRightsFile(string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            string entityName = dtoEntity.NamespaceLastPart;

            StringBuilder sb = new();
            sb.AppendLine($"***** Add the following line(s) at the end of '{currentProject.CompanyName}.{currentProject.Name}.Crosscutting.Common/Rights.cs' file:");

            sb.AppendLine($"/// <summary>");
            sb.AppendLine($"/// The {entityName} rights.");
            sb.AppendLine($"/// </summary>");
            sb.AppendLine($"public static class {entityName}");
            sb.AppendLine($"{{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to access to the list of {entityName}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string List = \"{CommonMethods.FirstLetterUppercase(entityName)}_List\";{Environment.NewLine}");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to read {entityName}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string Read = \"{CommonMethods.FirstLetterUppercase(entityName)}_Read\";{Environment.NewLine}");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to create {entityName}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string Create = \"{CommonMethods.FirstLetterUppercase(entityName)}_Create\";{Environment.NewLine}");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to update {entityName}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string Update = \"{CommonMethods.FirstLetterUppercase(entityName)}_Update\";{Environment.NewLine}");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to delete {entityName}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string Delete = \"{CommonMethods.FirstLetterUppercase(entityName)}_Delete\";{Environment.NewLine}");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to save {entityName}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string Save = \"{CommonMethods.FirstLetterUppercase(entityName)}_Save\";{Environment.NewLine}");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The right to access to the list of {entityName}s.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public const string ListAccess = \"{CommonMethods.FirstLetterUppercase(entityName)}_List_Access\";");
            sb.AppendLine($"}}");

            CommonMethods.CreateFile(sb, Path.Combine(destDir, "UPDATE__Rights.cs__.txt"));
        }

        private void CreateBIANETConfigFile(string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            string entityName = dtoEntity.NamespaceLastPart;

            StringBuilder sb = new();
            sb.AppendLine($"***** Add the following line(s) at the end of '{currentProject.CompanyName}.{currentProject.Name}.Presentation.Api/bianetconfig.txt' file:");

            sb.AppendLine($"// {entityName}");
            sb.AppendLine($"{{");
            sb.AppendLine($"\"Names\": [ \"{entityName}_Create\", \"{entityName}_Update\", \"{entityName}_Delete\", \"{entityName}_Save\", \"{entityName}_List_Access\" ],");
            sb.AppendLine($"\"Roles\": [ \"Admin\", \"Site_Admin\" ]");
            sb.AppendLine($"}},");
            sb.AppendLine($"{{");
            sb.AppendLine($"\"Names\": [ \"{entityName}_List\", \"{entityName}_Read\" ]");
            sb.AppendLine($"\"Roles\": [ \"Admin\", \"Site_Admin\", \"User\" ]");
            sb.AppendLine($"}}");

            CommonMethods.CreateFile(sb, Path.Combine(destDir, "UPDATE__bianetconfig.txt__.txt"));
        }
        #endregion

        #region Tools
        private void GenerateFileHeader(StringBuilder sb, string fileName, string companyName)
        {
            sb.AppendLine($"// <copyright file=\"{fileName}\" company=\"{companyName}\">");
            sb.AppendLine($"//     Copyright (c) {companyName}. All rights reserved.");
            sb.AppendLine($"// </copyright>");
        }

        private void GenerateNamespaceUsing(StringBuilder sb, string entityName, EntityInfo dtoEntity, ClassDefinition classDefinition)
        {
            // Generate namespace
            var @namespace = classDefinition.NamespaceSyntax.Name.ToString().
                Replace(classDefinition.CompagnyName, dtoEntity.CompagnyName).
                Replace(classDefinition.ProjectName, dtoEntity.ProjectName).
                Replace(classDefinition.EntityName, entityName);
            sb.AppendLine($"{classDefinition.NamespaceSyntax.NamespaceKeyword} {@namespace}");

            // Generate using
            sb.AppendLine($"{{");

            classDefinition.NamespaceSyntax.Usings.ToList().ForEach(@using =>
            {
                var usingFormated = @using.ToString().
                   Replace(classDefinition.CompagnyName, dtoEntity.CompagnyName).
                   Replace(classDefinition.ProjectName, dtoEntity.ProjectName).
                   Replace(classDefinition.EntityName, entityName);
                sb.AppendLine($"   {usingFormated}");
            });

            sb.AppendLine();
        }

        private void GenerateClassDeclaration(StringBuilder sb, string entityName, ClassDefinition classDefinition, string className, string comment)
        {
            string baselist = classDefinition.BaseList.ToString().
               Replace(classDefinition.EntityName, entityName);

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
            sb.AppendLine($"    {classDefinition.VisibilityList} {type} {className} {baselist}");
            sb.AppendLine($"    {{");
        }

        private void GenerateProperty(StringBuilder sb, string type, string property, string modifier = "public")
        {
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Gets or Sets {property}.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        {modifier} {type} {property} {{ get; set; }}{Environment.NewLine}");
        }

        private void GenerateField(StringBuilder sb, string fieldDef, string dtoEntityName, string classDefEntityName, string modifier = "public")
        {
            var tmp = fieldDef.Split(' ');
            string type = TransformValue(tmp[0], dtoEntityName, classDefEntityName);
            string field = TransformValue(tmp[1], dtoEntityName, classDefEntityName, true);

            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// The {field}.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        {modifier} {type} {field}{Environment.NewLine}");
        }

        private void GenerateConstructor(StringBuilder sb, string dtoEntityName, string classDefEntityName, string constructorName, string parameters, string baseList, string body, string modifier = "public")
        {
            constructorName = TransformValue(constructorName, dtoEntityName, classDefEntityName);
            parameters = TransformValue(parameters, dtoEntityName, classDefEntityName);
            baseList = TransformValue(baseList, dtoEntityName, classDefEntityName);
            body = TransformValue(body, dtoEntityName, classDefEntityName);

            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Initializes a new instance of the <see cref=\"{constructorName}\"/> class.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        {modifier} {constructorName}{parameters}");
            sb.AppendLine($"{baseList}{body}{Environment.NewLine}");
        }

        private void GenerateMethod(StringBuilder sb, string dtoEntityName, string classDefEntityName, List<AttributeListSyntax> attributeList, string type, string methodName, string parameters, string body, string modifier = "public")
        {
            parameters = TransformValue(parameters, dtoEntityName, classDefEntityName);
            body = TransformValue(body, dtoEntityName, classDefEntityName);

            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// XXXX");
            sb.AppendLine($"        /// </summary>");
            attributeList.ForEach(attr =>
            {
                string attrFormated = TransformValue(attr.ToString(), dtoEntityName, classDefEntityName);
                sb.AppendLine($"        {attrFormated}");
            });
            sb.AppendLine($"        {modifier} {type} {methodName}{parameters}");
            sb.AppendLine($"        {body}{Environment.NewLine}");
        }

        private string TransformValue(string value, string dtoEntityName, string classDefEntityName, bool format = false)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            string transform = value.Replace(classDefEntityName, dtoEntityName, StringComparison.InvariantCultureIgnoreCase);
            if (format) // Set first character to lower
                return Char.ToLowerInvariant(transform[0]) + transform.Substring(1);

            return transform;
        }
        #endregion
    }
}
