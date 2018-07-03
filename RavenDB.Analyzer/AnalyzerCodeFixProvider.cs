using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace RavenDB.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerCodeFixProvider)), Shared]
    public class AnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Add Take()";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Analyzer.DiagnosticIdRDB1001);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            // NOTE: not expecting more than one diagnostic per span.
            //       Diagnostics: All the diagnostics in this collection have the same span
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            if (diagnostic.Id == Analyzer.DiagnosticIdRDB1001)
            {
                if (diagnostic.Properties[nameof(Analyzer.RDB1001InvocationType)] == nameof(Analyzer.RDB1001InvocationType.Enumerable))
                {
                    var ies = root.FindToken(diagnosticSpan.End).Parent.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();

                    if (ies != null)
                    {
                        var oldNode = ((MemberAccessExpressionSyntax)ies.Expression).Expression; // take inner node - not ToArray(), ToList() etc.

                        // Register a code action that will invoke the fix.
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: Title,
                                createChangedDocument: c => InsertTakeInvocationAsync(context.Document, oldNode, c),
                                equivalenceKey: Title),
                            diagnostic);
                    }
                }
                else if (diagnostic.Properties[nameof(Analyzer.RDB1001InvocationType)] == nameof(Analyzer.RDB1001InvocationType.Foreach))
                {
                    var ies = root.FindToken(diagnosticSpan.End).Parent.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();

                    if (ies != null)
                    {
                        // Register a code action that will invoke the fix.
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: Title,
                                createChangedDocument: c => InsertTakeInvocationAsync(context.Document, ies, c),
                                equivalenceKey: Title),
                            diagnostic);
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static async Task<Document> InsertTakeInvocationAsync(Document document, ExpressionSyntax oldNode, CancellationToken cancellationToken)
        {
            var newNode = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        oldNode.WithTrailingTrivia(SyntaxTriviaList.Empty), // formatting works best if a dummy node is added (SyntaxTriviaList.Empty) - EOL case
                        IdentifierName("Take")))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(1024))))));

            // add EOL to new node if old node had EOL
            var oldNodeHasEndOfLineTrivia = oldNode.GetTrailingTrivia().Any(st => st.IsKind(SyntaxKind.EndOfLineTrivia));
            newNode = oldNodeHasEndOfLineTrivia
                ? newNode.WithTrailingTrivia(EndOfLine(Environment.NewLine)).WithAdditionalAnnotations(Formatter.Annotation)
                : newNode;

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(oldNode, newNode);
            var newRootFormatted = Formatter.Format(newRoot, Formatter.Annotation, document.Project.Solution.Workspace);
            var newDocument = document.WithSyntaxRoot(newRootFormatted);
            return newDocument;
        }
    }
}
