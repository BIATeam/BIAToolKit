namespace BIA.ToolKit.Domain.CRUDGenerator
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Collections.Generic;

    public class ClassDefinition
    {
        public string FileName { get; set; }

        public NamespaceDeclarationSyntax? NamespaceSyntax { get; set; }

        public SyntaxTokenList VisibilityList { get; set; }

        public SyntaxKind Type { get; set; }

        public SyntaxToken Name { get; set; }

        public BaseListSyntax? BaseList { get; set; }

        public List<FieldDeclarationSyntax> PropertyList { get; set; }

        public List<ConstructorDeclarationSyntax> ConstructorList { get; set; }

        public List<MethodDeclarationSyntax> MethodList { get; set; }

        public ClassDefinition(string fileName)
        {
            this.FileName = fileName;
            PropertyList = new();
            ConstructorList = new();
            MethodList = new();
        }
    }
}
