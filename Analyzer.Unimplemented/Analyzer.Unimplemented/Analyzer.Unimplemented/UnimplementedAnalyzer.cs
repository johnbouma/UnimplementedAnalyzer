using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzer.Unimplemented
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnimplementedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UnimplementedInterface";

        private const string Category = "Implementations";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle),
            Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString MessageFormat =
            new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager,
                typeof(Resources));

        private static readonly LocalizableString Description =
            new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager,
                typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            var interfaceMap = new ConcurrentDictionary<INamedTypeSymbol, bool>();
            var compilationStart = BuildCompilationStartHandler(interfaceMap);
            context.RegisterCompilationStartAction(compilationStart);
        }

        /// <summary>
        /// Create the action that processes on compilation start,
        ///     allows the dict to be passed down.
        /// </summary>
        /// <param name="dict">The interface-is implemented map</param>
        /// <returns>an action on the compilation start context</returns>
        private Action<CompilationStartAnalysisContext> BuildCompilationStartHandler(
            ConcurrentDictionary<INamedTypeSymbol, bool> dict)
        {
            var symbolAnalyzer = BuildSymbolAnalyzer(dict);
            var compilationEndAction = BuildCompilationEndAction(dict);
            return context =>
            {
                context.RegisterSymbolAction(symbolAnalyzer, SymbolKind.NamedType);
                context.RegisterCompilationEndAction(compilationEndAction);
            };
        }

        private Action<CompilationAnalysisContext> BuildCompilationEndAction(
            ConcurrentDictionary<INamedTypeSymbol, bool> dict)
        {
            return context =>
            {
                foreach (var symKvp in dict)
                    if (symKvp.Value == false && symKvp.Key.Locations.Any())
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Rule,
                                symKvp.Key.Locations.First(),
                                DiagnosticSeverity.Warning,
                                symKvp.Key.Locations.Skip(1),
                                ImmutableDictionary<string, string>.Empty,
                                $"{BuildNamespaceString(symKvp.Key.ContainingNamespace)}{symKvp.Key.Name}"));
            };
        }

        private static string BuildNamespaceString(INamespaceSymbol @namespace)
        {
            if (@namespace == null || @namespace.IsGlobalNamespace) return string.Empty;

            var sb = new StringBuilder();
            sb.Append(BuildNamespaceString(@namespace.ContainingNamespace));
            sb.Append(@namespace.Name);
            sb.Append('.');
            return sb.ToString();
        }

        private Action<SymbolAnalysisContext> BuildSymbolAnalyzer(ConcurrentDictionary<INamedTypeSymbol, bool> dict)
        {
            var classAnalyzer = BuildClassAnalyzer(dict);
            var interfaceAnalyzer = BuildInterfaceAnalyzer(dict);
            return context =>
            {
                if (context.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Interface } @interface)
                    interfaceAnalyzer(@interface);

                if (context.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Class } @class) classAnalyzer(@class);
            };
        }

        private Action<INamedTypeSymbol> BuildInterfaceAnalyzer(ConcurrentDictionary<INamedTypeSymbol, bool> dict)
        {
            return sym =>
            {
                if (!dict.ContainsKey(sym))
                    if (!dict.TryAdd(sym, false))
                        throw new InvalidOperationException("Could not add symbol to dictionary");
            };
        }

        private Action<INamedTypeSymbol> BuildClassAnalyzer(ConcurrentDictionary<INamedTypeSymbol, bool> dict)
        {
            return sym =>
            {
                if (sym.TypeKind == TypeKind.Class) ProcessClassInterfaces(sym, dict);
            };
        }


        private void ProcessClassInterfaces(INamedTypeSymbol sym, ConcurrentDictionary<INamedTypeSymbol, bool> dict)
        {
            if (sym.TypeKind == TypeKind.Class && sym.AllInterfaces != null && sym.AllInterfaces.Any())
            {
                var toggler = BuildInterfaceToggle(dict);
                foreach (var @interface in sym.AllInterfaces) toggler(@interface);
            }
        }


        private Action<INamedTypeSymbol> BuildInterfaceToggle(ConcurrentDictionary<INamedTypeSymbol, bool> dict)
        {
            return sym => { dict.AddOrUpdate(sym, _ => true, (_, __) => true); };
        }
    }
}