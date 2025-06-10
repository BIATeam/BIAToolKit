
namespace BIA.ToolKit.Domain.DtoGenerator
{
    using System.Text.RegularExpressions;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using Humanizer;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public class EntityInfo
    {
        public EntityInfo(string path, string @namespace, string name, string? baseType, string? baseKeyType, List<AttributeArgumentSyntax>? arguments, List<string>? baseList)
        {
            Path = path;
            Namespace = @namespace;
            Name = name;
            BaseType = baseType;
            BaseList = baseList ?? new List<string>();
            BaseKeyType = baseKeyType ?? CommonTools.GetBaseKeyType(BaseList);
            if (arguments != null && arguments.Count > 0)
            {
                ClassAnnotations = new();
                ParseAnnotations(arguments);
            }
        }

        public string Namespace { get; }
        public string Path { get; }
        public string NamespaceLastPart => Namespace.Split('.').Last();
        public string CompanyName => Namespace.Split('.').First();
        public string ProjectName => Namespace.Split('.').ElementAt(1);
        public string Name { get; }
        public string FullNamespace => string.Join(".", Namespace, Name);
        public string NamePluralized => Name.Pluralize();
        public string? BaseType { get; }
        public List<string> BaseList { get; }
        public List<PropertyInfo> Properties { get; } = new List<PropertyInfo>();
        public string? CompositeKeyName { get; set; }
        public List<PropertyInfo> CompositeKeys { get; } = new List<PropertyInfo>();
        public List<KeyValuePair<string, string>> ClassAnnotations { get; } = new();
        public string BaseKeyType { get; set; }
        public bool IsTeam => BaseList.Contains("Team") || BaseList.Contains("TeamDto");
        public bool IsVersioned => BaseList.Contains("VersionedTable");
        public bool IsFixable => BaseList.Any(x => x.StartsWith("IEntityFixable<"));
        public bool IsArchivable => IsFixable || BaseList.Any(x => x.StartsWith("IEntityArchivable<"));
        public string AncestorTeamName => ClassAnnotations.FirstOrDefault(c => c.Key == CRUDDataUpdateType.AncestorTeam.ToString()).Value;
        public bool HasAncestorTeam => !string.IsNullOrWhiteSpace(AncestorTeamName);

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