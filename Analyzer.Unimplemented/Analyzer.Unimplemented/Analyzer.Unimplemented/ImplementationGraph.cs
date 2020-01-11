using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer.Unimplemented
{
    /// <summary>
    /// Stores Interface and Class Declaration Syntax Nodes and their corresponding implementors
    ///   TODO: Will allow querying of
    ///   TODO:  Initially: InterfaceDeclarationsWithoutImplementations();
    /// </summary>
    internal class ImplementationGraph
    {
        // Graph of interface -> implementors / inheritors
        private readonly Dictionary<InterfaceDeclarationSyntax, List<TypeDeclarationSyntax>> _interfaceImplementations
            = new Dictionary<InterfaceDeclarationSyntax, List<TypeDeclarationSyntax>>();

        // The semantic model used to resolve references in the syntax tree
        private readonly SemanticModel _semanticModel;

        internal ImplementationGraph(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="decl">The <see cref="TypeDeclarationSyntax" /> node to be processed.</param>
        internal void AddDeclaration(TypeDeclarationSyntax decl)
        {
            switch (decl)
            {
                // We search class declarations for implementations
                case ClassDeclarationSyntax classDecl:
                    AddClass(classDecl);
                    break;
                // We check for inheritance and create new nodes
                case InterfaceDeclarationSyntax interfaceDecl:
                    AddInterface(interfaceDecl);
                    break;
                //Default processing for unknown type declaration nodes
                default:
                    AddOtherTypeDeclarationSyntax(decl);
                    break;
            }
        }

        private void AddClass(ClassDeclarationSyntax classDecl)
        {

        }

        private void AddInterfaceAndImplementation(InterfaceDeclarationSyntax interfaceDecl,
            TypeDeclarationSyntax implDecl)
        {
            if (!_interfaceImplementations.ContainsKey(interfaceDecl))
            {
                _interfaceImplementations.Add(interfaceDecl, new List<TypeDeclarationSyntax>());
            }

            if (!_interfaceImplementations[interfaceDecl].Contains(implDecl))
            {
                _interfaceImplementations[interfaceDecl].Add(implDecl);
            }
        }
        
        private void AddInterface(InterfaceDeclarationSyntax interfaceDecl)
        {

        }

        //Separates the processing of a TypeDeclarationSyntax node's
        // BaseList Types if they exist
        private void ProcessTypeDeclarationCommon(TypeDeclarationSyntax syntax)
        {
            if (syntax.BaseList != null)
            {
                foreach (var item in syntax.BaseList.Types)
                {
                    //TODO: Need a way to traverse from the BaseTypeSyntax node to its declaration
                }
            }
        }

        private void AddOtherTypeDeclarationSyntax(TypeDeclarationSyntax typeDecl)
        {

        }
    }
}