using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TestHelper;

namespace ContainsKeyAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void EmptyText_NoDiagnostic()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void DictionaryType_SingleDiagnostic()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            Dictionary<int,int> dict = new Dictionary<int,int>();
            public void Test()
            {
                dict.Keys.Contains(1);
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "ContainsKeyAnalyzer",
                Message = String.Format("Dictionary call '{0}' can be specialized", "dict.Keys.Contains(1)"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 13, 17)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void IDictionary_NoDiagnostic_ContainsValueNotAvailable()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
        IDictionary<int,int> dict;
            public void Test()
            {
                dict.Values.Contains(1);
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IDictionary_SingleDiagnostic()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
        IDictionary<int,int> dict;
            public void Test()
            {
                dict.Keys.Contains(1);
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "ContainsKeyAnalyzer",
                Message = String.Format("Dictionary call '{0}' can be specialized", "dict.Keys.Contains(1)"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 13, 17)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }
        [TestMethod]
        public void IDictionary_BoundedExample_IReadOnly_SingleDiagnostic()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class TypeName
        {   private static bool IsRelevant<T>(T candidate, IReadOnlyDictionary<string, T> context)
{
    return context.Keys.Contains(candidate);
}
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "ContainsKeyAnalyzer",
                Message = String.Format("Dictionary call '{0}' can be specialized", "context.Keys.Contains(candidate)"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 11, 12)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void IDictionary_SingleCodeFix_ContainsKey()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
        IDictionary<int,int> dict;
            public void Test()
            {
                dict.Keys.Contains(1);
            }
        }
    }";

            var expected = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
        IDictionary<int,int> dict;
            public void Test()
            {
                dict.ContainsKey(1);
            }
        }
    }";

            VerifyCSharpFix(test, expected);
        }

        [TestMethod]
        public void IDictionary_SingleCodeFix_ContainsValue()
        {
            var test = @"
    using System.Collections.Generic;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            Dictionary<int,int> dict = new Dictionary<int,int>();
            public void Test()
            {
                dict.Values.Contains(1);
            }
        }
    }";

            var expected = @"
    using System.Collections.Generic;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            Dictionary<int,int> dict = new Dictionary<int,int>();
            public void Test()
            {
                dict.ContainsValue(1);
            }
        }
    }";

            VerifyCSharpFix(test, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ContainsKeyAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ContainsKeyAnalyzerAnalyzer();
        }
    }
}
