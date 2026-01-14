namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Extensions;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Parser;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ProjectAnalysis;
    using Microsoft.Build.Locator;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.MSBuild;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /*   using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;*/

    public class CSharpParserService
    {
        private readonly List<string> excludedEntitiesFilesSuffixes = new() { "Mapper", "Service", "Repository", "Customizer", "Specification", "Dto" };
        private readonly IConsoleWriter consoleWriter;
        private readonly UIEventBroker eventBroker;
        private Solution CurrentSolution;
        public IReadOnlyList<ClassInfo> CurrentSolutionClasses { get; private set; } = [];

        private MSBuildWorkspace Workspace { get; set; }

        public CSharpParserService(IConsoleWriter consoleWriter, UIEventBroker eventBroker)
        {
            this.consoleWriter = consoleWriter;
            this.eventBroker = eventBroker;
        }

        public ClassDefinition ParseClassFile(string fileName)
        {
#if DEBUG
            consoleWriter.AddMessageLine($"Parse file: '{fileName}'", "Green");
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

            TypeDeclarationSyntax typeDeclaration;
            var descendants = root.Descendants<TypeDeclarationSyntax>();
            if (descendants.Count() == 1)
            {
                typeDeclaration = descendants.Single();
            }
            else if (descendants.Count() > 1)
            {
#if DEBUG
                consoleWriter.AddMessageLine($"More of one declaration found on file '{fileName}' :", "Orange");
                descendants.ToList().ForEach(x => consoleWriter.AddMessageLine($"   - {x.Identifier} ({x.Kind()})", "Orange"));
#endif
                // TODO NMA 
                typeDeclaration = descendants.Where(x => x.IsKind(SyntaxKind.ClassDeclaration)).Single();
            }
            else
            {
                consoleWriter.AddMessageLine($"No declaration found on file '{fileName}' :", "Orange");
                typeDeclaration = null;
            }
            NamespaceDeclarationSyntax namespaceSyntax = root.Descendants<NamespaceDeclarationSyntax>().SingleOrDefault();

            ClassDefinition classDefinition = new(fileName)
            {
                NamespaceSyntax = namespaceSyntax,
                Name = typeDeclaration.Identifier,
                Type = typeDeclaration.Kind(),
                BaseList = typeDeclaration.BaseList,
                VisibilityList = typeDeclaration.Modifiers,
            };

            List<MemberDeclarationSyntax> propertyList = typeDeclaration.Members.Where(x => x.Kind() == SyntaxKind.PropertyDeclaration).ToList();
            if (propertyList != null && propertyList.Any())
            {
                propertyList.ForEach(x => classDefinition.PropertyList.Add((PropertyDeclarationSyntax)x));
            }

            List<MemberDeclarationSyntax> fieldList = typeDeclaration.Members.Where(x => x.Kind() == SyntaxKind.FieldDeclaration).ToList();
            if (fieldList != null && fieldList.Any())
            {
                fieldList.ForEach(x => classDefinition.FieldList.Add((FieldDeclarationSyntax)x));
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

        public List<Domain.DtoGenerator.PropertyInfo> GetPropertyList(List<PropertyDeclarationSyntax> propertyList, string dtoCustomAttributeName)
        {
            return propertyList.Select(prop =>
            {
                foreach (AttributeListSyntax attributes in prop.AttributeLists.ToList())
                {
                    foreach (AttributeSyntax attribute in attributes.Attributes.ToList())
                    {
                        string annontationType = attribute.Name.ToString();
                        if (dtoCustomAttributeName.Equals(annontationType, StringComparison.OrdinalIgnoreCase))
                        {
                            List<AttributeArgumentSyntax> annotations = attribute?.ArgumentList?.Arguments.ToList();
                            return new Domain.DtoGenerator.PropertyInfo(prop.Type.ToString(), prop.Identifier.ToString(), annotations);
                        }
                    }
                }
                return new Domain.DtoGenerator.PropertyInfo(prop.Type.ToString(), prop.Identifier.ToString(), null);
            }).ToList();
        }

        public static List<AttributeArgumentSyntax> GetClassAnnotationList(SyntaxList<AttributeListSyntax> attributeLists, string dtoCustomClassName)
        {
            //List<KeyValuePair<string, string>> annotationList = new List<KeyValuePair<string, string>>();
            foreach (AttributeListSyntax attributes in attributeLists)
            {
                foreach (AttributeSyntax attribute in attributes.Attributes.ToList())
                {
                    string annontationType = attribute.Name.ToString();
                    if (dtoCustomClassName.Equals(annontationType, StringComparison.OrdinalIgnoreCase))
                    {
                        return attribute?.ArgumentList?.Arguments.ToList();
                    }
                }
            }
            return null;
        }

        public IEnumerable<EntityInfo> GetDomainEntities(Domain.ModifyProject.Project project)
        {
            List<EntityInfo> entities = new();

            string entitiesFolder = $"{project.CompanyName}.{project.Name}.Domain";
            string projectDomainPath = Path.Combine(project.Folder, Constants.FolderDotNet, entitiesFolder);
            string projectDomainDtoPath = projectDomainPath + ".Dto";

            try
            {
                if (Directory.Exists(projectDomainPath))
                {
                    foreach (var entity in CurrentSolutionClasses.Where(x =>
                        !x.FilePath.StartsWith(projectDomainDtoPath, StringComparison.InvariantCultureIgnoreCase)
                        && x.FilePath.StartsWith(projectDomainPath, StringComparison.InvariantCultureIgnoreCase)
                        && x.FilePath.EndsWith(".cs")
                        && !excludedEntitiesFilesSuffixes.Any(suffix => Path.GetFileNameWithoutExtension(x.FilePath).EndsWith(suffix))))
                    {
                        entities.Add(new EntityInfo(entity));
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }

            return entities.OrderBy(x => x.Name);
        }

        public void RegisterMSBuild(IConsoleWriter consoleWriter)
        {
            try
            {
                if (MSBuildLocator.IsRegistered)
                {
                    InitWorkspace();
                    return;
                }

                var instances = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(x => x.Version).ToList();
                if (instances.Count > 0)
                {
                    MSBuildLocator.RegisterInstance(instances.First());
                    InitWorkspace();
                    return;
                }

                var msBuildDirectories = new List<DirectoryInfo>();

                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var vsDirectory = new DirectoryInfo(Path.Combine(programFiles, "Microsoft Visual Studio"));
                if (vsDirectory.Exists)
                {
                    msBuildDirectories.AddRange(vsDirectory.GetDirectories("MSBuild", SearchOption.AllDirectories));
                }

                var msbuildPath = msBuildDirectories
                    .SelectMany(msBuildDir => msBuildDir.GetFiles("MSBuild.exe", SearchOption.AllDirectories))
                    .OrderByDescending(msBuild => msBuild.LastWriteTimeUtc)
                    .FirstOrDefault()?.FullName;

                if (string.IsNullOrWhiteSpace(msbuildPath))
                {
                    consoleWriter.AddMessageLine("Error: MSBuild is not installed on this system.", "red");
                    return;
                }

                MSBuildLocator.RegisterMSBuildPath(Path.GetDirectoryName(msbuildPath));
                InitWorkspace();
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error: Failed to register MSBuild : {ex.Message}", "red");
            }
        }

        private void InitWorkspace()
        {
            Workspace = MSBuildWorkspace.Create();

            if (Workspace == null)
            {
                consoleWriter.AddMessageLine("Error: Workspace could not be created.", "red");
                return;
            }
        }

        public async Task LoadSolution(string solutionPath)
        {
            if (Workspace is null)
            {
                consoleWriter.AddMessageLine($"MSBuildWorkspace has not been initialized", "red");
                return;
            }

            if (!await RestoreSolution(solutionPath))
                return;

            consoleWriter.AddMessageLine("Opening solution...", "darkgray");
            Workspace.CloseSolution();
            var solution = await Workspace.OpenSolutionAsync(solutionPath);

            if (solution == null)
            {
                consoleWriter.AddMessageLine($"Error: Solution at path '{solutionPath}' could not be loaded.", "red");
                return;
            }

            consoleWriter.AddMessageLine($"Solution loaded successfully", "lightgreen");
            CurrentSolution = solution;
        }

        public async Task ParseSolutionClasses()
        {
            var result = new List<ClassInfo>();
            consoleWriter.AddMessageLine("Parsing classes...", "darkgray");

            var classesInfosPerProjectTasks = CurrentSolution.Projects.Select(GetClassesInfoFromProject);
            var classesInfosReports = await Task.WhenAll(classesInfosPerProjectTasks);
            foreach (var report in classesInfosReports)
            {
                consoleWriter.AddMessageLine($"{report.Project} : {report.ClassesCount} classes parsed in {report.ElapsedSeconds} seconds", "gray");
            }
            CurrentSolutionClasses = classesInfosReports.SelectMany(x => x.ClassesInfo).ToList();
            eventBroker.NotifySolutionClassesParsed();
            consoleWriter.AddMessageLine($"Classes parsed successfully", "lightgreen");
        }

        private async Task<(List<ClassInfo> ClassesInfo, string Project, int ClassesCount, double ElapsedSeconds)> GetClassesInfoFromProject(Project project)
        {
            var result = new List<ClassInfo>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Compilation (nécessaire pour symboles/semantics)
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            if (compilation is null)
            {
                return (result, project.Name, 0, 0);
            }

            // Énumérer tous les types nommés
            var allTypes = RoslynHelper.GetAllNamedTypes(compilation.Assembly.GlobalNamespace)
                .Where(t =>
                    t.TypeKind == TypeKind.Class
                    && t.DeclaredAccessibility == Accessibility.Public
                    && !t.IsStatic
                    && !t.IsAbstract)
                .ToList();

            foreach (var cls in allTypes)
            {
                // Fichier source principal (si partiel, on prend la 1re location source)
                var filePath = cls.Locations.FirstOrDefault(l => l.IsInSource)?.SourceTree?.FilePath ?? string.Empty;

                // Attributs de la classe
                var classAttributes = cls.GetAttributes()
                    .Select(RoslynHelper.ToAttributeInfo)
                    .ToList();

                // Propriétés publiques accessibles (héritage inclus)
                var properties = RoslynHelper.GetAllAccessiblePublicProperties(cls)
                    .Select(p => new Domain.ProjectAnalysis.PropertyInfo(
                        TypeName: RoslynHelper.Display(p.Type),
                        Name: p.Name,
                        IsExplicitInterfaceImplementation: p.ExplicitInterfaceImplementations.Length > 0,
                        Attributes: p.GetAttributes().Select(RoslynHelper.ToAttributeInfo).ToList()
                    ))
                    .ToList();

                // Chaîne de bases
                var baseChain = RoslynHelper.GetBaseTypes(cls)
                    .Select(RoslynHelper.ToInheritedTypeInfo)
                    .ToList();

                // Interfaces (transitives, dédupliquées par nom complet)
                var interfaces = cls.AllInterfaces
                    .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
                    .Select(RoslynHelper.ToInheritedTypeInfo)
                    .ToList();

                result.Add(new ClassInfo(
                    ClassName: RoslynHelper.Display(cls), // inclut génériques si présents
                    FilePath: filePath,
                    Namespace: cls.ContainingNamespace?.ToDisplayString() ?? "",
                    IsGeneric: cls.IsGenericType,
                    Attributes: classAttributes,
                    PublicProperties: properties,
                    BaseClassesChain: baseChain,
                    AllInterfaces: interfaces
                ));
            }

            stopwatch.Stop();

            return (result, project.Name, result.Count, stopwatch.Elapsed.TotalSeconds);
        }

        public async Task FixUsings()
        {
            if(string.IsNullOrWhiteSpace(CurrentSolution?.FilePath))
            {
                consoleWriter.AddMessageLine("No solution loaded to fix usings.", "red");
                return;
            }

            consoleWriter.AddMessageLine("Start fix usings", "pink");

            try
            {
                await LoadSolution(CurrentSolution.FilePath);
                foreach (var project in CurrentSolution.Projects)
                {
                    try
                    {
                        consoleWriter.AddMessageLine($"Analyzing project {project.Name}...", "darkgray");

                        foreach (var document in project.Documents)
                        {
                            try
                            {
                                if (await document.GetSyntaxRootAsync() is not CompilationUnitSyntax syntaxRoot)
                                {
                                    consoleWriter.AddMessageLine($"-> {document.Name} : No compilation unit syntax root found.", "orange");
                                    continue;
                                }

                                var compilation = await project.GetCompilationAsync();
                                if (compilation == null)
                                {
                                    consoleWriter.AddMessageLine($"-> {document.Name} : Compilation not available.", "orange");
                                    continue;
                                }

                                var documentSyntaxTree = await document.GetSyntaxTreeAsync();
                                if (documentSyntaxTree == null)
                                {
                                    consoleWriter.AddMessageLine($"-> {document.Name} : No syntax tree available.", "orange");
                                    continue;
                                }

                                var semanticModel = compilation.GetSemanticModel(documentSyntaxTree);
                                if (semanticModel == null)
                                {
                                    consoleWriter.AddMessageLine($"-> {document.Name} : No semantic model available.", "orange");
                                    continue;
                                }

                                // Handle missing usings
                                var updatedRoot = AddMissingUsings(document.Name, syntaxRoot, compilation, semanticModel);
                                // Handle obsolete usings
                                updatedRoot = RemoveObsoleteUsings(semanticModel, document.Name, updatedRoot);
                                // Handle order usings
                                updatedRoot = OrderUsings(updatedRoot, document.Name);

                                var formattedRoot = Microsoft.CodeAnalysis.Formatting.Formatter.Format(updatedRoot, Workspace);
                                File.WriteAllText(document.FilePath!, formattedRoot.ToFullString());
                            }
                            catch (Exception docEx)
                            {
                                consoleWriter.AddMessageLine($"-> {document.Name} : {docEx.Message}\n{docEx.StackTrace}", "red");
                            }
                        }
                    }
                    catch (Exception projEx)
                    {
                        consoleWriter.AddMessageLine($"{projEx.Message}\n{projEx.StackTrace}", "red");
                    }
                }
            }
            catch (Exception solEx)
            {
                consoleWriter.AddMessageLine($"Error opening solution: {solEx.Message}\n{solEx.StackTrace}", "red");
            }
            finally
            {
                consoleWriter.AddMessageLine("End fix usings", "pink");
            }
        }

        private async Task<bool> RestoreSolution(string solutionPath)
        {
            if (IsSolutionRestored(solutionPath))
                return true;

            consoleWriter.AddMessageLine("Restore solution...", "darkgray");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"restore \"{solutionPath}\"",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                }
            };

            process.Start();
            var error = process.StandardError.ReadToEnd();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                consoleWriter.AddMessageLine($"Restore failed: {error}", "red");
                return false;
            }

            consoleWriter.AddMessageLine($"Restore succeed", "lightgreen");
            return true;
        }

        private static bool IsSolutionRestored(string solutionPath)
        {
            var solutionDir = Path.GetDirectoryName(solutionPath)!;
            var csprojFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories).Where(x => !x.Contains("BIAPackage"));
            return csprojFiles.All(IsProjectRestored);
        }

        private static bool IsProjectRestored(string projectFilePath)
        {
            var projectDir = Path.GetDirectoryName(projectFilePath);
            var assetsFile = Path.Combine(projectDir!, "obj", "project.assets.json");
            return File.Exists(assetsFile);
        }

        private CompilationUnitSyntax AddMissingUsings(string documentName, CompilationUnitSyntax syntaxRoot, Compilation compilation, SemanticModel semanticModel)
        {
            var missingUsingDiagnostics = semanticModel.GetDiagnostics()
                                                .Where(d => d.Id == "CS0246" || d.Id == "CS0118" || d.Id == "CS0103")
                                                .ToList();

            var typesWithMissingNamespace = missingUsingDiagnostics
                .Select(d =>
                {
                    var message = d.GetMessage();
                    if (string.IsNullOrWhiteSpace(message))
                        return string.Empty;

                    var extractTypeRegex = @"'([^']*)'";
                    var match = Regex.Match(message, extractTypeRegex);
                    var typeName = d.Id switch
                    {
                        "CS0118" => match.Success ? match.Groups[1].Value : string.Empty,
                        "CS0246" => match.Success ? match.Groups[1].Value : string.Empty,
                        "CS0103" => match.Success ? match.Groups[1].Value : string.Empty,
                        _ => string.Empty
                    };

                    if (string.IsNullOrWhiteSpace(typeName))
                        return string.Empty;

                    return ExtractTypeName(typeName);
                })
                .Where(ns => !string.IsNullOrWhiteSpace(ns))
                .Distinct()
                .ToList();

            var missingNamespaces = new List<string>();
            var typesWithMultipleNamespaces = new List<string>();
            var typesWithoutNamespaces = new List<string>();
            foreach (var type in typesWithMissingNamespace)
            {
                var namespaces = FindNamespaces(type, compilation);
                if (namespaces.Count() == 1)
                    missingNamespaces.Add(namespaces.First());
                else if (namespaces.Count() > 1)
                    typesWithMultipleNamespaces.Add(type);
                else
                    typesWithoutNamespaces.Add(type);
            }

            if (typesWithMultipleNamespaces.Count != 0)
                consoleWriter.AddMessageLine($"-> {documentName} : Multiple namespaces candidates to resolve using for types {string.Join(", ", typesWithMultipleNamespaces)}", "orange");
            if (typesWithoutNamespaces.Count != 0)
                consoleWriter.AddMessageLine($"-> {documentName} : Unable to resolve usings namespace for types {string.Join(", ", typesWithoutNamespaces)}", "orange");

            var updatedRoot = syntaxRoot;

            if (missingNamespaces.Count == 0)
                return updatedRoot;

            var usingDirectives = syntaxRoot.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Where(u => u.Name != null)
                .ToList();

            var existingNamespaces = usingDirectives
                .Select(u => u.Name!.ToString())
                .ToHashSet();

            var newUsings = missingNamespaces
                .Where(ns => !existingNamespaces.Contains(ns))
                .Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)))
                .ToList();

            if (newUsings.Count == 0)
                return updatedRoot;

            var lastUsing = usingDirectives.LastOrDefault();
            updatedRoot = lastUsing != null ?
                syntaxRoot.ReplaceNode(lastUsing, new[] { lastUsing }.Concat(newUsings)) :
                syntaxRoot.AddUsings(newUsings.ToArray());

            consoleWriter.AddMessageLine($"-> {documentName} : {missingNamespaces.Count} missing using added", "lightgreen");

            return updatedRoot;
        }

        private CompilationUnitSyntax RemoveObsoleteUsings(SemanticModel semanticModel, string documentName, CompilationUnitSyntax syntaxRoot)
        {
            var obsoleteNamespaceDiagnostics = semanticModel.GetDiagnostics()
                                                .Where(d => d.Id == "CS0234")
                                                .ToList();

            if (obsoleteNamespaceDiagnostics.Count == 0)
                return syntaxRoot;

            var usingsRemovedCount = 0;
            var updatedRoot = syntaxRoot;
            foreach (var obsoleteNamespaceDiagnostic in obsoleteNamespaceDiagnostics)
            {
                var diagnosticSpan = obsoleteNamespaceDiagnostic.Location.SourceSpan;
                var diagnosticNode = syntaxRoot.FindNode(diagnosticSpan);

                if (diagnosticNode is IdentifierNameSyntax identifierName)
                {
                    var usingDirective = identifierName.Ancestors()
                                                       .OfType<UsingDirectiveSyntax>()
                                                       .FirstOrDefault();

                    if (usingDirective != null)
                    {
                        updatedRoot = updatedRoot.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepDirectives);
                        usingsRemovedCount++;
                    }
                    else
                    {
                        consoleWriter.AddMessageLine($"-> {documentName} : No matching using directive found for {identifierName.Identifier.Text}.", "orange");
                    }
                }
                else
                {
                    consoleWriter.AddMessageLine($"-> {documentName} : Unexpected node type at {diagnosticSpan}.", "orange");
                }
            }

            if (usingsRemovedCount > 0)
            {
                consoleWriter.AddMessageLine($"-> {documentName} : {usingsRemovedCount} obsolete using removed", "yellow");
            }

            return updatedRoot;
        }

        private CompilationUnitSyntax OrderUsings(CompilationUnitSyntax syntaxRoot, string documentName)
        {
            var updatedRoot = syntaxRoot;

            if (syntaxRoot.Usings.Any())
            {
                var ordered = OrderUsingsList(syntaxRoot.Usings);
                updatedRoot = updatedRoot.WithUsings(ordered);
            }

            var namespaceNodes = updatedRoot.DescendantNodes()
                                            .OfType<NamespaceDeclarationSyntax>()
                                            .ToList();

            updatedRoot = updatedRoot.ReplaceNodes(namespaceNodes, (oldNs, _) =>
            {
                if (!oldNs.Usings.Any())
                    return oldNs;

                var ordered = OrderUsingsList(oldNs.Usings);
                return oldNs.WithUsings(ordered);
            });

            if (!syntaxRoot.IsEquivalentTo(updatedRoot))
            {
                consoleWriter.AddMessageLine($"-> {documentName} : usings sorted", "lightgreen");
            }

            return updatedRoot;
        }

        private static SyntaxList<UsingDirectiveSyntax> OrderUsingsList(SyntaxList<UsingDirectiveSyntax> usings)
        {
            var systemUsings = usings.Where(u => u.Name is IdentifierNameSyntax id && id.Identifier.Text.StartsWith("System") ||
                                                  u.Name is QualifiedNameSyntax qn && qn.ToString().StartsWith("System"))
                                      .OrderBy(u => u.Name.ToString(), StringComparer.OrdinalIgnoreCase);

            var otherUsings = usings.Except(systemUsings)
                                    .OrderBy(u => u.Name.ToString(), StringComparer.OrdinalIgnoreCase);

            return SyntaxFactory.List(systemUsings.Concat(otherUsings));
        }

        private static string ExtractTypeName(string typeName) => typeName.Contains('<') ? typeName[..typeName.IndexOf('<')] : typeName;

        private IEnumerable<string> FindNamespaces(string typeName, Compilation compilation)
        {
            var result = new List<string>();

            try
            {
                foreach (var symbol in compilation.GlobalNamespace.GetMembers())
                {
                    var matchingType = FindType(symbol, typeName);
                    if (matchingType != null)
                        result.Add(matchingType.ContainingNamespace.ToDisplayString());
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error resolving namespace for type '{typeName}': {ex.Message}\n{ex.StackTrace}", "red");
            }

            result.AddRange(FindNamespacesInReferences(typeName, compilation));

            return result.Distinct();
        }

        private List<string> FindNamespacesInReferences(string typeName, Compilation compilation)
        {
            var result = new List<string>();

            foreach (var reference in compilation.References)
            {
                try
                {
                    if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
                    {
                        continue;
                    }

                    if (assemblySymbol.TypeNames.Contains(typeName))
                    {
                        foreach (var symbol in assemblySymbol.GlobalNamespace.GetMembers())
                        {
                            var matchingType = FindType(symbol, typeName);
                            if (matchingType != null)
                                result.Add(matchingType.ContainingNamespace.ToDisplayString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Error resolving namespace in references for type '{typeName}': {ex.Message}\n{ex.StackTrace}", "red");
                }
            }

            return result;
        }

        private static INamedTypeSymbol FindType(INamespaceOrTypeSymbol symbol, string typeName)
        {
            if (symbol is INamedTypeSymbol typeSymbol && typeSymbol.Name == typeName)
                return typeSymbol;

            var memberSymbols = symbol.GetMembers()
                .Where(m => m is INamespaceOrTypeSymbol)
                .Cast<INamespaceOrTypeSymbol>()
                .ToList();

            foreach (var memberSymbol in memberSymbols)
            {
                var memberTypeSymbol = FindType(memberSymbol, typeName);
                if (memberTypeSymbol != null)
                    return memberTypeSymbol;
            }

            return null;
        }
    }
}
