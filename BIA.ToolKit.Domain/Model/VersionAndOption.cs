namespace BIA.ToolKit.Domain.Model
{
    using BIA.ToolKit.Domain.Work;
    using Microsoft.CodeAnalysis;
    using System.Collections.ObjectModel;

    public class VersionAndOption
    {
        public VersionAndOption()
        {
            WorkCompanyFiles = [];
            WorkTemplates = [];
            Profiles = [];
            Options = [];
        }
        public bool? Test { get; set; }

        public ObservableCollection<WorkRepository> WorkTemplates { get; set; }
        public ObservableCollection<string> Profiles { get; set; }
        public ObservableCollection<CFOption> Options { get; set; }
        public ObservableCollection<WorkRepository> WorkCompanyFiles { get; set; }

        public List<FeatureSetting> FeatureSettings { get; set; } = [];

        // use as output
        // Framework version
        public WorkRepository WorkTemplate { get; set; }

        public bool UseCompanyFiles { get; set; }
        // CompanyFileVersion
        public WorkRepository WorkCompanyFile { get; set; }
        // Profile selected
        public string Profile { get; set; }

        /// <summary>
        /// Indicates whether the default team should be created for the entity.
        /// </summary>
        public bool HasDefaultTeam { get; set; }

        /// <summary>
        /// Gets or sets the default team name.
        /// </summary>
        public string DefaultTeamName { get; set; }

        /// <summary>
        /// Gets or sets the default team name plural.
        /// </summary>
        public string DefaultTeamNamePlural { get; set; }

        /// <summary>
        /// Gets or sets the default team domain name.
        /// </summary>
        public string DefaultTeamDomainName { get; set; }
    }
}
