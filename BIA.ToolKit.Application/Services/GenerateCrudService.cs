namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
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

                // Copy Dto file

                // Entity
                GenerateEntityFile(entityName, dotnetDir, currentProject, dtoEntity, classDefinitionFromZip.Where(x => x.FileType == FileType.Entity).First());
                //GenerateEntityMapperFile(dotnetDir, currentProject, dtoEntity);

                // Application
                //GenerateIApplicationFile(dotnetDir, currentProject, dtoEntity);
                //GenerateApplicationFile(dotnetDir, currentProject, dtoEntity);

                // Controller
                //GenerateControllerFile(dotnetDir, currentProject, dtoEntity);

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
            Directory.CreateDirectory(Path.Combine(dotnetDir, domainDtoFolder));
            Directory.CreateDirectory(Path.Combine(dotnetDir, controllerFolder));

            // Clean destination directory if already exist
            if (Directory.Exists(angularDir))
            {
                Directory.Delete(angularDir, true);
            }
            Directory.CreateDirectory(angularDir);
        }

        private string GenerateFileHeader(string fileName, string companyName)
        {
            StringBuilder sb = new();
            sb.AppendLine($"// <copyright file=\"{fileName}\" company=\"{companyName}\">");
            sb.AppendLine($"//     Copyright (c) {companyName}. All rights reserved.");
            sb.AppendLine($"// </copyright>");
            return sb.ToString();
        }

        #region Entity
        private void GenerateEntityFile(string entityName, string destDir, Project currentProject, EntityInfo dtoEntity, ClassDefinition entityClass)
        {
            string fileName = $"{entityName}.cs";
            StringBuilder sb = new();

            // Generate file header
            sb.AppendLine(GenerateFileHeader(fileName, currentProject.CompanyName));

            // Generate namespace + using   
            sb.AppendLine($"{entityClass.NamespaceSyntax.Name}");

            sb.AppendLine($"{{");
            for (int i = 0; i < entityClass.NamespaceSyntax.Usings.Count; i++)
            {
                sb.AppendLine($"   {entityClass.NamespaceSyntax.Usings[i]}");
            }
            sb.AppendLine();

            // Generate class declaration
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The {entityName} entity.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    {entityClass.VisibilityList} class {entityName}{entityClass.BaseList}");
            sb.AppendLine($"    {{");

            // Generate primary key
            if (dtoEntity.PrimaryKey.ToLower() == "int")
            {
                var prop = entityClass.PropertyList.Where(x => x.Identifier.Text.ToLower() == "id").First();
                if (prop != null)
                {
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Gets or Sets the id.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        {prop}{Environment.NewLine}");
                }
            }

            // Generate properties
            foreach (PropertyInfo p in dtoEntity.Properties)
            {
                if (p.Type == "int" && p.Name.ToLower() == "siteid")    // int SiteId
                {
                    string tmp = p.Name[..^2];
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Gets or Sets {tmp}.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public virtual {tmp} {tmp} {{ get; set; }}{Environment.NewLine}");
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Gets or Sets {p.Name}.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public {p.Type} {p.Name} {{ get; set; }}{Environment.NewLine}");
                }
                else if (p.Type == "OptionDto")    // Type OptionDto  
                {
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Gets or Sets {p.Name}.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        //****************************************************************************************");
                    sb.AppendLine($"        //****************** Rework OptionDto by 2 lines: type + id ******************************");
                    sb.AppendLine($"        // public int {p.Name}Id {{ get; set; }}");
                    sb.AppendLine($"        // public virtual {p.Name} {p.Name} {{ get; set; }}");
                    sb.AppendLine($"        //****************************************************************************************");
                    sb.AppendLine($"        public {p.Type} {p.Name} {{ get; set; }}{Environment.NewLine}");
                }
                else if (p.Type.EndsWith("Dto"))    // Type XXXDto
                {
                    string tmp = p.Type[..^3];
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Gets or Sets {p.Name}.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public virtual {tmp} {p.Name} {{ get; set; }}{Environment.NewLine}");
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Gets or Sets {p.Name}.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public int {p.Name}Id {{ get; set; }}{Environment.NewLine}");
                }
                else if (p.Type.EndsWith("Dto>"))   // Collection of XXXDto
                {
                    string tmp = p.Type.Replace("Dto", "");
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Gets or Sets {p.Name}.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public {tmp} {p.Name} {{ get; set; }}{Environment.NewLine}");
                }
                else                                // All others cases     
                {
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Gets or Sets {p.Name}.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public {p.Type} {p.Name} {{ get; set; }}{Environment.NewLine}");
                }
            }
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, domainFolder, fileName));
        }

        private void GenerateEntityMapperFile(string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            string entityName = dtoEntity.NamespaceLastPart;
            string fileName = $"{entityName}Mapper.cs";
            StringBuilder sb = new();

            // Generate file header
            sb.AppendLine(GenerateFileHeader(fileName, currentProject.CompanyName));

            // TODO NMA

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, domainFolder, fileName));
        }
        #endregion

        #region Application
        private void GenerateIApplicationFile(string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            string entityName = dtoEntity.NamespaceLastPart;
            string fileName = $"I{entityName}AppService.cs";
            StringBuilder sb = new();

            // Generate file header
            sb.AppendLine(GenerateFileHeader(fileName, currentProject.CompanyName));

            // Generate namespace + using
            sb.AppendLine($"namespace {currentProject.CompanyName}.{currentProject.Name}.Application.{entityName}");
            sb.AppendLine($"{{");
            sb.AppendLine($"    using System.Collections.Generic;");
            sb.AppendLine($"    using System.Threading.Tasks;");
            sb.AppendLine($"    using BIA.Net.Core.Application;");
            sb.AppendLine($"    using BIA.Net.Core.Domain.Dto.Base;");
            sb.AppendLine($"    using Safran.PAS.Domain.{entityName};");
            sb.AppendLine($"    using Safran.PAS.Domain.Dto.{entityName};");

            // Generate class declaration
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The interface defining the application service for {entityName}.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public interface I{entityName}AppService : ICrudAppServiceBase<{entityName}Dto, {entityName}, {dtoEntity.PrimaryKey}, PagingFilterFormatDto>");
            sb.AppendLine($"    {{");

            // TODO NMA

            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, applicationFolder, fileName));
        }

        private void GenerateApplicationFile(string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            string entityName = dtoEntity.NamespaceLastPart;
            string fileName = $"{entityName}AppService.cs";
            StringBuilder sb = new();

            // Generate file header
            sb.AppendLine(GenerateFileHeader(fileName, currentProject.CompanyName));

            // Generate namespace + using
            sb.AppendLine($"namespace {currentProject.CompanyName}.{currentProject.Name}.Application.{entityName}");
            sb.AppendLine($"{{");
            sb.AppendLine($"    using System.Collections.Generic;");
            sb.AppendLine($"    using System.Security.Principal;");
            sb.AppendLine($"    using System.Threading.Tasks;");
            sb.AppendLine($"    using BIA.Net.Core.Domain.Dto.Base;");
            sb.AppendLine($"    using BIA.Net.Core.Domain.Dto.User;");
            sb.AppendLine($"    using BIA.Net.Core.Domain.RepoContract;");
            sb.AppendLine($"    using BIA.Net.Core.Domain.Specification;");
            sb.AppendLine($"    using Safran.PAS.Domain.Dto.{entityName};");
            sb.AppendLine($"    using Safran.PAS.Domain.{entityName};");
            sb.AppendLine($"    using BIA.Net.Core.Application;");
            sb.AppendLine($"    using BIA.Net.Core.Application.Authentication;");

            // Generate class declaration
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The application service used for {entityName}.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {entityName}AppService : CrudAppServiceBase<{entityName}Dto, {entityName}, {dtoEntity.PrimaryKey}, PagingFilterFormatDto, {entityName}Mapper>, I{entityName}AppService");
            sb.AppendLine($"    {{");

            // TODO NMA

            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, applicationFolder, fileName));
        }
        #endregion

        #region Controller
        private void GenerateControllerFile(string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            string entityName = dtoEntity.NamespaceLastPart;
            string fileName = $"{entityName}sController.cs";
            StringBuilder sb = new();

            // Generate file header
            sb.AppendLine(GenerateFileHeader(fileName, currentProject.CompanyName));

            // TODO NMA

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
    }
}
