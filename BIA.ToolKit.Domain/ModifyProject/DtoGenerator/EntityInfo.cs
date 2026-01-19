
namespace BIA.ToolKit.Domain.DtoGenerator
{
    using System.Text.RegularExpressions;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ProjectAnalysis;
    using Humanizer;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public class EntityInfo
    {
        public EntityInfo(ClassInfo classInfo)
        {
            Path = classInfo.FilePath;
            Namespace = classInfo.Namespace;
            Name = classInfo.ClassName;
            BaseList = classInfo.BaseClassesChain.Concat(classInfo.AllInterfaces).Select(x => x.DisplayName).Distinct().ToList();
            BaseKeyType = CommonTools.GetBaseKeyType(BaseList);
            ClassAnnotations = new(classInfo.Attributes.SelectMany(x => x.Arguments));
            Properties = new(classInfo.PublicProperties.Select(x => new PropertyInfo(x)));
            IsAuditable = classInfo.Attributes.Any(x => x.Name == "AuditInclude");
        }

        public EntityInfo(string path, string @namespace, string name, string baseType, string baseKeyType, List<AttributeArgumentSyntax> arguments, List<string> baseList)
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
        public string NamespaceInProject => string.Join(".", Namespace.Split('.').Skip(3));
        public string CompanyName => Namespace.Split('.').First();
        public string ProjectName => Namespace.Split('.').ElementAt(1);
        public string Name { get; }
        public string FullNamespace => string.Join(".", Namespace, Name);
        public string NamePluralized => Name.Pluralize();
        public string BaseType { get; }
        public List<string> BaseList { get; }
        public List<PropertyInfo> Properties { get; } = new List<PropertyInfo>();
        public string CompositeKeyName { get; set; }
        public List<PropertyInfo> CompositeKeys { get; } = new List<PropertyInfo>();
        public List<KeyValuePair<string, string>> ClassAnnotations { get; } = new();
        public string BaseKeyType { get; set; }
        public bool IsTeam => BaseList.Contains("Team") || BaseList.Contains("TeamDto") || BaseList.Any(x => (x.StartsWith("BaseEntity") || x.StartsWith("BaseDto")) && x.Contains("Team"));
        public bool IsVersioned => IsTeam || BaseList.Contains("VersionedTable") || BaseList.Any(x => (x.StartsWith("BaseEntity") || x.StartsWith("BaseDto")) && x.Contains("Versioned"));
        public bool IsFixable => BaseList.Any(x => x.StartsWith("IEntityFixable<")) || BaseList.Any(x => (x.StartsWith("BaseEntity") || x.StartsWith("BaseDto")) && x.Contains("Fixable"));
        public bool IsArchivable => IsFixable || BaseList.Any(x => x.StartsWith("IEntityArchivable<")) || BaseList.Any(x => (x.StartsWith("BaseEntity") || x.StartsWith("BaseDto")) && x.Contains("Archivable"));
        public string AncestorTeamName => ClassAnnotations.FirstOrDefault(c => c.Key == CRUDDataUpdateType.AncestorTeam.ToString()).Value;
        public bool HasAncestorTeam => !string.IsNullOrWhiteSpace(AncestorTeamName);
        public bool IsAuditable { get; set; }


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
