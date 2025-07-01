namespace BIA.ToolKit.Domain.DtoGenerator
{
    using BIA.ToolKit.Common;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public class PropertyInfo
    {
        public string Type { get; }

        public string Name { get; }

        public List<KeyValuePair<string, string>> Annotations { get; } = new List<KeyValuePair<string, string>>();
        public bool IsOptionDto => Type.Equals("OptionDto") || Type.Equals("ICollection<OptionDto>");

        public PropertyInfo(string type, string name, List<AttributeArgumentSyntax> arguments)
        {
            Type = type;
            Name = name;
            if (arguments != null && arguments.Count > 0)
            {
                ParseAnnotations(arguments);
            }
        }

        private void ParseAnnotations(List<AttributeArgumentSyntax> annotations)
        {
            const string regex = @"(\w*)\s?=\s?""?(\w*)""?";
            foreach (AttributeArgumentSyntax annotation in annotations)
            {
                string key = CommonTools.GetMatchRegexValue(regex, annotation.ToString(), 1);
                string value = CommonTools.GetMatchRegexValue(regex, annotation.ToString(), 2);
                if (!string.IsNullOrEmpty(key))
                {
                    Annotations.Add(new KeyValuePair<string, string>(key, value));
                }
            }
        }
    }
}