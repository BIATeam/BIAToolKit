namespace BIA.ToolKit.Domain.CRUDGenerator
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Collections.Generic;

    public class ClassDefinition
    {
        public string FileName { get; set; }

        public string? EntityName { get; set; }

        public string? CompagnyName { get; private set; }

        public string? ProjectName { get; private set; }

        private NamespaceDeclarationSyntax? namespaceSyntax;
        public NamespaceDeclarationSyntax? NamespaceSyntax
        {
            get { return namespaceSyntax; }
            set
            {
                if (namespaceSyntax != value)
                {
                    namespaceSyntax = value;
                    CompagnyName = namespaceSyntax?.Name.ToString().Split('.')[0];
                    ProjectName = namespaceSyntax?.Name.ToString().Split('.')[1];
                }
            }
        }

        public SyntaxTokenList VisibilityList { get; set; }

        public SyntaxKind Type { get; set; }

        public SyntaxToken Name { get; set; }

        public BaseListSyntax? BaseList { get; set; }

        public List<PropertyDeclarationSyntax> PropertyList { get; set; }

        public List<FieldDeclarationSyntax> FieldList { get; set; }

        public List<ConstructorDeclarationSyntax> ConstructorList { get; set; }

        public List<MethodDeclarationSyntax> MethodList { get; set; }

        public ClassDefinition(string fileName)
        {
            this.FileName = fileName;
            this.PropertyList = new();
            this.FieldList = new();
            this.ConstructorList = new();
            this.MethodList = new();
        }
    }
}
