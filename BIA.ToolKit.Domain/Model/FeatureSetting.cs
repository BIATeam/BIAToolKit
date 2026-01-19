namespace BIA.ToolKit.Domain.Model
{
    public class FeatureSetting
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is selected.
        /// </summary>
        public bool IsSelected { get; set; } = true;

        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// Gets or sets the folders to excludes.
        /// </summary>
        public List<string> FoldersToExcludes { get; set; } = [];

        /// <summary>
        /// Gets or sets the features ID to disabled when current feature is enabled.
        /// </summary>
        public List<int> DisabledFeatures { get; set; } = [];
    }
}
