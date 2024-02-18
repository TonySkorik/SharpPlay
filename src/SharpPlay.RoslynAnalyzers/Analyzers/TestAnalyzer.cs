using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SharpPlay.RoslynAnalyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestAnalyzer : DiagnosticAnalyzer
{
    public const string DIAGNOSTIC_ID = "TestRoslynAnalyzerDiagnostics";

    // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
    // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
    private const string TITLE = "Type name contains lowercase letters";

    private const string MESSAGE_FORMAT = "Type name '{0}' contains lowercase letters";
    private const string DESCRIPTION = "Type names should be all uppercase.";

    private const string CATEGORY = "Naming";

    private static readonly DiagnosticDescriptor _rule = new(
        DIAGNOSTIC_ID,
        TITLE,
        MESSAGE_FORMAT,
        CATEGORY,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: DESCRIPTION);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        // Find just those named type symbols with names containing lowercase letters.
        if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
        {
            // For all such symbols, produce a diagnostic.
            var diagnostic = Diagnostic.Create(_rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
