namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock
{
    public class CRUDPropertyType
    {
        public string Name { get; }

        private string? type;
        public string? Type
        {
            get { return type; }
            set
            {
                type = value;
                SimplifiedType = type?.Split('|')[0].Trim();
            }
        }

        public string? SimplifiedType { get; private set; }

        public CRUDPropertyType(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }
}
