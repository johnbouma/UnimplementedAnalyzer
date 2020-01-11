using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzer.Unimplemented
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnimplementedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UnimplementedInterface";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Implementations";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
            // context.RegisterCompilationAction();
        }


        private void AnalyzeSyntaxTree(Compilation compilation, SyntaxTree tree)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            var implGraph = new ImplementationGraph(semanticModel);
            var classDeclarations = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
        }

        private void ProcessInterfaceDeclarations(ImplementationGraph implGraph, SyntaxTree tree)
        {
            var declarations = tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            AddDeclarations(implGraph, declarations);
        }

        private void ProcessClassDeclarations(ImplementationGraph implGraph, SyntaxTree tree)
        {
            var declarations = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            AddDeclarations(implGraph, declarations);
        }

        private void AddDeclarations<T>(ImplementationGraph implGraph, IEnumerable<T> declarations) where T : TypeDeclarationSyntax
        {
            foreach (var decl in declarations)
            {
                implGraph.AddDeclaration(decl);
            }
        }


        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            syntaxTrees.Add(context.Tree);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Find just those named type symbols with names containing lowercase letters.
            if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
