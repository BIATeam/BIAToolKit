namespace BIA.ToolKit.Domain.DtoGenerator
{
    public class PropertyInfo
    {
        public string Type { get; }

        public string Name { get; }

        public string Annotation { get; }

        public PropertyInfo(string type, string name, string annotation = "")
        {
            Type = type;
            Name = name;
            Annotation = annotation;
        }
    }
}