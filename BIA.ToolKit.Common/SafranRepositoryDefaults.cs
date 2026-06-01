namespace BIA.ToolKit.Common
{
    /// <summary>
    /// Hardcoded Safran-internal default sources, surfaced by the Config
    /// tab empty states so users can bootstrap each repository category
    /// in one click without typing URLs or paths.
    ///
    /// Tune these constants if forking the ToolKit for another company.
    /// </summary>
    public static class SafranRepositoryDefaults
    {
        public const string ToolkitUpdateSourceName = "BIAToolkit Shared";
        public const string ToolkitUpdateSourcePath = @"\\share.bia.safran\BIAToolKit\Releases\BiaToolkit";

        public const string TemplatesSourceName = "BIATemplate Shared";
        public const string TemplatesSourcePath = @"\\share.bia.safran\BIAToolKit\Releases\BiaTemplate";

        public const string CompanyFilesSourceName = "BIACompanyFiles Azure";
        public const string CompanyFilesSourceUrl = "https://azure.devops.safran/SafranElectricalAndPower/Digital%20Manufacturing/_git/BIACompanyFiles";
    }
}
