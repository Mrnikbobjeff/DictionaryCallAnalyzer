using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ContainsKeyAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ContainsKeyAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ContainsKeyAnalyzer";
        static readonly string[] PropertyNames = new string[] { "Keys", "Values"};
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Performance";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var invocation = context.Node as InvocationExpressionSyntax;
            if ((invocation.Expression is MemberAccessExpressionSyntax memAccess) && !"Contains".Equals(memAccess.Name.Identifier.ValueText))
                return; //early return for perf
            if (invocation.ArgumentList.Arguments.Count != 1)
                return; //As stated in issue we are only interested in single argument calls
            var valueOrKeysExpression = ((invocation.Expression as MemberAccessExpressionSyntax)?.Expression as MemberAccessExpressionSyntax);
            if (!PropertyNames.Contains(valueOrKeysExpression.Name.Identifier.ValueText))
                return; //Not contains expression on .Keys or .Values
            var typeMemberAccess = context.SemanticModel.GetTypeInfo(((invocation.Expression as MemberAccessExpressionSyntax)?.Expression as MemberAccessExpressionSyntax)?.Expression).Type;
            if (typeMemberAccess is null)
                return; //when typing this may occur
            if (typeMemberAccess.AllInterfaces.Any(@interface => @interface.Name == "IDictionary" && @interface.Arity == 2) 
                || typeMemberAccess is INamedTypeSymbol namedType && namedType.Arity == 2 && (typeMemberAccess.Name.Equals("IDictionary") || typeMemberAccess.Name.Equals("IReadOnlyDictionary")))
            {
                if (valueOrKeysExpression.Name.Identifier.ValueText.Equals("Values")
                    && !typeMemberAccess.Name.Equals("Dictionary"))
                    return; //ContainsValue only available on Dictionary
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), invocation);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
