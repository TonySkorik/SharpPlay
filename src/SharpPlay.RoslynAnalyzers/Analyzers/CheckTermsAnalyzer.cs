using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace SharpPlay.RoslynAnalyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CheckTermsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CheckTerms001";

    private const string Title = "Type name contains invalid term";
    private const string MessageFormat = "The term '{0}' is not allowed in a type name.";
    private const string Category = "Policy";

    private static DiagnosticDescriptor _rule =
        new(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            // Find the file with the invalid terms.
            ImmutableArray<AdditionalText> additionalFiles = compilationStartContext.Options.AdditionalFiles;
            AdditionalText termsFile = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals("Terms.txt"));

            if (termsFile != null)
            {
                HashSet<string> terms = new();

                // Read the file line-by-line to get the terms.
                SourceText fileText = termsFile.GetText(compilationStartContext.CancellationToken);
                foreach (TextLine line in fileText.Lines)
                {
                    terms.Add(line.ToString());
                }

                // Check every named type for the invalid terms.
                compilationStartContext.RegisterSymbolAction(symbolAnalysisContext =>
                {
                    INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol)symbolAnalysisContext.Symbol;
                    string symbolName = namedTypeSymbol.Name;

                    foreach (string term in terms)
                    {
                        if (symbolName.Contains(term))
                        {
                            symbolAnalysisContext.ReportDiagnostic(Diagnostic.Create(_rule, namedTypeSymbol.Locations[0], term));
                        }
                    }
                },
                SymbolKind.NamedType);
            }
        });
    }
}
