namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Extensions;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Parser;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.DtoGenerator;
    using Microsoft.Build.Locator;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /*   using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;*/

    public class CSharpParserService
    {
        private readonly IConsoleWriter consoleWriter;

        public CSharpParserService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public EntityInfo ParseEntity(string fileName)
        {
#if DEBUG
            consoleWriter.AddMessageLine($"*** Parse file: '{fileName}' ***", "Green");
#endif

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
                keyNames = new List<string>() { "Id" };
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

            return entityInfo;
        }

        public ClassDefinition ParseClassFile(string fileName)
        {
#if DEBUG
            consoleWriter.AddMessageLine($"*** Parse file: '{fileName}' ***", "Green");
#endif

            var cancellationToken = new CancellationToken();

            var fileText = File.ReadAllText(fileName);

            var tree = CSharpSyntaxTree.ParseText(fileText, cancellationToken: cancellationToken);
            var root = tree.GetCompilationUnitRoot(cancellationToken: cancellationToken);
            if (root.ContainsDiagnostics)
            {
                // source contains syntax error
                throw new ParseException(root.GetDiagnostics().Select(diag => diag.ToString()));
            }

            TypeDeclarationSyntax typeDeclaration = root.Descendants<TypeDeclarationSyntax>().SingleOrDefault();
            NamespaceDeclarationSyntax namespaceSyntax = root.Descendants<NamespaceDeclarationSyntax>().SingleOrDefault();

            //if (typeDeclaration.IsKind(SyntaxKind.ClassDeclaration))
            //{
            //    ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)typeDeclaration;
            //}
            //if (typeDeclaration.IsKind(SyntaxKind.InterfaceDeclaration))
            //{
            //    InterfaceDeclarationSyntax interfaceDeclaration = (InterfaceDeclarationSyntax)typeDeclaration;
            //}

            ClassDefinition classDefinition = new(fileName)
            {
                NamespaceSyntax = namespaceSyntax,
                Name = typeDeclaration.Identifier,
                Type = typeDeclaration.Kind(),
                BaseList = typeDeclaration.BaseList,
                VisibilityList = typeDeclaration.Modifiers,
            };

            List<MemberDeclarationSyntax> propertyList = typeDeclaration.Members.Where(x => x.Kind() == SyntaxKind.FieldDeclaration).ToList();
            if (propertyList != null && propertyList.Any())
            {
                propertyList.ForEach(x => classDefinition.PropertyList.Add((FieldDeclarationSyntax)x));
            }

            List<MemberDeclarationSyntax> constructorList = typeDeclaration.Members.Where(x => x.Kind() == SyntaxKind.ConstructorDeclaration).ToList();
            if (constructorList != null && constructorList.Any())
            {
                constructorList.ForEach(x => classDefinition.ConstructorList.Add((ConstructorDeclarationSyntax)x));
            }

            List<MemberDeclarationSyntax> methodList = typeDeclaration.Members.Where(x => x.Kind() == SyntaxKind.MethodDeclaration).ToList();
            if (methodList != null && methodList.Any())
            {
                methodList.ForEach(x => classDefinition.MethodList.Add((MethodDeclarationSyntax)x));
            }

            return classDefinition;
        }

        public async Task ParseSolution(string projectPath)
        {
            try
            {
                MSBuildLocator.RegisterDefaults();
                var workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();
                var project = await workspace.OpenProjectAsync(projectPath);
                var compilation = await project.GetCompilationAsync();

                foreach (var diagnostic in compilation.GetDiagnostics()
                    .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error))
                {
                    Console.WriteLine(diagnostic);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            /*            var workspace = Workspace.LoadSolution(projectPath);
                       var solution = workspace.CurrentSolution;

                        foreach (var projectId in solution.ProjectIds)
                        {
                            var project = solution.GetProject(projectId);
                            foreach (var documentId in project.DocumentIds)
                            {
                                var document = project.GetDocument(documentId);
                                CompilationUnitSyntax compilationUnit = (CompilationUnitSyntax)document.GetSyntaxRoot();
                                Debug.WriteLine(String.Format("compilationUnit={0} before", compilationUnit.GetHashCode()));
                                Debug.WriteLine(String.Format("project={0} before", project.GetHashCode()));
                                Debug.WriteLine(String.Format("solution={0} before", solution.GetHashCode()));
                                var mcu = new AnnotatorSyntaxRewritter().Visit(compilationUnit);
                                var project2 = document.UpdateSyntaxRoot(mcu).Project;
                                if (mcu != compilationUnit)
                                {
                                    solution = project2.Solution;
                                }
                                Debug.WriteLine(String.Format("compilationUnit={0} after", mcu.GetHashCode()));
                                Debug.WriteLine(String.Format("project={0} after", project2.GetHashCode()));
                                Debug.WriteLine(String.Format("solution={0} after", solution.GetHashCode()));
                            }
                        }

                        foreach (var projectId in solution.ProjectIds)
                        {
                            var project = solution.GetProject(projectId);
                            foreach (var documentId in project.DocumentIds)
                            {
                                var document = project.GetDocument(documentId);
                                var compilationUnit = document.GetSyntaxRoot();
                                var semanticModel = document.GetSemanticModel();
                                Debug.WriteLine(String.Format("compilationUnit={0} stage", compilationUnit.GetHashCode()));
                                Debug.WriteLine(String.Format("project={0} stage", project.GetHashCode()));
                                Debug.WriteLine(String.Format("solution={0}", solution.GetHashCode()));

                                MySyntaxWalker sw = new MySyntaxWalker(semanticModel);
                                sw.Visit((SyntaxNode)compilationUnit);
                            }
                        }*/
        }
    }
}
