
namespace BIA.ToolKit.Domain.DtoGenerator
{
    using System.Text.RegularExpressions;
    using BIA.ToolKit.Common;
    using Humanizer;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public class EntityInfo
    {
        
        public EntityInfo(string path, string @namespace, string name, string? baseType, string? primaryKey, List<AttributeArgumentSyntax>? arguments, List<string>? baseList)
        {
            Path = path;
            Namespace = @namespace;
            Name = name;
            BaseType = baseType;
            PrimaryKey = primaryKey;
            BaseList = baseList ?? new List<string>();
            CommonTools.GetBaseKey(BaseList);
            if (arguments != null && arguments.Count > 0)
            {
                ClassAnnotations = new();
                ParseAnnotations(arguments);
            }
        }

        public string Namespace { get; }
        public string Path { get; }
        public string NamespaceLastPart => Namespace.Split('.').Last();
        public string CompagnyName => Namespace.Split('.').First();
        public string ProjectName => Namespace.Split('.').ElementAt(1);
        public string Name { get; }
        public string FullNamespace => string.Join(".", Namespace, Name);
        public string NamePluralized => Name.Pluralize();
        public string? BaseType { get; }
        public string? PrimaryKey { get; }
        public List<string> BaseList { get; }
        public List<PropertyInfo> Properties { get; } = new List<PropertyInfo>();
        public string? CompositeKeyName { get; set; }
        public List<PropertyInfo> CompositeKeys { get; } = new List<PropertyInfo>();
        public List<KeyValuePair<string, string>> ClassAnnotations { get; }
        public string BaseKeyType { get; set; }

        private void ParseAnnotations(List<AttributeArgumentSyntax> annotations)
        {
            const string regex = @"(\w*)\s?=\s?""?(\w*)""?";
            foreach (AttributeArgumentSyntax annotation in annotations)
            {
                string key = CommonTools.GetMatchRegexValue(regex, annotation.ToString(), 1);
                string value = CommonTools.GetMatchRegexValue(regex, annotation.ToString(), 2);
                if (!string.IsNullOrEmpty(key))
                {
                    ClassAnnotations.Add(new KeyValuePair<string, string>(key, value));
                }
            }
        }
    }
}