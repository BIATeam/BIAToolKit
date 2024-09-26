namespace BIA.ToolKit.Application.Settings
{
    using BIA.ToolKit.Application.Services;

    public class CRUDSettings
    {
        private readonly SettingsService settingsService;

        // File name
        public string GenerationSettingsFileName { get; private set; }
        public string CrudGenerationHistoryFileName { get; private set; }
        public string OptionGenerationHistoryFileName { get; private set; }

        public string DtoCustomAttributeFieldName { get; private set; }

        public string DtoCustomAttributeClassName { get; private set; }

        public string PackageLockFileName { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CRUDSettings(SettingsService settingsService)
        {
            this.settingsService = settingsService;

            Init();
        }

        /// <summary>
        /// Read settings to init variables.
        /// </summary>
        private void Init()
        {
            GenerationSettingsFileName = settingsService.ReadSetting("GenerationSettings");
            CrudGenerationHistoryFileName = settingsService.ReadSetting("CrudGenerationHistory");
            OptionGenerationHistoryFileName = settingsService.ReadSetting("OptionGenerationHistory");
            DtoCustomAttributeFieldName = settingsService.ReadSetting("DtoCustomAttributeField");
            DtoCustomAttributeClassName = settingsService.ReadSetting("DtoCustomAttributeClass");
            PackageLockFileName = settingsService.ReadSetting("PackageLockFileName");
        }
    }
}
