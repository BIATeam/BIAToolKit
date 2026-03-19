namespace BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class FeatureRegenerationItem : INotifyPropertyChanged
    {
        private string fromVersionOverride;

        public event PropertyChangedEventHandler PropertyChanged;

        public string EntityNameSingular { get; set; }
        public string FeatureType { get; set; }
        public string ToVersion { get; set; }

        /// <summary>Version stored in the .bia history file. Null/empty means not stored.
        /// This is set once at construction and never changed — computed properties that derive
        /// from it do not need to raise <see cref="PropertyChanged"/>.</summary>
        public string StoredFromVersion { get; set; }

        /// <summary>True when no version was stored in history — the user must select one.</summary>
        public bool IsFromVersionEditable => string.IsNullOrEmpty(StoredFromVersion);

        /// <summary>True when the version is already stored in history (read-only display).</summary>
        public bool IsFromVersionFixed => !string.IsNullOrEmpty(StoredFromVersion);

        /// <summary>User-selected override, used only when <see cref="IsFromVersionEditable"/> is true.</summary>
        public string FromVersionOverride
        {
            get => fromVersionOverride;
            set
            {
                if (fromVersionOverride != value)
                {
                    fromVersionOverride = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EffectiveFromVersion));
                }
            }
        }

        /// <summary>The effective FROM version: the stored version if present, otherwise the user-selected override.</summary>
        public string EffectiveFromVersion => !string.IsNullOrEmpty(StoredFromVersion) ? StoredFromVersion : FromVersionOverride;

        /// <summary>Available framework versions for the dropdown.</summary>
        public ObservableCollection<string> AvailableVersions { get; set; } = [];

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
