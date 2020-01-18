using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using Analyzer.Unimplemented;

namespace Analyzer.Unimplemented.Test
{
    [TestClass]
    public class UnimplementedAnalyzerTests : CodeFixVerifier
    {


        //No diagnostics expected to show up
        [TestMethod]
        public void TestEmpty()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestValid_SingleFile_SameNamespace()
        {
            var test = @"
    namespace N
    {
        public interface IInterface 
        {
            int A { get; }
        }
    
        public class Implementation: IInterface 
        {
            public int A { get; } = 10;
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TestValid_MultiFile_SameNamespace()
        {
            string test1 = @"
namespace N
{
    public interface IInterface 
    {
        int A { get; }
    }
}",
                test2 = @"
namespace N
{
    public class Implementation: IInterface 
    {
        public int A { get; } = 10;
    }
}";
            VerifyCSharpDiagnostic(new[] { test1, test2 });
        }

        [TestMethod]
        public void TestValid_MultiFile_DifferentNamespace()
        {
            string test1 = @"
namespace N
{
    public interface IInterface 
    {
        int A { get; }
    }
}",
                test2 = @"
namespace M
{
    using N;
    public class Implementation: IInterface 
    {
        public int A { get; } = 10;
    }
}";
            VerifyCSharpDiagnostic(new[] { test1, test2 });
        }


        [TestMethod]
        public void TestValid_SingleFile_SeparateNamespaces()
        {
            var test = @"
    namespace N
    {
        namespace Abstractions 
        {
            public interface IInterface 
            {
                int A { get; }
            }
        }

        namespace Foo
        {
            using Abstractions;
    
            public class Implementation: IInterface 
            {
                public int A { get; } = 10;
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestInvalid_SingleFile_SameNamespace()
        {
            var test = @"
namespace N
{
    public interface IInterface
    {
        int A { get; }
    }

    namespace Abstractions
    {
        public interface IInterface
        {
            long B { get; }
        }


        public class Implementation : IInterface
        {
            public int A { get; } = 10;
            public long B { get; } = 456;
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "UnimplementedInterface",
                Message = String.Format("Interface '{0}.{1}' is not implemented", "N", "IInterface"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 4, 22)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void TestInvalid_SingleFile_FullyQualified()
        {
            var test = @"
namespace N
{
    public interface IInterface
    {
        int A { get; }
    }

    namespace Abstractions
    {
        public interface IInterface
        {
            long B { get; }
        }


        public class Implementation : N.IInterface
        {
            public int A { get; } = 10;
            public long B { get; } = 456;
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "UnimplementedInterface",
                Message = String.Format("Interface '{0}.{1}' is not implemented", "N.Abstractions", "IInterface"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 11, 26)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }



        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UnimplementedAnalyzer();
        }
    }
}
