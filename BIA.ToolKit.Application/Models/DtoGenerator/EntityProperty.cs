namespace BIA.ToolKit.Application.Models.DtoGenerator
{
    using System.Collections.Generic;
    using System.Linq;
    using CommunityToolkit.Mvvm.ComponentModel;

    public partial class EntityProperty : ObservableObject
    {
        public string Name { get; set; }
        public string Type { get; set; }

        [ObservableProperty]
        private bool isSelected;

        public string CompositeName { get; set; }
        public List<EntityProperty> Properties { get; set; } = [];
        public string ParentType { get; set; }

        public List<EntityProperty> GetAllPropertiesRecursively()
        {
            var allProperties = new List<EntityProperty> { this };
            allProperties.AddRange(Properties.SelectMany(x => x.GetAllPropertiesRecursively()));
            return allProperties;
        }
    }
}
