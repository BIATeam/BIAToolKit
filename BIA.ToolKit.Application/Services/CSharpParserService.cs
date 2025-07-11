﻿namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Extensions;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Parser;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.DtoGenerator;
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
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /*   using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;*/

    public class CSharpParserService
    {
        private readonly List<string> excludedEntitiesFolders = new() { "bin", "obj" };
        private readonly List<string> excludedEntitiesFilesSuffixes = new() { "Mapper", "Service", "Repository", "Customizer", "Specification" };
        private readonly IConsoleWriter consoleWriter;

        public CSharpParserService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public EntityInfo ParseEntity(string fileName, string dtoCustomFieldName, string dtoCustomClassName)
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

            BaseNamespaceDeclarationSyntax namespaceSyntax = root
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
            var baseListNames = baseList?.Descendants<SimpleBaseTypeSyntax>().Select(x => x.ToString()).ToList();

            var genericNameSyntax = baseList?.Descendants<SimpleBaseTypeSyntax>()
                 .FirstOrDefault(node => !node.ToFullString().StartsWith("I"))? // Not interface
                 .Descendants<GenericNameSyntax>()
                 .FirstOrDefault();

            string baseType;
            string primaryKey;
            IEnumerable<string> keyNames = null;
            if (genericNameSyntax == null)
            {
                // No generic parameter -> Entity with Composite Keys
                baseType = baseList?.Descendants<SimpleBaseTypeSyntax>()
                    .SingleOrDefault(node => !node.ToFullString().StartsWith("I"))?
                    .Type?.ToString();
                primaryKey = null;
                keyNames = new List<string>() { "Id" };
            }
            else
            {
                // Normal entity
                baseType = genericNameSyntax.Identifier.ToString();
                primaryKey = genericNameSyntax.Descendants<TypeArgumentListSyntax>().Single().Arguments[0].ToString();
            }

            var properties = GetPropertyList(root.Descendants<PropertyDeclarationSyntax>().ToList(), dtoCustomFieldName);
            var classAnnotations = GetClassAnnotationList(classDeclarationSyntax.AttributeLists, dtoCustomClassName);

            var entityInfo = new EntityInfo(fileName, @namespace, className, baseType, primaryKey/*, relativeDirectory*/, classAnnotations, baseListNames);
            entityInfo.Properties.AddRange(properties);
            if (keyNames != null)
            {
                entityInfo.CompositeKeyName = $"{className}Key";
                var propList = keyNames.Select(k => properties.FirstOrDefault(prop => k == prop?.Name));
                if (propList != null)
                    entityInfo.CompositeKeys.AddRange(propList);
            }

            return entityInfo;
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



        public static async Task ParseSolution(string projectPath)
        {
            try
            {
                MSBuildLocator.RegisterDefaults();
                var workspace = MSBuildWorkspace.Create();
                var project = await workspace.OpenProjectAsync(projectPath);
                var compilation = await project.GetCompilationAsync();

                foreach (var diagnostic in compilation.GetDiagnostics()
                    .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error))
                {
                    Console.WriteLine(diagnostic);
                }
            }
            catch
            {
                throw;
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

        public List<PropertyInfo> GetPropertyList(List<PropertyDeclarationSyntax> propertyList, string dtoCustomAttributeName)
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
                            return new PropertyInfo(prop.Type.ToString(), prop.Identifier.ToString(), annotations);
                        }
                    }
                }
                return new PropertyInfo(prop.Type.ToString(), prop.Identifier.ToString(), null);
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

        public List<EntityInfo> GetDomainEntities(Domain.ModifyProject.Project project, CRUDSettings settings, IEnumerable<string> excludedPropertiesNames = null, IEnumerable<string> filteredEntityBaseTypes = null)
        {
            List<EntityInfo> entities = new();

            string entitiesFolder = $"{project.CompanyName}.{project.Name}.Domain";
            string projectDomainPath = Path.Combine(project.Folder, Constants.FolderDotNet, entitiesFolder);

            try
            {
                var files = new List<string>();
                if (Directory.Exists(projectDomainPath))
                {
                    var subFolders = Directory.GetDirectories(projectDomainPath).Where(x => !excludedEntitiesFolders.Contains(Path.GetFileName(x))).ToList();
                    foreach (var subFolder in subFolders)
                    {
                        var subFolderFiles = Directory.EnumerateFiles(subFolder, "*.cs", SearchOption.AllDirectories);
                        files.AddRange(subFolderFiles.Where(file => !excludedEntitiesFilesSuffixes.Any(suffix => Path.GetFileNameWithoutExtension(file).EndsWith(suffix))));
                    }
                }

                foreach (var file in files.OrderBy(x => Path.GetFileName(x)))
                {
                    try
                    {
                        var entityInfo = ParseEntity(file, settings.DtoCustomAttributeFieldName, settings.DtoCustomAttributeClassName);

                        if (filteredEntityBaseTypes != null && !entityInfo.BaseList.Any(x => filteredEntityBaseTypes.Any(y => x.StartsWith(y))))
                        {
                            continue;
                        }

                        if (excludedPropertiesNames != null)
                        {
                            entityInfo.Properties.RemoveAll(p => excludedPropertiesNames.Any(x => p.Name.Equals(x, StringComparison.InvariantCultureIgnoreCase)));
                        }

                        entities.Add(entityInfo);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }

            return entities;
        }

        public static void RegisterMSBuild(IConsoleWriter consoleWriter)
        {
            try
            {
                if (MSBuildLocator.IsRegistered)
                    return;

                var instances = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(x => x.Version).ToList();
                if (instances.Count > 0)
                {
                    MSBuildLocator.RegisterInstance(instances.First());
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
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error: Failed to register MSBuild : {ex.Message}", "red");
            }
        }

        public async Task FixUsings(string solutionPath, IEnumerable<string> filteredDocumentsPath = null, bool forceRestore = false)
        {
            consoleWriter.AddMessageLine("Start fix usings", "pink");

            try
            {
                using var workspace = MSBuildWorkspace.Create();
                if (workspace == null)
                {
                    consoleWriter.AddMessageLine("Error: Workspace could not be created.", "red");
                    return;
                }

                if (!await RestoreSolution(solutionPath, forceRestore))
                    return;

                consoleWriter.AddMessageLine("Opening solution...", "darkgray");
                var solution = await workspace.OpenSolutionAsync(solutionPath);

                if (solution == null)
                {
                    consoleWriter.AddMessageLine($"Error: Solution at path '{solutionPath}' could not be loaded.", "red");
                    return;
                }

                consoleWriter.AddMessageLine($"Solution loaded successfully", "lightgreen");

                foreach (var project in solution.Projects)
                {
                    try
                    {
                        consoleWriter.AddMessageLine($"Analyzing project {project.Name}...", "darkgray");

                        foreach (var document in project.Documents)
                        {
                            if(filteredDocumentsPath != null && !filteredDocumentsPath.Any(path => path.Equals(document.FilePath)))
                            {
                                continue;
                            }

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

                                var formattedRoot = Microsoft.CodeAnalysis.Formatting.Formatter.Format(updatedRoot, workspace);
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

        private async Task<bool> RestoreSolution(string solutionPath, bool forceRestore)
        {
            if (!forceRestore && IsSolutionRestored(solutionPath))
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
            var csprojFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);

            foreach (var proj in csprojFiles)
            {
                if (!IsProjectRestored(proj))
                    return false;
            }

            return true;
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
