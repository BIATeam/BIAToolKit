namespace BIA.ToolKit.Domain.Model
{
    using BIA.ToolKit.Domain.Work;
    using System.Collections.ObjectModel;

    public class VersionAndOption
    {
        public VersionAndOption()
        {
            WorkCompanyFiles = new ObservableCollection<WorkRepository>();
            WorkTemplates = new ObservableCollection<WorkRepository>();
            Profiles = new ObservableCollection<string>();
            FeatureSettings = new ObservableCollection<FeatureSetting>();
        }
        public bool? Test { get; set; }

        public ObservableCollection<WorkRepository>? WorkTemplates { get; set; }
        public ObservableCollection<string>? Profiles { get; set; }
        public ObservableCollection<WorkRepository>? WorkCompanyFiles { get; set; }

        public ObservableCollection<FeatureSetting>? FeatureSettings { get; set; }

        // use as output
        public WorkRepository? WorkTemplate { get; set; }
        public bool UseCompanyFiles { get; set; }
        public WorkRepository? WorkCompanyFile { get; set; }
        public string? Profile { get; set; }
        public IList<CFOption>? Options { get; set; }
    }
}
