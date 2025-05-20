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

    public static class RoslynHelper
    {
        public static AttributeArgumentSyntax CreateAttributeArgument(
        string attributeName,
        IEnumerable<(string Name, object Value)> namedArgs)
        {
            // -----------------------------------------------------------------
            // 1. Convertit chaque (clé,valeur) en « Prop = Expression »
            // -----------------------------------------------------------------
            var assignmentsWithCommas = namedArgs
                .Select((kvp, idx) =>
                {
                    // PropName = <expr>
                    var assignExpr = SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(kvp.Name),
                        ToExpression(kvp.Value));

                    // Insère la virgule SAUF après le dernier
                    return idx < namedArgs.Count() - 1
                        ? new SyntaxNodeOrToken[]
                          { assignExpr, SyntaxFactory.Token(SyntaxKind.CommaToken) }
                        : new SyntaxNodeOrToken[] { assignExpr };
                })
                .SelectMany(x => x);

            // -----------------------------------------------------------------
            // 2. new AttributeName { … }
            // -----------------------------------------------------------------
            var objCreation = SyntaxFactory.ObjectCreationExpression(
                                  SyntaxFactory.IdentifierName(attributeName))
                             .WithInitializer(
                                 SyntaxFactory.InitializerExpression(
                                     SyntaxKind.ObjectInitializerExpression,
                                     SyntaxFactory.SeparatedList<ExpressionSyntax>(assignmentsWithCommas)));

            // -----------------------------------------------------------------
            // 3. Enveloppe dans l'argument d'attribut
            // -----------------------------------------------------------------
            return SyntaxFactory.AttributeArgument(objCreation);
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
    }
}
