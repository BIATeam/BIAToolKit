namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using System;
    using System.IO;
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

        public void GenerateCrudFiles(Project currentProject, EntityInfo dtoEntity)
        {

#if DEBUG
            consoleWriter.AddMessageLine($"*** Generate CRUD files on '{Path.Combine(currentProject.Folder, GENERATED_FOLDER)}' ***", "Green");
#endif

            try
            {
                string dotnetDir = Path.Combine(currentProject.Folder, GENERATED_FOLDER, Constants.FolderNetCore);
                string angularDir = Path.Combine(currentProject.Folder, GENERATED_FOLDER, Constants.FolderAngular);

                InitPath(dtoEntity.NamespaceLastPart, currentProject.CompanyName, currentProject.Name);
                PrepareFolders(dotnetDir, angularDir);

                // Entity
                GenerateEntityFile(dotnetDir, currentProject, dtoEntity);
                GenerateEntityMapperFile(dotnetDir, currentProject, dtoEntity);

                // Application
                GenerateIApplicationFile(dotnetDir, currentProject, dtoEntity);
                GenerateApplicationFile(dotnetDir, currentProject, dtoEntity);

                // Controller
                GenerateControllerFile(dotnetDir, currentProject, dtoEntity);

                // IocContainer
                CreateIocContainerFile(dotnetDir, dtoEntity.NamespaceLastPart);

                // Rights
                CreateRightsFile(dotnetDir, currentProject, dtoEntity);
                CreateBIANETConfigFile(dotnetDir, currentProject, dtoEntity);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in CRUD generation process: {ex.Message}", "Red");
            }
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

        private string GenerateFileHeader(string fileName, string companyName)
        {
            StringBuilder sb = new();
            sb.AppendLine($"// <copyright file=\"{fileName}\" company=\"{companyName}\">");
            sb.AppendLine($"//     Copyright (c) {companyName}. All rights reserved.");
            sb.AppendLine($"// </copyright>");
            return sb.ToString();
        }

        #region Entity
        private void GenerateEntityFile(string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            string entityName = dtoEntity.NamespaceLastPart;
            string fileName = $"{entityName}.cs";
            StringBuilder sb = new();

            // Generate file header
            sb.AppendLine(GenerateFileHeader(fileName, currentProject.CompanyName));

            // Generate namespace + using
            sb.AppendLine($"namespace {currentProject.CompanyName}.{currentProject.Name}.Domain.{entityName}Module.Aggregate");
            sb.AppendLine($"{{");
            sb.AppendLine($"    using System;");
            sb.AppendLine($"    using System.Collections.Generic;");
            sb.AppendLine($"    using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine($"    using BIA.Net.Core.Domain;");
            sb.AppendLine($"    using {currentProject.CompanyName}.{currentProject.Name}.Domain.SiteModule.Aggregate;");
            sb.AppendLine();

            // Generate class declaration
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// The {entityName} entity.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {entityName} : VersionedTable, IEntity<{dtoEntity.PrimaryKey}>");
            sb.AppendLine($"    {{");

            // Generate primary key
            if (dtoEntity.PrimaryKey.ToLower() == "int")
            {
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Gets or Sets the id.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public {dtoEntity.PrimaryKey} Id {{ get; set; }}{Environment.NewLine}");
            }

            // Generate properties
            foreach (PropertyInfo p in dtoEntity.Properties)
            {
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Gets or Sets {p.Name}.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public {p.Type} {p.Name} {{ get; set; }}{Environment.NewLine}");
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
