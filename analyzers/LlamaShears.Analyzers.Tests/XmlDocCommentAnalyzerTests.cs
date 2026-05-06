using System.Linq;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class XmlDocCommentAnalyzerTests
{
    [Test]
    public async Task ConcreteClassWithoutDocDoesNotReport()
    {
        const string source = "namespace N; public class Foo { }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task ConcreteClassWithDocReportsLS0008Warning()
    {
        const string source =
            """
            namespace N;

            /// <summary>This is Foo.</summary>
            public class Foo { }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.XmlDocOnConcreteType);
        await Assert.That(diagnostics[0].DefaultSeverity).IsEqualTo(DiagnosticSeverity.Warning);
        await Assert.That(diagnostics[0].GetMessage()).Contains("Foo");
    }

    [Test]
    public async Task RecordWithDocReportsLS0008()
    {
        const string source =
            """
            namespace N;

            /// <summary>A record.</summary>
            public sealed record Foo(int Bar);
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.XmlDocOnConcreteType);
    }

    [Test]
    public async Task StructWithDocReportsLS0008()
    {
        const string source =
            """
            namespace N;

            /// <summary>A struct.</summary>
            public struct Foo { }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.XmlDocOnConcreteType);
    }

    [Test]
    public async Task EnumWithDocReportsLS0008()
    {
        const string source =
            """
            namespace N;

            /// <summary>Kinds.</summary>
            public enum Kind { A, B }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.XmlDocOnConcreteType);
    }

    [Test]
    public async Task ConcreteMethodWithDocReportsLS0008()
    {
        const string source =
            """
            namespace N;

            public class Foo
            {
                /// <summary>Does the thing.</summary>
                public void Bar() { }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.XmlDocOnConcreteType);
        await Assert.That(diagnostics[0].GetMessage()).Contains("Bar");
    }

    [Test]
    public async Task ConcreteMemberWithInheritDocOnlyDoesNotReport()
    {
        const string source =
            """
            namespace N;

            /// <summary>Foo.</summary>
            public interface IFoo
            {
                /// <summary>Bar.</summary>
                void Bar();
            }

            public class Foo : IFoo
            {
                /// <inheritdoc/>
                public void Bar() { }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task ConcreteMemberWithInheritDocFullElementFormDoesNotReport()
    {
        const string source =
            """
            namespace N;

            /// <summary>Foo.</summary>
            public interface IFoo
            {
                /// <summary>Bar.</summary>
                void Bar();
            }

            public class Foo : IFoo
            {
                /// <inheritdoc></inheritdoc>
                public void Bar() { }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task PublicInterfaceWithoutDocReportsLS0009Error()
    {
        const string source =
            """
            namespace N;

            public interface IFoo
            {
                /// <summary>Bar.</summary>
                void Bar();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.PublicInterfaceMissingXmlDoc);
        await Assert.That(diagnostics[0].DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostics[0].GetMessage()).Contains("IFoo");
    }

    [Test]
    public async Task PublicInterfaceMemberWithoutDocReportsLS0011Error()
    {
        const string source =
            """
            namespace N;

            /// <summary>Foo interface.</summary>
            public interface IFoo
            {
                void Bar();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.PublicInterfaceMemberMissingXmlDoc);
        await Assert.That(diagnostics[0].DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostics[0].GetMessage()).Contains("Bar");
    }

    [Test]
    public async Task NonPublicInterfaceMemberWithoutDocReportsLS0012Warning()
    {
        const string source =
            """
            namespace N;

            /// <summary>Foo.</summary>
            internal interface IFoo
            {
                void Bar();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.NonPublicInterfaceMemberMissingXmlDoc);
        await Assert.That(diagnostics[0].DefaultSeverity).IsEqualTo(DiagnosticSeverity.Warning);
        await Assert.That(diagnostics[0].GetMessage()).Contains("Bar");
    }

    [Test]
    public async Task InternalInterfaceWithoutDocReportsLS0010Warning()
    {
        const string source =
            """
            namespace N;

            internal interface IFoo
            {
                /// <summary>Bar.</summary>
                void Bar();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.NonPublicInterfaceMissingXmlDoc);
        await Assert.That(diagnostics[0].DefaultSeverity).IsEqualTo(DiagnosticSeverity.Warning);
    }

    [Test]
    public async Task FullyDocumentedPublicInterfaceDoesNotReport()
    {
        const string source =
            """
            namespace N;

            /// <summary>Foo.</summary>
            public interface IFoo
            {
                /// <summary>Bar.</summary>
                void Bar();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task PublicInterfaceMissingTypeAndMemberDocsReportsBothIds()
    {
        const string source =
            """
            namespace N;

            public interface IFoo
            {
                void Bar();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(2);
        await Assert.That(diagnostics.Any(d => d.Id == DiagnosticIds.PublicInterfaceMissingXmlDoc)).IsTrue();
        await Assert.That(diagnostics.Any(d => d.Id == DiagnosticIds.PublicInterfaceMemberMissingXmlDoc)).IsTrue();
    }

    [Test]
    public async Task DocumentationModeNotDiagnoseReportsLS0013()
    {
        const string source = "namespace N; public class Foo { }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source,
            DocumentationMode.Parse);

        await Assert.That(diagnostics.Any(d => d.Id == DiagnosticIds.DocumentationModeNotDiagnose)).IsTrue();
        var compileError = diagnostics.First(d => d.Id == DiagnosticIds.DocumentationModeNotDiagnose);
        await Assert.That(compileError.DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(compileError.Location).IsEqualTo(Location.None);
    }

    [Test]
    public async Task DocumentationModeDiagnoseDoesNotReportLS0013()
    {
        const string source = "namespace N; public class Foo { }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source,
            DocumentationMode.Diagnose);

        await Assert.That(diagnostics.Any(d => d.Id == DiagnosticIds.DocumentationModeNotDiagnose)).IsFalse();
    }

    [Test]
    public async Task IAgentShapedSourceDoesNotReport()
    {
        const string source =
            """
            using System;

            namespace N;

            /// <summary>
            /// An agent: an autonomous component that periodically heartbeats and
            /// communicates via input and output channels rather than returning
            /// data to a caller. Inputs are accepted from any of
            /// <see cref="InputChannels"/> and outputs are sent to all of
            /// <see cref="OutputChannels"/>; both append to <see cref="Context"/>.
            /// <para>
            /// An agent owns its own heartbeat: it subscribes to the system tick
            /// at construction and decides per tick whether to fire. <see cref="IDisposable.Dispose"/>
            /// tears down that subscription and any other lifetime-bound state.
            /// </para>
            /// </summary>
            public interface IAgent : IDisposable
            {
                /// <summary>Last heartbeat.</summary>
                DateTimeOffset LastHeartbeatAt { get; }

                /// <summary>Period.</summary>
                TimeSpan HeartbeatPeriod { get; }

                /// <summary>Enabled.</summary>
                bool HeartbeatEnabled { get; }

                /// <summary>Context.</summary>
                System.Collections.Generic.IReadOnlyList<int> Context { get; }

                /// <summary>Inputs.</summary>
                System.Collections.Generic.IReadOnlyList<int> InputChannels { get; }

                /// <summary>Outputs.</summary>
                System.Collections.Generic.IReadOnlyList<int> OutputChannels { get; }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task FileScopedNamespaceWithUsingAndMultiLineSummaryDoesNotReport()
    {
        const string source =
            """
            using System;

            namespace N;

            /// <summary>
            /// Multi-line summary like real code.
            /// </summary>
            public interface IFoo : IDisposable
            {
                /// <summary>Bar.</summary>
                void Bar();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new XmlDocCommentAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task XmlDocOnConcreteIsConfigurable()
    {
        var analyzer = new XmlDocCommentAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics
            .First(d => d.Id == DiagnosticIds.XmlDocOnConcreteType);

        await Assert.That(descriptor.CustomTags).DoesNotContain("NotConfigurable");
    }

    [Test]
    public async Task NonPublicInterfaceMissingDocIsConfigurable()
    {
        var analyzer = new XmlDocCommentAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics
            .First(d => d.Id == DiagnosticIds.NonPublicInterfaceMissingXmlDoc);

        await Assert.That(descriptor.CustomTags).DoesNotContain("NotConfigurable");
    }
}
