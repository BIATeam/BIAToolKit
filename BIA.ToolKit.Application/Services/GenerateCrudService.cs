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

        public GenerateCrudService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void GenerateCrudFiles(Project currentProject, EntityInfo dtoEntity)
        {
            try
            {
                string dotnetDir = Path.Combine(currentProject.Folder, GENERATED_FOLDER, Constants.FolderNetCore);
                string angularDir = Path.Combine(currentProject.Folder, GENERATED_FOLDER, Constants.FolderAngular);

                PrepareFolders(dotnetDir, angularDir);

                // 
                GenerateEntityFile(dotnetDir, currentProject, dtoEntity);
                GenerateEntityMapperFile(dotnetDir, currentProject, dtoEntity);

                //
                GenerateIApplicationFile(dotnetDir, currentProject, dtoEntity);
                GenerateApplicationFile(dotnetDir, currentProject, dtoEntity);

                //
                GenerateControllerFile(dotnetDir, currentProject, dtoEntity);

            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"An error has occurred in CRUD generation process: {ex.Message}", "Red");
            }
        }

        private void PrepareFolders(string dotnetDir, string angularDir)
        {
            // Clean destination directory if already exist
            if (Directory.Exists(dotnetDir))
            {
                Directory.Delete(dotnetDir, true);
            }
            Directory.CreateDirectory(dotnetDir);

            // Clean destination directory if already exist
            if (Directory.Exists(angularDir))
            {
                Directory.Delete(angularDir, true);
            }
            Directory.CreateDirectory(angularDir);
        }

        private void GenerateEntityFile(string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            string entityName = dtoEntity.NamespaceLastPart;

            StringBuilder sb = new();
            // Generate file header
            sb.AppendLine($"// <copyright file=\"{entityName}.cs\" company=\"{currentProject.CompanyName}\">");
            sb.AppendLine($"//     Copyright (c) {currentProject.CompanyName}. All rights reserved.");
            sb.AppendLine($"// </copyright>");
            sb.AppendLine();

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
            CommonMethods.CreateFile(sb, Path.Combine(destDir, $"{entityName}.cs"));
        }

        private void GenerateEntityMapperFile(string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            // TODO NMA
            string entityName = dtoEntity.NamespaceLastPart;

            StringBuilder sb = new();

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, $"{entityName}Mapper.cs"));
        }

        private void GenerateIApplicationFile(string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            // TODO NMA
            string entityName = dtoEntity.NamespaceLastPart;

            StringBuilder sb = new();

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, $"I{entityName}AppService.cs"));
        }

        private void GenerateApplicationFile(string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            // TODO NMA
            string entityName = dtoEntity.NamespaceLastPart;

            StringBuilder sb = new();

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, $"{entityName}AppService.cs"));
        }

        private void GenerateControllerFile(string destDir, Project currentProject, EntityInfo dtoEntity)
        {
            // TODO NMA
            string entityName = dtoEntity.NamespaceLastPart;

            StringBuilder sb = new();

            // Create generated file on disk            
            CommonMethods.CreateFile(sb, Path.Combine(destDir, $"{entityName}sController.cs"));
        }
    }
}
