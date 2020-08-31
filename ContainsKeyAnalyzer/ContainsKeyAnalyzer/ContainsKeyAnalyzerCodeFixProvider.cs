using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace ContainsKeyAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ContainsKeyAnalyzerCodeFixProvider)), Shared]
    public class ContainsKeyAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Replace with specialized method";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ContainsKeyAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            foreach(var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => ReplaceWithSpecializedContains(context.Document, declaration, c),
                        equivalenceKey: title),
                    diagnostic);
            }
        }

        private async Task<Solution> ReplaceWithSpecializedContains(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var correctMemberAccess = ((invocation.Expression as MemberAccessExpressionSyntax).Expression as MemberAccessExpressionSyntax).Expression; // This is the IDictionary<'2>
            var callAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, correctMemberAccess,

                ((invocation.Expression as MemberAccessExpressionSyntax).Expression as MemberAccessExpressionSyntax).Name.Identifier.ValueText == "Keys" ?
                SyntaxFactory.IdentifierName(
                       @"ContainsKey") : SyntaxFactory.IdentifierName(
                       @"ContainsValue")
                );
            var containsKeyOrValueCall =
                SyntaxFactory.InvocationExpression(callAccess, invocation.ArgumentList);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(invocation, containsKeyOrValueCall);
            var originalSolution = document.Project.Solution;
            var newSolution = originalSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
            return newSolution;
        }
    }
}
