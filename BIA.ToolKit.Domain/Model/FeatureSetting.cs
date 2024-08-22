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
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is selected.
        /// </summary>
        public bool IsSelected { get; set; } = true;

        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Gets or sets the folders to excludes.
        /// </summary>
        public List<string>? FoldersToExcludes { get; set; }

        /// <summary>
        /// Gets all.
        /// </summary>
        /// <returns></returns>
        public static List<FeatureSetting> GetAll()
        {
            return new List<FeatureSetting>()
            {
                new FeatureSetting()
                {
                    Id = 1,
                    DisplayName = "FrontEnd",
                    Description = "FrontEnd desc",
                    IsSelected = false,
                    Tags = new List<string>() {"BIA_FRONT_FEATURE" },
                    FoldersToExcludes = new List<string>()
                    {
                        ".*Angular.*$"
                    }
                },
                new FeatureSetting()
                {
                    Id = 2,
                    DisplayName = "BackToBackAuth",
                    Description = "BackToBackAuth desc",
                    Tags = new List<string>() {"BIA_SERVICE_API" },
                    IsSelected = true,

                },
                new FeatureSetting()
                {
                    Id = 3,
                    DisplayName = "DeployDb",
                    Description = "DeployDb desc",
                    IsSelected = true,
                    FoldersToExcludes = new List<string>()
                    {
                        ".*DeployDB$"
                    }
                },
                new FeatureSetting()
                {
                    Id = 4,
                    DisplayName = "WorkerService",
                    Description = "WorkerService desc",
                    IsSelected = true,
                    FoldersToExcludes = new List<string>()
                    {
                        ".*WorkerService$"
                    }
                },
            };
        }
    }
}
