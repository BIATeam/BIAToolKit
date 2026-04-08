namespace BIA.ToolKit.Application.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;

    /// <summary>
    /// Centralises the loading, saving, and version-stamping of all generation history files
    /// (CRUD, Option, DTO).
    /// <para>
    /// Eliminates duplicated <c>CommonTools.DeserializeJsonFile</c> /
    /// <c>SerializeToJsonFile</c> calls that previously appeared in
    /// <c>RegenerateFeaturesUC</c>, <c>CRUDGeneratorUC</c>, <c>OptionGeneratorUC</c>,
    /// <c>DtoGeneratorUC</c>, and <c>RegenerateFeaturesDiscoveryService</c>.
    /// </para>
    /// </summary>
    public class GenerationHistoryService(SettingsService settingsService, EntityResolutionService entityResolutionService)
    {
        private readonly CRUDSettings crudSettings = new(settingsService);
        private readonly EntityResolutionService entityResolutionService = entityResolutionService;

        // ── CRUD history ──────────────────────────────────────────────────────

        public string GetCrudHistoryFilePath(Project project)
            => Path.Combine(project.Folder, Constants.FolderBia, crudSettings.CrudGenerationHistoryFileName);

        public CRUDGeneration LoadCrudHistory(Project project)
            => CommonTools.DeserializeJsonFile<CRUDGeneration>(GetCrudHistoryFilePath(project));

        public void SaveCrudHistory(Project project, CRUDGeneration history)
            => CommonTools.SerializeToJsonFile(history, GetCrudHistoryFilePath(project));

        // ── Option history ────────────────────────────────────────────────────

        public string GetOptionHistoryFilePath(Project project)
            => Path.Combine(project.Folder, Constants.FolderBia, crudSettings.OptionGenerationHistoryFileName);

        public OptionGeneration LoadOptionHistory(Project project)
            => CommonTools.DeserializeJsonFile<OptionGeneration>(GetOptionHistoryFilePath(project));

        public void SaveOptionHistory(Project project, OptionGeneration history)
            => CommonTools.SerializeToJsonFile(history, GetOptionHistoryFilePath(project));

        // ── DTO history ───────────────────────────────────────────────────────

        public string GetDtoHistoryFilePath(Project project)
            => Path.Combine(project.Folder, Constants.FolderBia, crudSettings.DtoGenerationHistoryFileName);

        public DtoGenerationHistory LoadDtoHistory(Project project)
            => CommonTools.DeserializeJsonFile<DtoGenerationHistory>(GetDtoHistoryFilePath(project));

        public void SaveDtoHistory(Project project, DtoGenerationHistory history)
            => CommonTools.SerializeToJsonFile(history, GetDtoHistoryFilePath(project));

        // ── Framework version stamping ────────────────────────────────────────

        /// <summary>
        /// Updates the <c>FrameworkVersion</c> field of the history entry for the given
        /// entity and feature type to the current project version and persists the file.
        /// Throws <see cref="InvalidOperationException"/> on failure so the caller can log the
        /// message without the service having a direct dependency on <c>IConsoleWriter</c>.
        /// </summary>
        public void UpdateFrameworkVersion(Project project, string entityName, string featureType)
        {
            try
            {
                switch (featureType)
                {
                    case "CRUD":
                    {
                        CRUDGeneration history = LoadCrudHistory(project);
                        if (history == null) return;
                        CRUDGenerationHistory entry = history.CRUDGenerationHistory
                            .FirstOrDefault(h => string.Equals(h.EntityNameSingular, entityName, StringComparison.OrdinalIgnoreCase));
                        if (entry != null)
                        {
                            entry.FrameworkVersion = project.FrameworkVersion;
                            SaveCrudHistory(project, history);
                        }
                        break;
                    }

                    case "Option":
                    {
                        OptionGeneration history = LoadOptionHistory(project);
                        if (history == null) return;
                        OptionGenerationHistory entry = history.OptionGenerationHistory
                            .FirstOrDefault(h => string.Equals(h.EntityNameSingular, entityName, StringComparison.OrdinalIgnoreCase));
                        if (entry != null)
                        {
                            entry.FrameworkVersion = project.FrameworkVersion;

                            // Back-populate EntityNamespace for older history entries that predate
                            // the fix so future discovery can use the fast namespace-based path.
                            if (string.IsNullOrEmpty(entry.EntityNamespace))
                            {
                                string ns = entityResolutionService.ResolveOptionEntityNamespace(entry, project);
                                if (!string.IsNullOrEmpty(ns))
                                    entry.EntityNamespace = ns;
                            }

                            SaveOptionHistory(project, history);
                        }
                        break;
                    }

                    case "DTO":
                    {
                        DtoGenerationHistory history = LoadDtoHistory(project);
                        if (history == null) return;
                        DtoGeneration entry = history.Generations
                            .FirstOrDefault(h => string.Equals(h.EntityName, entityName, StringComparison.OrdinalIgnoreCase));
                        if (entry != null)
                        {
                            entry.FrameworkVersion = project.FrameworkVersion;
                            SaveDtoHistory(project, history);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Could not update FrameworkVersion in history for {entityName}/{featureType}: {ex.Message}", ex);
            }
        }
    }
}
