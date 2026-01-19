using System.Collections.Generic;
using BIA.ToolKit.Application.Settings;
using BIA.ToolKit.Domain.ModifyProject;
using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;

namespace BIA.ToolKit.Application.Services
{
    /// <summary>
    /// Service interface for ZIP parsing and CRUD generation operations
    /// </summary>
    public interface IZipParserService
    {
        /// <summary>
        /// Parse all ZIP files for CRUD generation
        /// </summary>
        bool ParseZips(IEnumerable<ZipFeatureType> zipFeatures, Project project, string biaFront, CRUDSettings settings);

        /// <summary>
        /// Clean BIA folders
        /// </summary>
        void CleanBiaFolders(IEnumerable<ZipFeatureType> zipFeatures, Project project, string biaFront);
    }
}
