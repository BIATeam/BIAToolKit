﻿// <copyright file="Constants.cs" company="BIA">
// Copyright (c) BIA. All rights reserved.
// </copyright>

namespace BIA.ToolKit.Common
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Application contants
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The BIA Template repositoty URL
        /// </summary>
        public const string BIATemplateRepoUrl = "https://github.com/BIATeam/BIATemplate.git";

        /// <summary>
        /// The BIA Template realese URL
        /// </summary>
        public const string BIATemplateReleaseUrl = "https://github.com/BIATeam/BIATemplate/archive/";

        /// <summary>
        /// The start text on the original file.
        /// </summary>
        public const string TempName = "GENERATE_FILES_";

        /// <summary>
        /// Prcess Folder
        /// </summary>
        public const string NameFolderProcess = "Process";

        /// <summary>
        /// Folder name for Angular files
        /// </summary>
        public const string FolderAngular = "Angular";

        /// <summary>
        /// Folder name for DotNet files
        /// </summary>
        public const string FolderDotNet = "DotNet";

        /// <summary>
        /// Folder name for Net Core files
        /// </summary>
        public const string FolderNetCore = "NetCore";

        /// <summary>
        /// Folder name for "doc" folder on DotNet and Angular parent folder.
        /// </summary>
        public const string FolderBia = ".bia";

        /// <summary>
        /// Folder name for CRUD generated files.
        /// </summary>
        public const string FolderCrudGeneration = "GeneratedCRUD";

        /// <summary>
        /// Folder name for CRUD generated files.
        /// </summary>
        public const string FolderCrudGenerationTmp = "BiaToolKit_CRUDGenerator";

        /// <summary>
        /// File name suffix for "partial" file.
        /// </summary>
        public const string PartialFileSuffix = ".partial";

        public const char PropertySeparator = ':';

        private static readonly List<string> primitiveTypes = new List<string>
        {
            "char",
            "decimal",
            "double",
            "float",
            "int",
            "uint",
            "long",
            "ulong",
            "short",
            "ushort",
            "string",
        };
        public static IEnumerable<string> PrimitiveTypes => primitiveTypes.OrderBy(x => x);

        /// <summary>
        /// File Extensions.
        /// </summary>
        public static class FileExtensions
        {
            /// <summary>
            /// The solution extension.
            /// </summary>
            public const string DotNetSolution = ".sln";

            /// <summary>
            /// The project extension
            /// </summary>
            public const string DotNetProject = ".csproj";

            /// <summary>
            /// The dot net class.
            /// </summary>
            public const string DotNetClass = ".cs";
        }

        /// <summary>
        /// Bia Project Name.
        /// </summary>
        public static class BiaProjectName
        {
            public const string Test = "Test";
        }

        public static class BiaFeatureTag
        {
            public const string ItemGroupTag = "Bia_ItemGroup_";
        }

        public static class FeaturePathAdaptation
        {
            public const string ParentRelativePath = "{ParentRelativePath}";
            public const string ParentRelativePathLinux = "{ParentRelativePathLinux}";
            public const string NewCrudNamePascalSingular = "{NewCrudNamePascalSingular}";
            public const string ParentDomainName = "{ParentDomainName}";
            public const string ParentNameKebabPlural = "{ParentNameKebabPlural}";
            public const string ParentNameKebabSingular = "{ParentNameKebabSingular}";
            public const string DomainName = "{DomainName}";
        }

        public static class BiaClassName
        {
            public const string OptionDto = "OptionDto";
            public const string CollectionOptionDto = $"ICollection<{OptionDto}>";
        }
    }
}
