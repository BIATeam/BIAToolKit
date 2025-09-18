namespace BIA.ToolKit.Application.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis;
    using System.Collections;
    using BIA.ToolKit.Domain.ProjectAnalysis;

    public static class RoslynHelper
    {
        public static AttributeArgumentSyntax CreateAttributeArgument(string argName, object value)
        {
            return SyntaxFactory.AttributeArgument(ToExpression(value))
                                .WithNameEquals(
                                    SyntaxFactory.NameEquals(
                                        SyntaxFactory.IdentifierName(argName)));
        }

        // ---------------------------------------------------------------------
        // Helpers – conversion des valeurs vers des littéraux/expressions Roslyn
        // ---------------------------------------------------------------------
        private static ExpressionSyntax ToExpression(object value) => value switch
        {
            string s => SyntaxFactory.LiteralExpression(
                              SyntaxKind.StringLiteralExpression,
                              SyntaxFactory.Literal(s)),
            bool b => b
                            ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                            : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression),
            int i => SyntaxFactory.LiteralExpression(
                              SyntaxKind.NumericLiteralExpression,
                              SyntaxFactory.Literal(i)),
            long l => SyntaxFactory.LiteralExpression(
                              SyntaxKind.NumericLiteralExpression,
                              SyntaxFactory.Literal(l)),
            double d => SyntaxFactory.LiteralExpression(
                              SyntaxKind.NumericLiteralExpression,
                              SyntaxFactory.Literal(d)),
            Enum e => SyntaxFactory.MemberAccessExpression(
                              SyntaxKind.SimpleMemberAccessExpression,
                              SyntaxFactory.IdentifierName(e.GetType().Name),
                              SyntaxFactory.IdentifierName(e.ToString())),
            ExpressionSyntax es => es,
            _ => throw new NotSupportedException(
                              $"Type de valeur non géré : {value.GetType().FullName}")
        };

        public static IEnumerable<INamedTypeSymbol> GetAllNamedTypes(INamespaceSymbol ns)
        {
            foreach (var member in ns.GetMembers())
            {
                if (member is INamespaceSymbol childNs)
                {
                    foreach (var t in GetAllNamedTypes(childNs))
                        yield return t;
                }
                else if (member is INamedTypeSymbol nt)
                {
                    // Inclut types imbriqués
                    foreach (var nested in GetSelfAndNested(nt))
                        yield return nested;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetSelfAndNested(INamedTypeSymbol type)
        {
            yield return type;
            foreach (var m in type.GetTypeMembers())
            {
                foreach (var n in GetSelfAndNested(m))
                    yield return n;
            }
        }

        public static string Display(ITypeSymbol symbol) =>
            symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat
                .WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters)
                .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.ExpandNullable
                                         | SymbolDisplayMiscellaneousOptions.UseSpecialTypes));

        public static string Display(INamedTypeSymbol symbol) => Display((ITypeSymbol)symbol);

        public static AttributeInfo ToAttributeInfo(AttributeData a)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            var ctor = a.AttributeConstructor;
            // Constructor args (avec noms si disponibles)
            for (int i = 0; i < a.ConstructorArguments.Length; i++)
            {
                var key = ctor is not null && i < ctor.Parameters.Length
                    ? ctor.Parameters[i].Name
                    : $"ctorArg{i}";
                dict[key] = TypedConstantToString(a.ConstructorArguments[i]);
            }
            // Named args
            foreach (var (name, value) in a.NamedArguments)
            {
                dict[name] = TypedConstantToString(value);
            }
            return new AttributeInfo(
                Name: a.AttributeClass?.Name.Replace("Attribute", "") ?? "Attribute",
                Arguments: dict
            );
        }

        private static string TypedConstantToString(TypedConstant tc)
        {
            if (tc.IsNull) return "null";
            if (tc.Kind == TypedConstantKind.Array)
            {
                var items = tc.Values.Select(TypedConstantToString);
                return $"[{string.Join(", ", items)}]";
            }
            // Affichage formaté des types et enums
            return tc.Value switch
            {
                ITypeSymbol t => Display(t),
                _ => tc.Value?.ToString() ?? "null"
            };
        }

        public static InheritedTypeInfo ToInheritedTypeInfo(INamedTypeSymbol type)
        {
            var args = type.IsGenericType
                ? type.TypeArguments.Select(Display).ToList()
                : new List<string>();
            return new InheritedTypeInfo(
                DisplayName: Display(type),
                HasGenerics: type.IsGenericType,
                GenericArguments: args
            );
        }

        public static IEnumerable<IPropertySymbol> GetAllAccessiblePublicProperties(INamedTypeSymbol type)
        {
            // Propriétés publiques déclarées
            IEnumerable<IPropertySymbol> own = type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public || p.ExplicitInterfaceImplementations.Length > 0);

            // Propriétés publiques des bases
            IEnumerable<IPropertySymbol> bases = GetBaseTypes(type)
                .SelectMany(bt => bt.GetMembers().OfType<IPropertySymbol>()
                    .Where(p => p.DeclaredAccessibility == Accessibility.Public));

            // Propriétés des interfaces (explicites ou non)
            IEnumerable<IPropertySymbol> ifaces = type.AllInterfaces
                .SelectMany(i => i.GetMembers().OfType<IPropertySymbol>());

            // Union + déduplication par “signature lisible”
            var map = new Dictionary<string, (IPropertySymbol prop, int depth)>(StringComparer.Ordinal);

            void consider(IPropertySymbol p, int depth)
            {
                var key = $"{Display(p.Type)} {p.Name}";
                // Garder la plus dérivée (depth plus petit = plus proche du type courant)
                if (!map.TryGetValue(key, out var existing) || depth < existing.depth)
                    map[key] = (p, depth);
            }

            // depth 0 = type courant
            foreach (var p in own) consider(p, 0);
            int d = 1;
            foreach (var bt in GetBaseTypes(type))
            {
                foreach (var p in bt.GetMembers().OfType<IPropertySymbol>()
                         .Where(p => p.DeclaredAccessibility == Accessibility.Public))
                    consider(p, d);
                d++;
            }
            // interfaces : profondeur “infinie” pour ne jamais écraser implémentations concrètes
            foreach (var p in ifaces) consider(p, int.MaxValue / 2);

            return map.Values.Select(v => v.prop);
        }

        public static IEnumerable<INamedTypeSymbol> GetBaseTypes(INamedTypeSymbol type)
        {
            var current = type.BaseType;
            while (current is not null && current.SpecialType != SpecialType.System_Object)
            {
                yield return current;
                current = current.BaseType;
            }
        }
    }
}
