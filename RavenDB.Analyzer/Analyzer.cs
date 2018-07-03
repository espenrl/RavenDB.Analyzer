using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RavenDB.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Analyzer : DiagnosticAnalyzer
    {
        private const string Category = "RavenDB";

        // RDB1001
        internal enum RDB1001InvocationType
        {
            Enumerable,
            Foreach
        }
        internal const string DiagnosticIdRDB1001 = "RDB1001";
        private static readonly LocalizableString TitleRDB1001 = new LocalizableResourceString(nameof(Resources.AnalyzerTitleRDB1001), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormatRDB1001 = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormatRDB1001), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString DescriptionRDB1001 = new LocalizableResourceString(nameof(Resources.AnalyzerDescriptionRDB1001), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor UseTakeRule = new DiagnosticDescriptor(DiagnosticIdRDB1001, TitleRDB1001, MessageFormatRDB1001, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: DescriptionRDB1001);

        // RDB1002
        private const string DiagnosticIdRDB1002 = "RDB1002";
        private static readonly LocalizableString TitleRDB1002 = new LocalizableResourceString(nameof(Resources.AnalyzerTitleRDB1002), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormatRDB1002 = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormatRDB1002), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString DescriptionRDB1002 = new LocalizableResourceString(nameof(Resources.AnalyzerDescriptionRDB1002), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor TakeWithlessThan1024Rule = new DiagnosticDescriptor(DiagnosticIdRDB1002, TitleRDB1002, MessageFormatRDB1002, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: DescriptionRDB1002);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UseTakeRule, TakeWithlessThan1024Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None); // skip auto generated code
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeForEachStatement, SyntaxKind.ForEachStatement);
        }

        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var okTrigger = false;
            var okIRavenQueryable = false;
            foreach (var ies in EnumerateCallchain((InvocationExpressionSyntax)context.Node, context.SemanticModel, context.CancellationToken))
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(ies, context.CancellationToken);

                // step 1: trigger on enumeration
                if (!okTrigger)
                {
                    var isTriggered = IsEnumerableMethod(nameof(Enumerable.ToList), context.SemanticModel, symbolInfo)
                                  || IsEnumerableMethod(nameof(Enumerable.ToArray), context.SemanticModel, symbolInfo)
                                  || IsEnumerableMethod(nameof(Enumerable.ToDictionary), context.SemanticModel, symbolInfo)
                                  || IsEnumerableMethod(nameof(Enumerable.AsEnumerable), context.SemanticModel, symbolInfo);

                    if (!isTriggered)
                    {
                        break; // not relevant, stop analyzing
                    }

                    okTrigger = true;
                    continue; // next iteration is step 2
                }

                // step 2: ... and only if invoked against IRavenQueryable type
                if (!okIRavenQueryable)
                {
                    if (!(symbolInfo.Symbol is IMethodSymbol methodSymbol) || !IsIRavenQueryableType(context.SemanticModel, methodSymbol.ReturnType))
                    {
                        break; // not relevant, stop analyzing
                    }

                    okIRavenQueryable = true;
                }

                // step 3: analyze rest of chain (all the left iterations)
                if (IsRavenDocumentSessionMethod("Query", context.SemanticModel, symbolInfo))
                {
                    // Take() not found at this point - create diagnostic
                    var diagnostic = Diagnostic.Create(UseTakeRule, context.Node.GetLocation(),
                        ImmutableDictionary<string, string>.Empty.Add(nameof(RDB1001InvocationType), nameof(RDB1001InvocationType.Enumerable)));
                    context.ReportDiagnostic(diagnostic);
                    break; // error found, stop analyzing
                }

                if (IsRavenQueryableExtensionsMethod("Take", context.SemanticModel, symbolInfo))
                {
                    // verify count: n <= 1024
                    // NOTE: only verifies constants - does not track variables / fields
                    if (ies.ArgumentList.Arguments.Count == 1)
                    {
                        var argumentSyntax = ies.ArgumentList.Arguments[0];
                        var optionalConstantValue = context.SemanticModel.GetConstantValue(argumentSyntax.Expression);

                        if (optionalConstantValue.HasValue
                            && optionalConstantValue.Value is int n
                            && n > 1024)
                        {
                            var diagnostic = Diagnostic.Create(TakeWithlessThan1024Rule, argumentSyntax.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                    }

                    break; // Take() found, stop analyzing
                }
            }
        }

        private static void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
        {
            var fss = (ForEachStatementSyntax)context.Node;

            if (!(fss.Expression is InvocationExpressionSyntax iesRoot))
            {
                return;
            }

            var symbolInfoRoot = context.SemanticModel.GetSymbolInfo(iesRoot, context.CancellationToken);

            if (!(symbolInfoRoot.Symbol is IMethodSymbol methodSymbol
                && IsIRavenQueryableType(context.SemanticModel, methodSymbol.ReturnType)))
            {
                return;
            }

            foreach (var ies in EnumerateCallchain(iesRoot, context.SemanticModel, context.CancellationToken))
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(ies, context.CancellationToken);

                if (IsRavenDocumentSessionMethod("Query", context.SemanticModel, symbolInfo))
                {
                    // Take() not found at this point - create diagnostic
                    var diagnostic = Diagnostic.Create(UseTakeRule, iesRoot.GetLocation(),
                        ImmutableDictionary<string, string>.Empty.Add(nameof(RDB1001InvocationType), nameof(RDB1001InvocationType.Foreach)));
                    context.ReportDiagnostic(diagnostic);
                    break; // error found, stop analyzing
                }

                if (IsRavenQueryableExtensionsMethod("Take", context.SemanticModel, symbolInfo))
                {
                    // verify count: n <= 1024
                    // NOTE: only verifies constants - does not track variables / fields
                    if (ies.ArgumentList.Arguments.Count == 1)
                    {
                        var argumentSyntax = ies.ArgumentList.Arguments[0];
                        var optionalConstantValue = context.SemanticModel.GetConstantValue(argumentSyntax.Expression);

                        if (optionalConstantValue.HasValue
                            && optionalConstantValue.Value is int n
                            && n > 1024)
                        {
                            var diagnostic = Diagnostic.Create(TakeWithlessThan1024Rule, argumentSyntax.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                    }

                    break; // Take() found, stop analyzing
                }
            }
        }

        private static IEnumerable<InvocationExpressionSyntax> EnumerateCallchain(InvocationExpressionSyntax iesRoot, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var iesCurrent = iesRoot;

            while (iesCurrent?.Expression is MemberAccessExpressionSyntax maesCurrent)
            {
                yield return iesCurrent;

                switch (maesCurrent.Expression)
                {
                    // The target is a simple identifier, the code being analysed is of the form
                    // "command.ExecuteReader()" and memberAccess.Expression is the "command"
                    // node
                    // NOTE: track local variable, but nothing else (method call, field variable)
                    case IdentifierNameSyntax ins when semanticModel.GetSymbolInfo(ins, cancellationToken).Symbol is ILocalSymbol localSymbol:
                        var variableDeclaration = localSymbol
                            .DeclaringSyntaxReferences
                            .Select(sr => sr.GetSyntax(cancellationToken))
                            .OfType<VariableDeclaratorSyntax>()
                            .FirstOrDefault();

                        if (variableDeclaration?.Initializer?.Value is InvocationExpressionSyntax ies)
                        {
                            iesCurrent = ies;
                        }
                        else
                        {
                            iesCurrent = null;
                        }
                        break;
                    case InvocationExpressionSyntax ies2:
                        // The target is another invocation, the code being analysed is of the form
                        // "GetCommand().ExecuteReader()" and memberAccess.Expression is the
                        // "GetCommand()" node

                        iesCurrent = ies2;
                        break;
                    default:
                        iesCurrent = null;
                        break;
                }
            }
        }

        private static bool IsIRavenQueryableType(SemanticModel semanticModel, ITypeSymbol symbolInfo)
        {
            var typeIRavenQueryable = semanticModel.Compilation
                .GetTypeByMetadataName("Raven.Client.Linq.IRavenQueryable`1");

            if (typeIRavenQueryable == null)
            {
                return false;
            }

            return (symbolInfo as INamedTypeSymbol)?.ConstructedFrom?.Equals(typeIRavenQueryable) ?? false;
        }

        private static bool IsRavenDocumentSessionMethod(string methodName, SemanticModel semanticModel, SymbolInfo symbolInfo)
        {
            var typeIDocumentSession = semanticModel.Compilation
                .GetTypeByMetadataName("Raven.Client.IDocumentSession");

            if (typeIDocumentSession == null || !(symbolInfo.Symbol is IMethodSymbol ms) || ms.ConstructedFrom == null)
            {
                return false;
            }

            return typeIDocumentSession
                .GetMembers(methodName)
                .OfType<IMethodSymbol>()
                .Any(methodSymbol => methodSymbol.Equals(ms.ConstructedFrom));
        }

        private static bool IsRavenQueryableExtensionsMethod(string methodName, SemanticModel semanticModel, SymbolInfo symbolInfo)
        {
            var typeRavenQueryableExtensions = semanticModel.Compilation
                .GetTypeByMetadataName("Raven.Client.Linq.RavenQueryableExtensions");

            if (typeRavenQueryableExtensions == null || !(symbolInfo.Symbol is IMethodSymbol ms) || ms.ConstructedFrom == null)
            {
                return false;
            }

            return typeRavenQueryableExtensions
                .GetMembers(methodName)
                .OfType<IMethodSymbol>()
                .Any(methodSymbol => ms.GetConstructedReducedFrom()?.ConstructedFrom.Equals(methodSymbol) ?? false);
        }

        private static bool IsEnumerableMethod(string methodName, SemanticModel semanticModel, SymbolInfo symbolInfo)
        {
            if (!(symbolInfo.Symbol is IMethodSymbol ms))
            {
                return false;
            }

            return semanticModel.Compilation
                .GetTypeByMetadataName(typeof(Enumerable).FullName)
                .GetMembers(methodName)
                .OfType<IMethodSymbol>()
                .Any(methodSymbol => ms.GetConstructedReducedFrom()?.ConstructedFrom.Equals(methodSymbol) ?? false);
        }
    }
}
