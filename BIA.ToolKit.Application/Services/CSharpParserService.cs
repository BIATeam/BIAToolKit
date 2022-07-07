namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Extensions;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Models;
    using BIA.ToolKit.Application.Parser;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class CSharpParserService
    {
        private IConsoleWriter consoleWriter;

        public CSharpParserService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void ParseEntity(string fileName)
        {
            var cancellationToken = new CancellationToken();

            var fileText = File.ReadAllText(fileName);

            var tree = CSharpSyntaxTree.ParseText(fileText, cancellationToken: cancellationToken);
            var root = tree.GetCompilationUnitRoot(cancellationToken: cancellationToken);
            if (root.ContainsDiagnostics)
            {
                // source contains syntax error
                var ex = new ParseException(root.GetDiagnostics().Select(diag => diag.ToString()));
                throw ex;
            }

            BaseNamespaceDeclarationSyntax? namespaceSyntax = root
                .Descendants<NamespaceDeclarationSyntax>()
                .SingleOrDefault();

            namespaceSyntax ??= root
                .Descendants<FileScopedNamespaceDeclarationSyntax>()
                .SingleOrDefault();

            var @namespace = namespaceSyntax?.Name.ToString();

            /*var relativeDirectory = @namespace
                .RemovePreFix(projectInfo.FullName + ".")
                .Replace('.', '/');*/

            var classDeclarationSyntax = root
                .Descendants<ClassDeclarationSyntax>()
                .Single();

            var className = classDeclarationSyntax.Identifier.ToString();
            var baseList = classDeclarationSyntax.BaseList!;

            var genericNameSyntax = baseList
                .Descendants<SimpleBaseTypeSyntax>()
                .First(node => !node.ToFullString().StartsWith("I")) // Not interface
                .Descendants<GenericNameSyntax>()
                .FirstOrDefault();

            string baseType;
            string? primaryKey;
            IEnumerable<string>? keyNames = null;
            if (genericNameSyntax == null)
            {
                // No generic parameter -> Entity with Composite Keys
                baseType = baseList.Descendants<SimpleBaseTypeSyntax>().Single(node => !node.ToFullString().StartsWith("I")).Type.ToString();
                primaryKey = null;

                // Get composite keys
                /*var getKeysMethod = root.Descendants<MethodDeclarationSyntax>().Single(m => m.Identifier.ToString() == "GetKeys");
                keyNames = getKeysMethod
                    .Descendants<InitializerExpressionSyntax>()
                    .First()
                    .Descendants<IdentifierNameSyntax>()
                    .Select(id => id.Identifier.ToString());*/
                keyNames = new List<string>() { "Id"};
            }
            else
            {
                // Normal entity
                baseType = genericNameSyntax.Identifier.ToString();
                primaryKey = genericNameSyntax.Descendants<TypeArgumentListSyntax>().Single().Arguments[0].ToString();
            }

            var properties = root.Descendants<PropertyDeclarationSyntax>()
                    .Select(prop => new PropertyInfo(prop.Type.ToString(), prop.Identifier.ToString()))
                    .ToList()
                ;
            var entityInfo = new EntityInfo(@namespace, className, baseType, primaryKey/*, relativeDirectory*/);
            entityInfo.Properties.AddRange(properties);
            if (keyNames != null)
            {
                entityInfo.CompositeKeyName = $"{className}Key";
                entityInfo.CompositeKeys.AddRange(
                    keyNames.Select(k => properties.Single(prop => prop.Name == k)));
            }


        }

    }
}
