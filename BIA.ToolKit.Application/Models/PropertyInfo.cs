namespace BIA.ToolKit.Application.Models
{
    public class PropertyInfo
    {
        public string Type { get; }

        public string Name { get; }

        public PropertyInfo(string type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}