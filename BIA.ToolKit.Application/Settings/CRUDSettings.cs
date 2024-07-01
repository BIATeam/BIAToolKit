namespace BIA.ToolKit.Application.Settings
{
    using BIA.ToolKit.Application.Services;

    public class CRUDSettings
    {
        private readonly SettingsService settingsService;

        public bool GenerateInProjectFolder { get; private set; } = true;
        // File name
        public string GenerationSettingsFileName { get; private set; }
        public string GenerationHistoryFileName { get; private set; }

        public string DtoCustomAttributeName { get; private set; }

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
            GenerationHistoryFileName = settingsService.ReadSetting("GenerationHistory");
            DtoCustomAttributeName = settingsService.ReadSetting("DtoCustomAttribute");
            PackageLockFileName = settingsService.ReadSetting("PackageLockFileName");

            string generate = settingsService.ReadSetting("GenerateInProjectFolder");
            if (!string.IsNullOrWhiteSpace(generate))
            {
                GenerateInProjectFolder = bool.Parse(generate);
            }
        }
    }
}
