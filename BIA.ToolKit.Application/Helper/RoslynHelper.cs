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
    }
}
