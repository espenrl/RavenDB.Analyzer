using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RavenDB.Analyzer.Test
{
    [TestClass]
    public class AnalyzerUnitTests : CodeFixVerifier
    {

        [TestMethod]
        public void NoDiagnosticOnEmptySource()
        {
            const string test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void WhenUsingFluentAndLinebreakInsertTakeAtRightPlace()
        {
            // NOTE: inserting Take() at its own line proves to be difficult - Rolsyn formatter does not handle this case as of now (aug 2018).

            var test = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session)
                                        => session
                                            .Query<object>()
                                            .ToList();
                            }
                        }";

            var expected = new DiagnosticResult
            {
                Id = "RDB1001",
                Message = "Missing use of Take() when using RavenDB query API",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 11, 44)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var testWithFix = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session)
                                        => session
                                            .Query<object>().Take(1024)
                                            .ToList();
                            }
                        }";

            VerifyCSharpFix(test, testWithFix);
        }

        [TestMethod]
        public void ForeachWithMissingTake()
        {
            var test = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session)
                                {
                                    foreach (var obj in session.Query<object>()) {}
                                }
                            }
                        }";

            var expected = new DiagnosticResult
            {
                Id = "RDB1001",
                Message = "Missing use of Take() when using RavenDB query API",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 12, 57)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var testWithFix = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session)
                                {
                                    foreach (var obj in session.Query<object>().Take(1024)) {}
                                }
                            }
                        }";

            VerifyCSharpFix(test, testWithFix);
        }

        [TestMethod]
        public void ToListWithMissingTake()
        {
            var test = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session) => session.Query<object>().ToList();
                            }
                        }";

            var expected = new DiagnosticResult
            {
                Id = "RDB1001",
                Message = "Missing use of Take() when using RavenDB query API",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 76)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var testWithFix = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session) => session.Query<object>().Take(1024).ToList();
                            }
                        }";

            VerifyCSharpFix(test, testWithFix);
        }

        [TestMethod]
        public void ToArrayWithMissingTake()
        {
            var test = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session) => session.Query<object>().ToArray();
                            }
                        }";

            var expected = new DiagnosticResult
            {
                Id = "RDB1001",
                Message = "Missing use of Take() when using RavenDB query API",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 10, 76)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var testWithFix = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session) => session.Query<object>().Take(1024).ToArray();
                            }
                        }";

            VerifyCSharpFix(test, testWithFix);
        }

        [TestMethod]
        public void ToDictionaryWithMissingTake()
        {
            var test = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session) => session.Query<object>().ToDictionary(o => 1);
                            }
                        }";

            var expected = new DiagnosticResult
            {
                Id = "RDB1001",
                Message = "Missing use of Take() when using RavenDB query API",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 10, 76)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var testWithFix = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session) => session.Query<object>().Take(1024).ToDictionary(o => 1);
                            }
                        }";

            VerifyCSharpFix(test, testWithFix);
        }

        [TestMethod]
        public void AsEnumerableWithMissingTake()
        {
            var test = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session) => session.Query<object>().AsEnumerable();
                            }
                        }";

            var expected = new DiagnosticResult
            {
                Id = "RDB1001",
                Message = "Missing use of Take() when using RavenDB query API",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 10, 76)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var testWithFix = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session) => session.Query<object>().Take(1024).AsEnumerable();
                            }
                        }";

            VerifyCSharpFix(test, testWithFix);
        }

        [TestMethod]
        public void TrackLocalVariables()
        {
            var test = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session)
                                {
                                    var a = session.Query<object>();
                                    var b = a.ToList();
                                }
                            }
                        }";

            var expected = new DiagnosticResult
            {
                Id = "RDB1001",
                Message = "Missing use of Take() when using RavenDB query API",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 13, 45)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var testWithFix = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session)
                                {
                                    var a = session.Query<object>();
                                    var b = a.Take(1024).ToList();
                                }
                            }
                        }";

            VerifyCSharpFix(test, testWithFix);
        }

        [TestMethod]
        public void WithTakeShouldGiveNoDiagnostic()
        {
            var test = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session) => session.Query<object>().Take(1024).AsEnumerable();
                            }
                        }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TakeWithCountAbove1024NotAllowed()
        {
            var test = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session) => session.Query<object>().Take(1025).AsEnumerable();
                            }
                        }";

            var expected = new DiagnosticResult
            {
                Id = "RDB1002",
                Message = "Take() with n > 1024 when using RavenDB query API",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 10, 105)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void DoNotTriggerOnIEnumerable()
        {
            var test = @"
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session) => Enumerable.Empty<int>().ToList();
                            }
                        }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void DoNotTriggerOnIEnumerableAfterTakeWithIRavenQueryable()
        {
            var test = @"
                        using Raven.Client;
                        using Raven.Client.Linq;
                        using System.Linq;

                        namespace ConsoleApplication1
                        {
                            class RavenDB
                            {   
                                void TestFunc(IDocumentSession session)
                                    => session.Query<object>()
                                              .Where(o => o == null)
                                              .Take(500)
                                              .ToList()
                                              .AsEnumerable()
                                              .ToArray()
                                              .ForEach(o => {});
                            }
                        }";

            VerifyCSharpDiagnostic(test);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new AnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new Analyzer();
        }
    }
}
