using BIA.ToolKit.Application.Helper;
using BIA.ToolKit.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BIA.ToolKit.Application.Services
{
    public class GenerateFilesService
    {
        private IConsoleWriter consoleWriter;
        List<string> _propertiesList = new List<string>();
        private string _projectName = string.Empty;
        DirectoryInfo _resultFolder = null;

        public GenerateFilesService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void GenerateFiles(string fileName, string folderName, ref string resultFolder)
        {
            try
            {
                FileInfo originalFile = new FileInfo(fileName);
                string fileTempName = Constants.TempName + originalFile.Name;

                consoleWriter.AddMessageLine("Checking folders.", "Pink");

                DirectoryInfo mainFolder = new DirectoryInfo(folderName);
                CommonMethods.CheckFoler(mainFolder);

                _resultFolder = new DirectoryInfo(Path.Combine(mainFolder.FullName, Constants.NameFolderProcess));
                CommonMethods.CheckFoler(_resultFolder);
                CommonMethods.CleanFolder(_resultFolder);

                DirectoryInfo resultNetCoreFolder = new DirectoryInfo(Path.Combine(_resultFolder.FullName, Constants.FolderNetCore));
                DirectoryInfo resultAngularFolder = new DirectoryInfo(Path.Combine(_resultFolder.FullName, Constants.FolderAngular));
                CommonMethods.CheckFoler(resultNetCoreFolder);
                CommonMethods.CheckFoler(resultAngularFolder);

                consoleWriter.AddMessageLine("Creating a copy from original file.", "Pink");
                FileInfo tempFile = new FileInfo(Path.Combine(_resultFolder.FullName, fileTempName));
                File.Copy(originalFile.FullName, tempFile.FullName);

                consoleWriter.AddMessageLine("Begin Process Create all files...", "Pink");
                CreateFiles(tempFile);
                consoleWriter.AddMessageLine("Remove copy of original file.", "Pink");
                File.Delete(tempFile.FullName);
                consoleWriter.AddMessageLine("End Process Create all files...", "Pink");

                consoleWriter.AddMessageLine("Opening result folder", "Pink");
                resultFolder = _resultFolder.FullName;


            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine("An error has occurred in the process. " + ex.Message, "Pink");

            }
        }

        //private void OpenProcessFolder()
        //{
        //    ProcessStartInfo startInfo = new ProcessStartInfo(_resultFolder.FullName);
        //    {
        //        UseShellExecute = true
        //    };

        //    Process.Start(startInfo);
        //}

        public void CreateFiles(FileInfo tempFile)
        {
            string className = tempFile.Name.Remove(tempFile.Name.Length - 3).Replace(Constants.TempName, "");
            _propertiesList = CommonMethods.GetProperties(tempFile, ref _projectName);
            consoleWriter.AddMessageLine(" * NetCore files ", "Pink");
            consoleWriter.AddMessageLine("    => Create Dto", "Pink");
            CreateDtoFile(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create List Mapper", "Pink");
            CreateListMapperFile(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create Interface", "Pink");
            CreateInterfaceFile(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create App Service", "Pink");
            CreateAppServiceFile(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create Controller", "Pink");
            CreateControllerFile(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create Mapper", "Pink");
            CreateMapperFile(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create List Item Dto ", "Pink");
            CreateListItemFile(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create line for IocContainer", "Pink");
            CreateIocContainerFile(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create lines for BIA Net Config file", "Pink");
            CreateBIANETConfigFile(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create lines for Rights file", "Pink");
            CreateRightsFile(className, tempFile.Name);

            consoleWriter.AddMessageLine(" * Angular files ", "Pink");
            consoleWriter.AddMessageLine("    => Create lines for Index Component", "Pink");
            CreateAngular_Index_ComponentFile(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create lines for Model", "Pink");
            Create_Angular_Model_File(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create lines for I18 Translations", "Pink");
            Create_Angular_TranslateI18n_File(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create lines for TableComponent InitForm", "Pink");
            CreateAngular_TableComponent_InitForm_File(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create lines for FormComponent InitFor", "Pink");
            CreateAngular_FormComponent_InitForm_File(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create lines for permission file", "Pink");
            CreateAngularPermissionFile(className, tempFile.Name);
            consoleWriter.AddMessageLine("    => Create lines for Form Component HTML ", "Pink");
            CreateAngularFormComponentHtmlFile(className, tempFile.Name);

            consoleWriter.AddMessageLine("Generate files finished.", "Green");
        }


        #region DTO_FILE
        private void CreateDtoFile(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderNetCore, $"{className}Dto.cs");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"namespace Safran.{_projectName}.Domain.Dto.{className}"));
            d.Append(CommonMethods.AddNewLine($"{{"));
            d.Append(CommonMethods.AddNewLine($"    using System;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Collections.Generic;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.Dto.Base;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.Dto.Option;"));
            d.Append(CommonMethods.AddNewLine($"    /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"    /// The {className}Dto entity."));
            d.Append(CommonMethods.AddNewLine($"    /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"    public class {className}Dto : BaseDto"));
            d.Append(CommonMethods.AddNewLine($"    {{"));

            foreach (string p in _propertiesList)
            {
                if (CommonMethods.GetValueFromList(p, 1).ToUpper() != "ID")
                {
                    d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
                    d.Append(CommonMethods.AddNewLine($"        /// Get or Set {CommonMethods.GetValueFromList(p, 1)}."));
                    d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
                    d.Append(CommonMethods.AddNewLine($"        public {CommonMethods.GetValueFromList(p, 0)} {CommonMethods.GetValueFromList(p, 1)} {{ get; set; }}{Environment.NewLine}"));
                }
            }
            d.Append(CommonMethods.AddNewLine($"    }}"));
            d.Append(CommonMethods.AddNewLine($"}}"));

            CommonMethods.CreateFile(d, fileDto);
        }

        #endregion

        #region ListItemDTO_FILE
        private void CreateListItemFile(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderNetCore, $"{className}ListItemDto.cs");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"namespace Safran.{_projectName}.Domain.Dto.{className}"));
            d.Append(CommonMethods.AddNewLine($"{{"));
            d.Append(CommonMethods.AddNewLine($"    using System;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.Dto.Base;"));
            d.Append(CommonMethods.AddNewLine($"    /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"    /// The DTO used to represent a {className}."));
            d.Append(CommonMethods.AddNewLine($"    /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"    public class {className}ListItemDto : BaseDto"));
            d.Append(CommonMethods.AddNewLine($"    {{"));
            foreach (string p in _propertiesList)
            {
                if (CommonMethods.GetValueFromList(p, 1).ToUpper() != "ID")
                {
                    d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
                    d.Append(CommonMethods.AddNewLine($"        /// Get or Set {CommonMethods.GetValueFromList(p, 1)}."));
                    d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
                    d.Append(CommonMethods.AddNewLine($"        public {CommonMethods.GetValueFromList(p, 0)} {CommonMethods.GetValueFromList(p, 1)} {{ get; set; }}{Environment.NewLine}"));
                }
            }
            d.Append(CommonMethods.AddNewLine($"    }}"));
            d.Append(CommonMethods.AddNewLine($"}}"));

            CommonMethods.CreateFile(d, fileDto);
        }

        #endregion

        #region Mapper_FILE
        private void CreateMapperFile(string className, string filename)
        {
            string classNameCamell = CommonMethods.ToCamelCase(className);

            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderNetCore, $"{className}Mapper.cs");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"namespace Safran.{_projectName}.Domain.{className}"));
            d.Append(CommonMethods.AddNewLine($"{{"));
            d.Append(CommonMethods.AddNewLine($"    using System;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Collections.Generic;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Linq;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Linq.Expressions;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.Dto.Base;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.Dto.Option;"));
            d.Append(CommonMethods.AddNewLine($"    using Safran.PAS.Domain.Dto.{className};"));
            d.Append(CommonMethods.AddNewLine($"    using Safran.PAS.Domain.Dto.Site;"));
            d.Append(CommonMethods.AddNewLine($"    /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"    /// The mapper used for {className}."));
            d.Append(CommonMethods.AddNewLine($"    /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"    public class {className}Mapper : BaseMapper<{className}Dto, {className}>"));
            d.Append(CommonMethods.AddNewLine($"    {{"));
            d.Append(CommonMethods.AddNewLine($"        /// <inheritdoc cref=\"BaseMapper{{ TDto,TEntity}}.ExpressionCollection\"/>"));
            d.Append(CommonMethods.AddNewLine($"        public override ExpressionCollection<{className}> ExpressionCollection"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            // It is not necessary to implement this function if you to not use the mapper for filtered list. In BIADemo it is use only for Calc SpreadSheet."));
            d.Append(CommonMethods.AddNewLine($"            get"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return new ExpressionCollection<{className}>"));
            d.Append(CommonMethods.AddNewLine($"                {{"));
            foreach (string p in _propertiesList)
            {
                d.Append(CommonMethods.AddNewLine($"                    {{ \"{CommonMethods.GetValueFromList(p, 1)}\", {classNameCamell} => {classNameCamell}.{CommonMethods.GetValueFromList(p, 1)} }},"));
            }

            d.Append(CommonMethods.AddNewLine($"                }};"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"        }}"));
            d.Append(CommonMethods.AddNewLine($"        /// <inheritdoc cref=\"BaseMapper{{ TDto,TEntity}}.DtoToEntity\"/>"));
            d.Append(CommonMethods.AddNewLine($"        public override void DtoToEntity({className}Dto dto, {className} entity)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            if (entity == null)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                entity = new {className}();"));
            d.Append(CommonMethods.AddNewLine($"            }}{Environment.NewLine}"));
            foreach (string p in _propertiesList)
            {
                d.Append(CommonMethods.AddNewLine($"            entity.{CommonMethods.GetValueFromList(p, 1)} = dto.{CommonMethods.GetValueFromList(p, 1)};"));
            }
            d.Append(CommonMethods.AddNewLine($"            //// Mapping relationship 1-* : Site"));
            d.Append(CommonMethods.AddNewLine($"            // if (dto.SiteId != 0)"));
            d.Append(CommonMethods.AddNewLine($"            // {{"));
            d.Append(CommonMethods.AddNewLine($"            //     entity.SiteId = dto.SiteId;"));
            d.Append(CommonMethods.AddNewLine($"            // }}"));
            d.Append(CommonMethods.AddNewLine($"        }}{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <inheritdoc cref=\"BaseMapper{{ TDto,TEntity}}.EntityToDto\"/>"));
            d.Append(CommonMethods.AddNewLine($"        public override Expression<Func<{className}, {className}Dto>> EntityToDto()"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            return entity => new {className}Dto"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            foreach (string p in _propertiesList)
            {
                d.Append(CommonMethods.AddNewLine($"                {CommonMethods.GetValueFromList(p, 1)} = entity.{CommonMethods.GetValueFromList(p, 1)},"));
            }
            d.Append(CommonMethods.AddNewLine($"                // Mapping relationship 1-* : Site"));
            d.Append(CommonMethods.AddNewLine($"                // SiteId = entity.SiteId,"));
            d.Append(CommonMethods.AddNewLine($"            }};"));
            d.Append(CommonMethods.AddNewLine($"        }}"));


            d.Append(CommonMethods.AddNewLine($"        /// <inheritdoc cref=\"BaseMapper{{ TDto,TEntity}}.DtoToRecord\"/>"));
            d.Append(CommonMethods.AddNewLine($"        public override Func<{className}Dto, object[]> DtoToRecord()"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            return x => new object[]"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            foreach (string p in _propertiesList)
            {
                d.Append(CommonMethods.AddNewLine($"                x.{CommonMethods.GetValueFromList(p, 1)},"));
            }

            d.Append(CommonMethods.AddNewLine($"            }};"));
            d.Append(CommonMethods.AddNewLine($"        }}"));



            d.Append(CommonMethods.AddNewLine($"    }}"));
            d.Append(CommonMethods.AddNewLine($"}}"));


            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion

        #region ListMapper_FILE
        private void CreateListMapperFile(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderNetCore, $"{className}ListMapper.cs");
            string classNameCamell = CommonMethods.ToCamelCase(className);
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"namespace Safran.{_projectName}.Domain.{className}"));
            d.Append(CommonMethods.AddNewLine($"{{"));
            d.Append(CommonMethods.AddNewLine($"    using System;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Linq;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Linq.Expressions;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain;"));
            d.Append(CommonMethods.AddNewLine($"    using Safran.{_projectName}.Domain.Dto.{className};{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"    /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"    /// The {className}ListMapper entity."));
            d.Append(CommonMethods.AddNewLine($"    /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"    public class {className}ListMapper : BaseMapper<{className}ListItemDto, {className}>"));
            d.Append(CommonMethods.AddNewLine($"    {{"));
            d.Append(CommonMethods.AddNewLine($"    /// <inheritdoc cref=\"BaseMapper{{ TDto,TEntity}}.ExpressionCollection\"/>"));
            d.Append(CommonMethods.AddNewLine($"        public override ExpressionCollection<{className}> ExpressionCollection"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            get"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return new ExpressionCollection<{className}>"));
            d.Append(CommonMethods.AddNewLine($"                {{"));
            foreach (string p in _propertiesList)
            {
                d.Append(CommonMethods.AddNewLine($"                    {{ \"{CommonMethods.GetValueFromList(p, 1)}\", {classNameCamell} => {classNameCamell}.{CommonMethods.GetValueFromList(p, 1)} }},"));
            }
            d.Append(CommonMethods.AddNewLine($"                }};"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"        }}{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <inheritdoc cref=\"BaseMapper{{ TDto,TEntity}}.EntityToDto\"/>"));
            d.Append(CommonMethods.AddNewLine($"        public override Expression<Func<{className}, {className}ListItemDto>> EntityToDto()"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            return entity => new {className}ListItemDto"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            foreach (string p in _propertiesList)
            {
                d.Append(CommonMethods.AddNewLine($"                {CommonMethods.GetValueFromList(p, 1)} = entity.{CommonMethods.GetValueFromList(p, 1)},"));
            }
            d.Append(CommonMethods.AddNewLine($"            }};"));
            d.Append(CommonMethods.AddNewLine($"        }}{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <inheritdoc cref=\"BaseMapper{{ TDto,TEntity}}.DtoToRecord\"/>"));
            d.Append(CommonMethods.AddNewLine($"        public override Func<{className}ListItemDto, object[]> DtoToRecord()"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            return x => new object[]"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            foreach (string p in _propertiesList)
            {
                d.Append(CommonMethods.AddNewLine($"                x.{CommonMethods.GetValueFromList(p, 1)},"));
            }


            d.Append(CommonMethods.AddNewLine($"            }};"));
            d.Append(CommonMethods.AddNewLine($"        }}"));
            d.Append(CommonMethods.AddNewLine($"    }}"));
            d.Append(CommonMethods.AddNewLine($"}}"));

            CommonMethods.CreateFile(d, fileDto);
        }

        #endregion

        #region AppService_FILE
        private void CreateAppServiceFile(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderNetCore, $"{className}AppService.cs");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"namespace Safran.{_projectName}.Application.{className}"));
            d.Append(CommonMethods.AddNewLine($"{{"));
            d.Append(CommonMethods.AddNewLine($"    using System.Collections.Generic;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Security.Principal;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Threading.Tasks;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.Dto.Base;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.Dto.User;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.RepoContract;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.Specification;"));
            d.Append(CommonMethods.AddNewLine($"    using Safran.PAS.Domain.Dto.{className};"));
            d.Append(CommonMethods.AddNewLine($"    using Safran.PAS.Domain.{className};"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Application;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Application.Authentication;"));
            d.Append(CommonMethods.AddNewLine($"    /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"    /// The application service used for {className}."));
            d.Append(CommonMethods.AddNewLine($"    /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"    public class {className}AppService : CrudAppServiceBase<{className}Dto, {className}, LazyLoadDto, {className}Mapper>, I{className}AppService"));
            d.Append(CommonMethods.AddNewLine($"    {{"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// The current SiteId."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        private readonly int currentSiteId;{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Initializes a new instance of the <see cref=\"{className}AppService\"/> class."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"repository\">The repository.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"principal\">The claims principal.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"planeQueryCustomizer\">The plane query customizer for include.</param>"));
            d.Append(CommonMethods.AddNewLine($"        public {className}AppService(ITGenericRepository<{className}> repository, IPrincipal principal)"));
            d.Append(CommonMethods.AddNewLine($"            : base(repository)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            this.currentSiteId = (principal as BIAClaimsPrincipal).GetUserData<UserDataDto>().CurrentSiteId;"));
            d.Append(CommonMethods.AddNewLine($"            // this.filtersContext.Add(AccessMode.Read, new DirectSpecification<{className}>(p => p.SiteId == this.currentSiteId));"));
            d.Append(CommonMethods.AddNewLine($"        }}{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <inheritdoc/>"));
            d.Append(CommonMethods.AddNewLine($"        public override Task<{className}Dto> AddAsync({className}Dto dto, string mapperMode = null)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            return base.AddAsync(dto, mapperMode);"));
            d.Append(CommonMethods.AddNewLine($"        }}{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Return a range to use in Calc SpreadSheet."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"filters\">The filter.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <returns><see cref=\"Task\"/>Representing the asynchronous operation.</returns>"));
            d.Append(CommonMethods.AddNewLine($"        public async Task<(IEnumerable<{className}Dto> Results, int Total)> GetRangeForCalcAsync(LazyLoadDto filters = null)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            return await this.GetRangeAsync<{className}Dto, {className}Mapper, LazyLoadDto>(filters: filters);"));
            d.Append(CommonMethods.AddNewLine($"        }}"));
            d.Append(CommonMethods.AddNewLine($"    }}"));
            d.Append(CommonMethods.AddNewLine($"}}"));

            CommonMethods.CreateFile(d, fileDto);
        }
        #endregion

        #region Interface_FILE
        private void CreateInterfaceFile(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderNetCore, $"I{className}AppService.cs");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"namespace Safran.{_projectName}.Application.{className}"));
            d.Append(CommonMethods.AddNewLine($"{{"));
            d.Append(CommonMethods.AddNewLine($"    using System.Collections.Generic;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Threading.Tasks;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Application;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.Dto.Base;"));
            d.Append(CommonMethods.AddNewLine($"    using Safran.PAS.Domain.{className};"));
            d.Append(CommonMethods.AddNewLine($"    using Safran.PAS.Domain.Dto.{className};"));
            d.Append(CommonMethods.AddNewLine($"    /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"    /// The interface defining the application service for {className}."));
            d.Append(CommonMethods.AddNewLine($"    /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"    public interface I{className}AppService : ICrudAppServiceBase<{className}Dto, {className}, LazyLoadDto>"));
            d.Append(CommonMethods.AddNewLine($"    {{"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Return a range to use in Calc SpreadSheet."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"filters\">The filter.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <returns><see cref=\"Task\"/>Representing the asynchronous operation.</returns>"));
            d.Append(CommonMethods.AddNewLine($"        Task<(IEnumerable<{className}Dto> Results, int Total)> GetRangeForCalcAsync(LazyLoadDto filters = null);"));
            d.Append(CommonMethods.AddNewLine($"    }}"));
            d.Append(CommonMethods.AddNewLine($"}}"));

            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion

        #region Controller_FILE
        private void CreateControllerFile(string className, string filename)
        {
            string classNameCamell = CommonMethods.ToCamelCase(className);
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderNetCore, $"{className}Controller.cs");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"namespace Safran.{_projectName}.Presentation.Api.Controllers"));
            d.Append(CommonMethods.AddNewLine($"{{"));
            d.Append(CommonMethods.AddNewLine($"    using System;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Collections.Generic;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Linq;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Security.Principal;"));
            d.Append(CommonMethods.AddNewLine($"    using System.Threading.Tasks;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Common;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Common.Exceptions;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Application.Authentication;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.Dto;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.Dto.Base;"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Core.Domain.Dto.User;"));
            d.Append(CommonMethods.AddNewLine($"#if UseHubForClientInPlane"));
            d.Append(CommonMethods.AddNewLine($"        using BIA.Net.Core.Domain.RepoContract;"));
            d.Append(CommonMethods.AddNewLine($"#endif"));
            d.Append(CommonMethods.AddNewLine($"    using BIA.Net.Presentation.Api.Controllers.Base;"));
            d.Append(CommonMethods.AddNewLine($"    using Hangfire;"));
            d.Append(CommonMethods.AddNewLine($"    using Microsoft.AspNetCore.Authorization;"));
            d.Append(CommonMethods.AddNewLine($"    using Microsoft.AspNetCore.Http;"));
            d.Append(CommonMethods.AddNewLine($"    using Microsoft.AspNetCore.Mvc;"));
            d.Append(CommonMethods.AddNewLine($"#if UseHubForClientInPlane"));
            d.Append(CommonMethods.AddNewLine($"        using Microsoft.AspNetCore.SignalR;"));
            d.Append(CommonMethods.AddNewLine($"#endif"));
            d.Append(CommonMethods.AddNewLine($"    using Safran.PAS.Application.{className};"));
            d.Append(CommonMethods.AddNewLine($"    using Safran.PAS.Crosscutting.Common;"));
            d.Append(CommonMethods.AddNewLine($"    using Safran.PAS.Domain.Dto.{className};"));
            d.Append(CommonMethods.AddNewLine($"    /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"    /// The API controller used to manage {className}."));
            d.Append(CommonMethods.AddNewLine($"    /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"    public class {className}Controller : BiaControllerBase"));
            d.Append(CommonMethods.AddNewLine($"    {{"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// The plane application service."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        private readonly I{className}AppService {classNameCamell}Service;"));

            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Initializes a new instance of the <see cref=\"{className}Controller\"/> class."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"{classNameCamell}Service\">The plane application service.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"clientForHubService\">The hub for client.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"principal\">The BIAClaimsPrincipal.</param>"));
            d.Append(CommonMethods.AddNewLine($"        public {className}Controller(I{className}AppService {classNameCamell}Service)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            this.{classNameCamell}Service = {classNameCamell}Service;"));
            d.Append(CommonMethods.AddNewLine($"        }}{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Get all attributes manager with filters."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"filters\">The filters.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <returns>The list of planes.</returns>"));
            d.Append(CommonMethods.AddNewLine($"        [HttpPost(\"all\")]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status200OK)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status400BadRequest)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status500InternalServerError)]"));
            d.Append(CommonMethods.AddNewLine($"        [Authorize(Roles = Rights.{className}.ListAccess)]"));
            d.Append(CommonMethods.AddNewLine($"        public async Task<IActionResult> GetAll([FromBody] LazyLoadDto filters)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            var (results, total) = await this.{classNameCamell}Service.GetRangeAsync(filters);"));
            d.Append(CommonMethods.AddNewLine($"            this.HttpContext.Response.Headers.Add(BIAConstants.HttpHeaders.TotalCount, total.ToString());"));
            d.Append(CommonMethods.AddNewLine($"            return this.Ok(results);"));
            d.Append(CommonMethods.AddNewLine($"        }}{Environment.NewLine}"));

            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Get all attributes manager  with filters containing id for Calc SpreadSheet."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"filters\">The filters.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <returns>The list of planes.</returns>"));
            d.Append(CommonMethods.AddNewLine($"        [HttpPost(\"allforcalc\")]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status200OK)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status400BadRequest)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status500InternalServerError)]"));
            d.Append(CommonMethods.AddNewLine($"        [Authorize(Roles = Rights.{className}.ListAccess)]"));
            d.Append(CommonMethods.AddNewLine($"        public async Task<IActionResult> GetAllForCalc([FromBody] LazyLoadDto filters)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            var (results, total) = await this.{classNameCamell}Service.GetRangeForCalcAsync(filters);"));
            d.Append(CommonMethods.AddNewLine($"            this.HttpContext.Response.Headers.Add(BIAConstants.HttpHeaders.TotalCount, total.ToString());"));
            d.Append(CommonMethods.AddNewLine($"            return this.Ok(results);"));
            d.Append(CommonMethods.AddNewLine($"        }}{Environment.NewLine}"));

            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Get attributes manager by its identifier."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"id\">The identifier.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <returns>The plane.</returns>"));
            d.Append(CommonMethods.AddNewLine($"        [HttpGet(\"{{id}}\")]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status200OK)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status400BadRequest)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status404NotFound)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status500InternalServerError)]"));
            d.Append(CommonMethods.AddNewLine($"        [Authorize(Roles = Rights.{className}.Read)]"));
            d.Append(CommonMethods.AddNewLine($"        public async Task<IActionResult> Get(int id)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            if (id == 0)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.BadRequest();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));

            d.Append(CommonMethods.AddNewLine($"            try"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                var dto = await this.{classNameCamell}Service.GetAsync(id);"));
            d.Append(CommonMethods.AddNewLine($"                return this.Ok(dto);"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (ElementNotFoundException)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.NotFound();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (Exception)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.StatusCode(500, \"Internal server error\");"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"        }}{Environment.NewLine}"));

            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Add a {className}."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"dto\">The plane DTO.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <returns>The result of the creation.</returns>"));
            d.Append(CommonMethods.AddNewLine($"        [HttpPost]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status201Created)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status400BadRequest)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status500InternalServerError)]"));
            d.Append(CommonMethods.AddNewLine($"        [Authorize(Roles = Rights.{className}.Create)]"));
            d.Append(CommonMethods.AddNewLine($"        public async Task<IActionResult> Add([FromBody] {className}Dto dto)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            try"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                var createdDto = await this.{classNameCamell}Service.AddAsync(dto);"));
            d.Append(CommonMethods.AddNewLine($"                //#if UseHubForClientInPlane"));
            d.Append(CommonMethods.AddNewLine($"                //                await this.clientForHubService.SendTargetedMessage(createdDto.SiteId.ToString(), \"{classNameCamell}\", \"refresh-{classNameCamell}\");"));
            d.Append(CommonMethods.AddNewLine($"                //#endif"));
            d.Append(CommonMethods.AddNewLine($"                return this.CreatedAtAction(\"Get\", new {{ id = createdDto.Id }}, createdDto);"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (ArgumentNullException)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.ValidationProblem();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (Exception)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.StatusCode(500, \"Internal server error\");"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"        }}{Environment.NewLine}"));

            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Update a {className}."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"id\">The plane identifier.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"dto\">The plane DTO.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <returns>The result of the update.</returns>"));
            d.Append(CommonMethods.AddNewLine($"        [HttpPut(\"{{id}}\")]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status200OK)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status400BadRequest)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status404NotFound)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status500InternalServerError)]"));
            d.Append(CommonMethods.AddNewLine($"        [Authorize(Roles = Rights.{className}.Update)]"));
            d.Append(CommonMethods.AddNewLine($"        public async Task<IActionResult> Update(int id, [FromBody] {className}Dto dto)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            if (id == 0 || dto == null || dto.Id != id)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.BadRequest();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));

            d.Append(CommonMethods.AddNewLine($"            try"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                var updatedDto = await this.{classNameCamell}Service.UpdateAsync(dto);"));
            d.Append(CommonMethods.AddNewLine($"                //#if UseHubForClientInPlane"));
            d.Append(CommonMethods.AddNewLine($"                //                _ = this.clientForHubService.SendTargetedMessage(updatedDto.SiteId.ToString(), \"{classNameCamell}\", \"refresh-{classNameCamell}\");"));
            d.Append(CommonMethods.AddNewLine($"                //#endif"));
            d.Append(CommonMethods.AddNewLine($"                return this.Ok(updatedDto);"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (ArgumentNullException)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.ValidationProblem();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (ElementNotFoundException)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.NotFound();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (Exception)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.StatusCode(500, \"Internal server error\");"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"        }}"));

            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Remove a {className}."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"id\">The plane identifier.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <returns>The result of the remove.</returns>"));
            d.Append(CommonMethods.AddNewLine($"        [HttpDelete(\"{{id}}\")]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status200OK)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status400BadRequest)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status404NotFound)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status500InternalServerError)]"));
            d.Append(CommonMethods.AddNewLine($"        [Authorize(Roles = Rights.{className}.Delete)]"));
            d.Append(CommonMethods.AddNewLine($"        public async Task<IActionResult> Remove(int id)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            if (id == 0)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.BadRequest();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));

            d.Append(CommonMethods.AddNewLine($"            try"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                await this.{classNameCamell}Service.RemoveAsync(id);"));
            d.Append(CommonMethods.AddNewLine($"                //#if UseHubForClientInPlane"));
            d.Append(CommonMethods.AddNewLine($"                //                _ = this.clientForHubService.SendTargetedMessage(deletedDto.SiteId.ToString(), \"{classNameCamell}\", \"refresh-{classNameCamell}\");"));
            d.Append(CommonMethods.AddNewLine($"                //#endif"));
            d.Append(CommonMethods.AddNewLine($"                return this.Ok();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (ElementNotFoundException)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.NotFound();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (Exception)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.StatusCode(500, \"Internal server error\");"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"        }}"));

            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Removes the specified {className} ids."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"ids\">The {classNameCamell} ids.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <returns>The result of the remove.</returns>"));
            d.Append(CommonMethods.AddNewLine($"        [HttpDelete]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status200OK)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status400BadRequest)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status404NotFound)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status500InternalServerError)]"));
            d.Append(CommonMethods.AddNewLine($"        [Authorize(Roles = Rights.{className}.Delete)]"));
            d.Append(CommonMethods.AddNewLine($"        public async Task<IActionResult> Remove([FromQuery] List<int> ids)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            if (ids?.Any() != true)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.BadRequest();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));

            d.Append(CommonMethods.AddNewLine($"            try"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                foreach (int id in ids)"));
            d.Append(CommonMethods.AddNewLine($"                {{"));
            d.Append(CommonMethods.AddNewLine($"                    await this.{classNameCamell}Service.RemoveAsync(id);"));
            d.Append(CommonMethods.AddNewLine($"                }}"));

            d.Append(CommonMethods.AddNewLine($"#if UseHubForClientInPlane"));
            d.Append(CommonMethods.AddNewLine($"                deletedDtos.Select(m => m.SiteId).Distinct().ToList().ForEach(parentId =>"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                _ = this.clientForHubService.SendTargetedMessage(parentId.ToString(), \"{classNameCamell}\", \"refresh-{classNameCamell}\");"));
            d.Append(CommonMethods.AddNewLine($"            }});"));
            d.Append(CommonMethods.AddNewLine($"#endif"));
            d.Append(CommonMethods.AddNewLine($"                return this.Ok();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (ElementNotFoundException)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.NotFound();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (Exception)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.StatusCode(500, \"Internal server error\");"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"        }}"));

            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Save all planes according to their state (added, updated or removed)."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"dtos\">The list of {classNameCamell}.</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <returns>The status code.</returns>"));
            d.Append(CommonMethods.AddNewLine($"        [HttpPost(\"save\")]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status200OK)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status400BadRequest)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status404NotFound)]"));
            d.Append(CommonMethods.AddNewLine($"        [ProducesResponseType(StatusCodes.Status500InternalServerError)]"));
            d.Append(CommonMethods.AddNewLine($"        [Authorize(Roles = Rights.{className}.Save)]"));
            d.Append(CommonMethods.AddNewLine($"        public async Task<IActionResult> Save(IEnumerable<{className}Dto> dtos)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"            var dtoList = dtos.ToList();"));
            d.Append(CommonMethods.AddNewLine($"            if (!dtoList.Any())"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.BadRequest();"));
            d.Append(CommonMethods.AddNewLine($"            }}{Environment.NewLine}"));

            d.Append(CommonMethods.AddNewLine($"            try"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                await this.{classNameCamell}Service.SaveAsync(dtoList);"));
            d.Append(CommonMethods.AddNewLine($"#if UseHubForClientInPlane"));
            d.Append(CommonMethods.AddNewLine($"            savedDtos.Select(m => m.SiteId).Distinct().ToList().ForEach(parentId =>"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                _ = this.clientForHubService.SendTargetedMessage(parentId.ToString(), \"{classNameCamell}\", \"refresh-{classNameCamell}\");"));
            d.Append(CommonMethods.AddNewLine($"            }});"));
            d.Append(CommonMethods.AddNewLine($"#endif"));
            d.Append(CommonMethods.AddNewLine($"                return this.Ok();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (ArgumentNullException)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.ValidationProblem();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (ElementNotFoundException)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.NotFound();"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"            catch (Exception)"));
            d.Append(CommonMethods.AddNewLine($"            {{"));
            d.Append(CommonMethods.AddNewLine($"                return this.StatusCode(500, \"Internal server error\");"));
            d.Append(CommonMethods.AddNewLine($"            }}"));
            d.Append(CommonMethods.AddNewLine($"        }}"));

            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// Generates a csv file according to the filters."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// <param name=\"filters\">filters ( <see cref=\"FileFiltersDto\"/>).</param>"));
            d.Append(CommonMethods.AddNewLine($"        /// <returns>a csv file.</returns>"));
            d.Append(CommonMethods.AddNewLine($"        [HttpPost(\"csv\")]"));
            d.Append(CommonMethods.AddNewLine($"        public virtual async Task<IActionResult> GetFile([FromBody] FileFiltersDto filters)"));
            d.Append(CommonMethods.AddNewLine($"        {{"));
            d.Append(CommonMethods.AddNewLine($"          byte[] buffer = await this.{classNameCamell}Service.GetCsvAsync(filters);"));
            d.Append(CommonMethods.AddNewLine($"          return this.File(buffer, BIAConstants.Csv.ContentType + \";charset=utf-8\", $\"{className} {{ BIAConstants.Csv.Extension}}\");"));
            d.Append(CommonMethods.AddNewLine($"        }}"));
            d.Append(CommonMethods.AddNewLine($"    }}"));
            d.Append(CommonMethods.AddNewLine($"}}"));


            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion

        #region Rights_FILE
        private void CreateRightsFile(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderNetCore, $"OPEN AND COPY_ Common_Rights_{className}.cs");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"    /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"    /// The {className} rights."));
            d.Append(CommonMethods.AddNewLine($"    /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"    public static class {className}"));
            d.Append(CommonMethods.AddNewLine($"    {{"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// The right to access to the list of {className}s."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        public const string ListAccess = \"{CommonMethods.FirstLetterUppercase(className)}_List_Access\";{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// The right to create {className}s."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        public const string Create = \"{CommonMethods.FirstLetterUppercase(className)}_Create\";{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// The right to read {className}s."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        public const string Read = \"{CommonMethods.FirstLetterUppercase(className)}_Read\";{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// The right to update {className}s."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        public const string Update = \"{CommonMethods.FirstLetterUppercase(className)}_Update\";{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// The right to delete {className}s."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        public const string Delete = \"{CommonMethods.FirstLetterUppercase(className)}_Delete\";{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// The right to save {className}s."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        public const string Save = \"{CommonMethods.FirstLetterUppercase(className)}_Save\";{Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"        /// <summary>"));
            d.Append(CommonMethods.AddNewLine($"        /// The right to save {className}s."));
            d.Append(CommonMethods.AddNewLine($"        /// </summary>"));
            d.Append(CommonMethods.AddNewLine($"        public const string Options = \"{CommonMethods.FirstLetterUppercase(className)}_Options\";"));

            d.Append(CommonMethods.AddNewLine($"    }}"));
            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion

        #region BIANETConfig_FILE
        private void CreateBIANETConfigFile(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderNetCore, $"OPEN AND COPY__bianetconfig.txt");
            StringBuilder d = new StringBuilder();


            d.Append(CommonMethods.AddNewLine($"// {className}"));
            d.Append(CommonMethods.AddNewLine($"{{"));
            d.Append(CommonMethods.AddNewLine($"\"Names\": [ \"{CommonMethods.FirstLetterUppercase(className)}_List_Access\", \"{CommonMethods.FirstLetterUppercase(className)}_Create\", \"{CommonMethods.FirstLetterUppercase(className)}_Read\", \"{CommonMethods.FirstLetterUppercase(className)}_Update\", \"{CommonMethods.FirstLetterUppercase(className)}_Delete\", \"{CommonMethods.FirstLetterUppercase(className)}_Save\", \"{CommonMethods.FirstLetterUppercase(className)}_Options\" ],"));
            d.Append(CommonMethods.AddNewLine($"\"Roles\": [ \"User\", \"Admin\", \"Site_Admin\" ]"));
            d.Append(CommonMethods.AddNewLine($"}}"));

            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion

        #region IocContainer_FILE
        private void CreateIocContainerFile(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderNetCore, $"OPEN AND COPY__IocContainer.txt");
            StringBuilder d = new StringBuilder();

            d.Append(CommonMethods.AddNewLine($"collection.AddTransient<I{className}AppService, {className}AppService>();"));

            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion

        #region Angular_Index_Component_FILE
        private void CreateAngular_Index_ComponentFile(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderAngular, $"OPEN AND COPY__views_{className}-index__{className}-index-Component_initTableConfiguration.ts");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"private initTableConfiguration() {{"));
            d.Append(CommonMethods.AddNewLine($"    this.biaTranslationService.culture$.subscribe((dateFormat) => {{"));
            d.Append(CommonMethods.AddNewLine($"      this.tableConfiguration = {{"));
            d.Append(CommonMethods.AddNewLine($"        columns: ["));
            foreach (string p in _propertiesList)
            {
                d.Append(CommonMethods.AddNewLine($"          new PrimeTableColumn('{CommonMethods.ToCamelCase(CommonMethods.GetValueFromList(p, 1))}', '{className.ToLower()}.{CommonMethods.ToCamelCase(CommonMethods.GetValueFromList(p, 1))}'),"));
            }

            d.Append(CommonMethods.AddNewLine($"        ]"));
            d.Append(CommonMethods.AddNewLine($"      }}; {Environment.NewLine}"));
            d.Append(CommonMethods.AddNewLine($"      this.columns = this.tableConfiguration.columns.map((col) => <KeyValuePair>{{ key: col.field, value: col.header }});"));
            d.Append(CommonMethods.AddNewLine($"      this.displayedColumns = [...this.columns];"));
            d.Append(CommonMethods.AddNewLine($"    }});"));
            d.Append(CommonMethods.AddNewLine($"  }}"));

            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion

        #region Angular_Model_FILE
        private void Create_Angular_Model_File(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderAngular, $"OPEN AND COPY__model_{className}.ts");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"//import {{ Site }} from 'src/app/domains/site/model/site';"));
            d.Append(CommonMethods.AddNewLine($"import {{ OptionDto }} from 'src/app/shared/bia-shared/model/option-dto';{Environment.NewLine}"));

            d.Append(CommonMethods.AddNewLine($"export interface {CommonMethods.FirstLetterUppercase(className)} {{"));
            foreach (string p in _propertiesList)
            {
                d.Append(CommonMethods.AddNewLine($"  {CommonMethods.ToCamelCase(CommonMethods.GetValueFromList(p, 1))}: {CommonMethods.ToAngularTypes(CommonMethods.GetValueFromList(p, 0))};"));

            }
            d.Append(CommonMethods.AddNewLine($"}}"));

            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion

        #region Angular_Translate_FILE
        private void Create_Angular_TranslateI18n_File(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderAngular, $"OPEN AND COPY___assets_i18n_App_{className}.json");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"\"{className.ToLower()}\": {{"));
            d.Append(CommonMethods.AddNewLine($"    \"add\": \"Add {className}\","));
            d.Append(CommonMethods.AddNewLine($"    \"edit\": \"Edit  {className} \","));
            d.Append(CommonMethods.AddNewLine($"    \"listOf\": \"List of {className}s \","));
            d.Append(CommonMethods.AddNewLine($"    \"title\": \"Title \","));

            foreach (string p in _propertiesList)
            {
                d.Append(CommonMethods.AddNewLine($"    \"{CommonMethods.ToCamelCase(CommonMethods.GetValueFromList(p, 1))}\": \"{(CommonMethods.GetValueFromList(p, 1))}\","));

            }

            d.Append(CommonMethods.AddNewLine($"}}"));
            CommonMethods.CreateFile(d, fileDto);
        }
        #endregion

        #region Angular_tableComponent_InitForm_FILE
        private void CreateAngular_TableComponent_InitForm_File(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderAngular, $"OPEN AND COPY__component_{className}-TABLE_{className}-table-component.ts");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"  public initForm() {{"));
            d.Append(CommonMethods.AddNewLine($"    this.form = this.formBuilder.group({{"));
            d.Append(CommonMethods.AddNewLine($"      id: [this.element.id], // This field is mandatory. Do not remove it."));
            foreach (string p in _propertiesList)
            {
                if (CommonMethods.GetValueFromList(p, 1).ToUpper() != "ID")
                {
                    d.Append(CommonMethods.AddNewLine($"     {CommonMethods.ToCamelCase(CommonMethods.GetValueFromList(p, 1))}: [this.element.{CommonMethods.ToCamelCase(CommonMethods.GetValueFromList(p, 1))}, Validators.required],"));
                }

            }


            d.Append(CommonMethods.AddNewLine($"    }});"));
            d.Append(CommonMethods.AddNewLine($"  }}"));

            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion

        #region Angular_FormComponent_InitForm_FILE
        private void CreateAngular_FormComponent_InitForm_File(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderAngular, $"OPEN AND COPY__component_{className}-FORM_{className}-form-components.ts");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"  private initForm() {{"));
            d.Append(CommonMethods.AddNewLine($"    this.form = this.formBuilder.group({{"));
            d.Append(CommonMethods.AddNewLine($"      id: [this.{className.ToLower()}.id], // This field is mandatory. Do not remove it."));
            foreach (string p in _propertiesList)
            {
                if (CommonMethods.GetValueFromList(p, 1).ToUpper() != "ID")
                {
                    d.Append(CommonMethods.AddNewLine($"     {CommonMethods.ToCamelCase(CommonMethods.GetValueFromList(p, 1))}: [this.{className.ToLower()}.{CommonMethods.ToCamelCase(CommonMethods.GetValueFromList(p, 1))}, Validators.required],"));
                }

            }


            d.Append(CommonMethods.AddNewLine($"    }});"));
            d.Append(CommonMethods.AddNewLine($"  }}"));

            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion

        #region Angular_Permission_FILE
        private void CreateAngularPermissionFile(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderAngular, $"OPEN AND COPY__shared_permission.txt");
            StringBuilder d = new StringBuilder();

            d.Append(CommonMethods.AddNewLine($"  {CommonMethods.FirstLetterUppercase(className)}_List_Access = \"{CommonMethods.FirstLetterUppercase(className)}_List_Access\","));
            d.Append(CommonMethods.AddNewLine($"  {CommonMethods.FirstLetterUppercase(className)}_Create = \"{CommonMethods.FirstLetterUppercase(className)}_Create\","));
            d.Append(CommonMethods.AddNewLine($"  {CommonMethods.FirstLetterUppercase(className)}_Read = \"{CommonMethods.FirstLetterUppercase(className)}_Read\","));
            d.Append(CommonMethods.AddNewLine($"  {CommonMethods.FirstLetterUppercase(className)}_Update = \"{CommonMethods.FirstLetterUppercase(className)}_Update\","));
            d.Append(CommonMethods.AddNewLine($"  {CommonMethods.FirstLetterUppercase(className)}_Delete = \"{CommonMethods.FirstLetterUppercase(className)}_Delete\","));
            d.Append(CommonMethods.AddNewLine($"  {CommonMethods.FirstLetterUppercase(className)}_Save = \"{CommonMethods.FirstLetterUppercase(className)}_Save\","));
            d.Append(CommonMethods.AddNewLine($"  {CommonMethods.FirstLetterUppercase(className)}_Options = \"{CommonMethods.FirstLetterUppercase(className)}_Options\","));

            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion

        #region Angular_Form_FILE
        private void CreateAngularFormComponentHtmlFile(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderAngular, $"OPEN AND COPY__component_{className}-form_{className}-component-html.txt");
            StringBuilder d = new StringBuilder();
            //d.Append(CommonMethods.AddNewLine($"namespace Safran.{_projectName}.Application.{className}"));
            //d.Append(CommonMethods.AddNewLine($"{{"));


            d.Append(CommonMethods.AddNewLine($"<form (submit)=\"onSubmit()\" fxLayout=\"column\" [formGroup]=\"form\">"));
            foreach (string p in _propertiesList)
            {
                d.Append(CommonMethods.AddNewLine(@"<div fxLayout=""row"" fxLayoutAlign=""center"">                                                              "));
                d.Append(CommonMethods.AddNewLine(@"    <div class=""app-field-container"" fxFlex=""90"">                                                        "));
                d.Append(CommonMethods.AddNewLine(@"        <span class=""md-inputfield"">                                                                       "));
                d.Append(CommonMethods.AddNewLine($"        <input formControlName=\"{CommonMethods.ToCamelCase(CommonMethods.GetValueFromList(p, 1))}\" type=\"text\" pInputText maxlength=\"64\" style =\"width:100%\"/>  "));
                d.Append(CommonMethods.AddNewLine($"        <label><span class=\"bia-star-mandatory\">*</span>{{{{ '{className.ToLower()}.{CommonMethods.ToCamelCase(CommonMethods.GetValueFromList(p, 1))}' | translate }}}}</label>"));
                d.Append(CommonMethods.AddNewLine(@"        </span>                                                                                              "));
                d.Append(CommonMethods.AddNewLine(@"    </div>                                                                                                   "));
                d.Append(CommonMethods.AddNewLine(@"</div>                                                                                                       "));
            }

            d.Append(CommonMethods.AddNewLine($" <div fxLayout=\"row\" fxLayoutGap=\"5px\" fxLayoutAlign=\"end end\">                                           "));
            d.Append(CommonMethods.AddNewLine($"     <button pButton label=\"{{{{ 'bia.save' | translate }}}}\" type=\"submit\" [disabled]=\"!form.valid\"></button>"));
            d.Append(CommonMethods.AddNewLine($"     <button                                                                                              "));
            d.Append(CommonMethods.AddNewLine($"       pButton                                                                                            "));
            d.Append(CommonMethods.AddNewLine($"       label=\"{{{{ 'bia.cancel' | translate }}}}\"                                                             "));
            d.Append(CommonMethods.AddNewLine($"       type=\"button\"                                                                                      "));
            d.Append(CommonMethods.AddNewLine($"       class=\"ui-button-secondary\"                                                                        "));
            d.Append(CommonMethods.AddNewLine($"       (click)=\"onCancel()\"                                                                               "));
            d.Append(CommonMethods.AddNewLine($"     ></button>                                                                                           "));
            d.Append(CommonMethods.AddNewLine($"   </div>                                                                                                 "));
            d.Append(CommonMethods.AddNewLine($"</form>                                                                                                  "));

            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion


        #region TEMPLATE_FILE
        private void CreateTEMPLATEFile(string className, string filename)
        {
            string fileDto = Path.Combine(_resultFolder.FullName, Constants.FolderAngular, $"{className}Dto.cs");
            StringBuilder d = new StringBuilder();
            d.Append(CommonMethods.AddNewLine($"namespace Safran.{_projectName}.Application.{className}"));
            d.Append(CommonMethods.AddNewLine($"{{"));


            d.Append(CommonMethods.AddNewLine($""));
            d.Append(CommonMethods.AddNewLine($""));
            d.Append(CommonMethods.AddNewLine($""));
            d.Append(CommonMethods.AddNewLine($""));
            d.Append(CommonMethods.AddNewLine($""));
            d.Append(CommonMethods.AddNewLine($""));
            d.Append(CommonMethods.AddNewLine($""));
            d.Append(CommonMethods.AddNewLine($""));
            d.Append(CommonMethods.AddNewLine($""));
            d.Append(CommonMethods.AddNewLine($""));
            d.Append(CommonMethods.AddNewLine($""));

            CommonMethods.CreateFile(d, fileDto);

        }
        #endregion


    }
}
