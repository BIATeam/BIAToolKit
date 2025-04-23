namespace BIA.ToolKit.Domain.Model
{
    using BIA.ToolKit.Domain.Work;
    using Microsoft.CodeAnalysis;
    using System.Collections.ObjectModel;

    public class VersionAndOption
    {
        public VersionAndOption()
        {
            WorkCompanyFiles = new ObservableCollection<WorkRepository>();
            WorkTemplates = new ObservableCollection<WorkRepository>();
            Profiles = new ObservableCollection<string>();
            FeatureSettings = new ObservableCollection<FeatureSetting>();
            Options = new ObservableCollection<CFOption>();
        }
        public bool? Test { get; set; }

        public ObservableCollection<WorkRepository> WorkTemplates { get; set; }
        public ObservableCollection<string> Profiles { get; set; }
        public ObservableCollection<CFOption> Options { get; set; }
        public ObservableCollection<WorkRepository> WorkCompanyFiles { get; set; }

        public ObservableCollection<FeatureSetting> FeatureSettings { get; set; }

        // use as output
        // Framework version
        public WorkRepository? WorkTemplate { get; set; }

        public bool UseCompanyFiles { get; set; }
        // CompanyFileVersion
        public WorkRepository? WorkCompanyFile { get; set; }
        // Profile selected
        public string? Profile { get; set; }
    }
}
