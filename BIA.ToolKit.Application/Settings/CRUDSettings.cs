namespace BIA.ToolKit.Application.Settings
{
    using BIA.ToolKit.Application.Services;

    public class CRUDSettings
    {
        private readonly SettingsService settingsService;

        public bool GenerateInProjectFolder { get; private set; } = true;
        // Entity name
        public string CRUDReferenceSingular { get; private set; }
        public string CRUDReferencePlurial { get; private set; }
        public string OptionReferenceSingular { get; private set; }
        public string OptionReferencePlurial { get; private set; }
        public string TeamReferenceSingular { get; private set; }
        public string TeamReferencePlurial { get; private set; }
        // Zip files name
        public string ZipNameBack { get; private set; }
        public string ZipNameFeature { get; private set; }
        public string ZipNameOption { get; private set; }
        public string ZipNameTeam { get; private set; }
        // File name
        public string OptionsToRemoveFileName { get; private set; }
        // DotNet files to update path
        public string DotNetBianetConfigPath { get; set; }
        public string DotNetRightsPath { get; set; }
        public string DotNetIocContainerPath { get; set; }

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
            CRUDReferenceSingular = settingsService.ReadSetting("CrudReferenceSingular");
            CRUDReferencePlurial = settingsService.ReadSetting("CrudReferencePlurial");
            OptionReferenceSingular = settingsService.ReadSetting("OptionReferenceSingular");
            OptionReferencePlurial = settingsService.ReadSetting("OptionReferencePlurial");
            TeamReferenceSingular = settingsService.ReadSetting("TeamReferenceSingular");
            TeamReferencePlurial = settingsService.ReadSetting("TeamReferencePlurial");

            ZipNameBack = settingsService.ReadSetting("ZipNameBack_DotNet");
            ZipNameFeature = settingsService.ReadSetting("ZipNameFeature_Angular");
            ZipNameOption = settingsService.ReadSetting("ZipNameOption_Angular");
            ZipNameTeam = settingsService.ReadSetting("ZipNameTeam_Angular");

            OptionsToRemoveFileName = settingsService.ReadSetting("OptionsToRemove");

            DotNetBianetConfigPath = settingsService.ReadSetting("DotNetBianetConfigPath");
            DotNetRightsPath = settingsService.ReadSetting("DotNetRightsPath");
            DotNetIocContainerPath = settingsService.ReadSetting("DotNetIocContainerPath");

            string generate = settingsService.ReadSetting("GenerateInProjectFolder");
            if (!string.IsNullOrWhiteSpace(generate))
            {
                GenerateInProjectFolder = bool.Parse(generate);
            }
        }
    }
}
