using System.Collections.Immutable;
using System.Text;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace SharpPlay.RoslynAnalyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CheckAdditionalFileAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CheckAppsettings001";

    private const string Title = "Type name contains invalid term";
    private const string MessageFormat = "The term '{0}' is not allowed in a type name.";
    private const string Category = "Policy";

    private static DiagnosticDescriptor Rule =
        new(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        // enable analyzer concurrent execution
        context.EnableConcurrentExecution();

        // configures generated code analysis
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze
            | GeneratedCodeAnalysisFlags.ReportDiagnostics
        );

        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            // Find the file with the invalid terms.
            ImmutableArray<AdditionalText> additionalFiles = compilationStartContext.Options.AdditionalFiles;

            AdditionalText termsFile = additionalFiles
                .FirstOrDefault(file => Path.GetExtension(file.Path)
                .Equals("json"));

            if (termsFile != null)
            {
                HashSet<string> terms = new();
                SourceText fileText = termsFile.GetText(compilationStartContext.CancellationToken);

                MemoryStream stream = new();
                using (StreamWriter writer = new(stream, Encoding.UTF8, 1024, true))
                {
                    fileText.Write(writer);
                }

                stream.Position = 0;

                // Read all the <Term> elements to get the terms.
                XDocument document = XDocument.Load(stream);
                foreach (XElement termElement in document.Descendants("Term"))
                {
                    terms.Add(termElement.Value);
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
                            symbolAnalysisContext.ReportDiagnostic(Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], term));
                        }
                    }
                },
                SymbolKind.NamedType);
            }
        });
    }
}