using System.Collections.Immutable;
using System.Threading;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class XmlDocCommentAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [
            Descriptors.XmlDocOnConcreteType,
            Descriptors.PublicInterfaceMissingXmlDoc,
            Descriptors.NonPublicInterfaceMissingXmlDoc,
            Descriptors.PublicInterfaceMemberMissingXmlDoc,
            Descriptors.NonPublicInterfaceMemberMissingXmlDoc,
            Descriptors.DocumentationModeNotDiagnose,
        ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
        context.RegisterSyntaxNodeAction(
            AnalyzeTypeDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.DelegateDeclaration);
        context.RegisterSyntaxNodeAction(
            AnalyzeMemberDeclaration,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.PropertyDeclaration,
            SyntaxKind.FieldDeclaration,
            SyntaxKind.EventDeclaration,
            SyntaxKind.EventFieldDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.ConversionOperatorDeclaration,
            SyntaxKind.IndexerDeclaration,
            SyntaxKind.EnumMemberDeclaration);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        foreach (var tree in context.Compilation.SyntaxTrees)
        {
            if (tree.Options is not CSharpParseOptions options)
            {
                continue;
            }
            if (options.DocumentationMode == DocumentationMode.Diagnose)
            {
                continue;
            }
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.DocumentationModeNotDiagnose,
                Location.None));
            return;
        }
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;
        var (location, name) = GetTypeIdentifier(node);
        if (location is null)
            return;

        var hasDoc = TryGetDocComment(node, out var doc);

        if (!hasDoc && node is InterfaceDeclarationSyntax interfaceDecl)
            ReportInterfaceMissingDoc(context, interfaceDecl, location, name);
        if (node is InterfaceDeclarationSyntax)
            return;
        if (!hasDoc)
            return;
        if (IsInheritDocOnly(doc!))
            return;
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.XmlDocOnConcreteType,
            location,
            name));
    }

    private static void AnalyzeMemberDeclaration(SyntaxNodeAnalysisContext context)
    {
        var member = context.Node;
        if (member.Parent is not BaseTypeDeclarationSyntax parentType)
        {
            return;
        }

        var hasDoc = TryGetDocComment(member, out var doc);
        var (location, name) = GetMemberIdentifier(member);
        if (location is null)
        {
            return;
        }

        if (parentType is InterfaceDeclarationSyntax interfaceDecl)
        {
            if (hasDoc)
            {
                return;
            }
            ReportInterfaceMemberMissingDoc(context, interfaceDecl, location, name);
        }
        else
        {
            if (hasDoc && !IsInheritDocOnly(doc!))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.XmlDocOnConcreteType,
                    location,
                    name));
            }
        }
    }

    private static void ReportInterfaceMissingDoc(
        SyntaxNodeAnalysisContext context,
        InterfaceDeclarationSyntax interfaceDecl,
        Location location,
        string name)
    {
        var accessibility = GetTypeAccessibility(
            interfaceDecl,
            context.SemanticModel,
            context.CancellationToken);
        var descriptor = accessibility == Accessibility.Public
            ? Descriptors.PublicInterfaceMissingXmlDoc
            : Descriptors.NonPublicInterfaceMissingXmlDoc;
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, name));
    }

    private static void ReportInterfaceMemberMissingDoc(
        SyntaxNodeAnalysisContext context,
        InterfaceDeclarationSyntax interfaceDecl,
        Location location,
        string name)
    {
        var accessibility = GetTypeAccessibility(
            interfaceDecl,
            context.SemanticModel,
            context.CancellationToken);
        var descriptor = accessibility == Accessibility.Public
            ? Descriptors.PublicInterfaceMemberMissingXmlDoc
            : Descriptors.NonPublicInterfaceMemberMissingXmlDoc;
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, name));
    }

    private static bool TryGetDocComment(SyntaxNode node, out DocumentationCommentTriviaSyntax? doc)
    {
        doc = null;
        foreach (var trivia in node.GetLeadingTrivia())
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                && !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                continue;
            }
            if (trivia.GetStructure() is DocumentationCommentTriviaSyntax found)
            {
                doc = found;
                return true;
            }
        }
        return false;
    }

    private static bool IsInheritDocOnly(DocumentationCommentTriviaSyntax doc)
    {
        var sawInheritDoc = false;
        foreach (var content in doc.Content)
        {
            switch (content)
            {
                case XmlEmptyElementSyntax empty
                    when empty.Name.LocalName.ValueText == "inheritdoc":
                    sawInheritDoc = true;
                    continue;
                case XmlElementSyntax element
                    when element.StartTag.Name.LocalName.ValueText == "inheritdoc":
                    sawInheritDoc = true;
                    continue;
                case XmlTextSyntax xmlText when IsWhitespaceOnly(xmlText):
                    continue;
                default:
                    return false;
            }
        }
        return sawInheritDoc;
    }

    private static bool IsWhitespaceOnly(XmlTextSyntax xmlText)
    {
        foreach (var token in xmlText.TextTokens)
        {
            if (!string.IsNullOrWhiteSpace(token.Text))
            {
                return false;
            }
        }
        return true;
    }

    private static Accessibility GetTypeAccessibility(
        BaseTypeDeclarationSyntax type,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetDeclaredSymbol(type, cancellationToken);
        return symbol?.DeclaredAccessibility ?? Accessibility.Internal;
    }

    private static (Location? Location, string Name) GetTypeIdentifier(SyntaxNode node)
        => node switch
        {
            BaseTypeDeclarationSyntax t => (t.Identifier.GetLocation(), t.Identifier.ValueText),
            DelegateDeclarationSyntax d => (d.Identifier.GetLocation(), d.Identifier.ValueText),
            _ => (null, "<unknown>"),
        };

    private static (Location? Location, string Name) GetMemberIdentifier(SyntaxNode node)
        => node switch
        {
            MethodDeclarationSyntax m
                => (m.Identifier.GetLocation(), m.Identifier.ValueText),
            PropertyDeclarationSyntax p
                => (p.Identifier.GetLocation(), p.Identifier.ValueText),
            FieldDeclarationSyntax f when f.Declaration.Variables.Count > 0
                => (f.Declaration.Variables[0].Identifier.GetLocation(),
                    f.Declaration.Variables[0].Identifier.ValueText),
            EventDeclarationSyntax e
                => (e.Identifier.GetLocation(), e.Identifier.ValueText),
            EventFieldDeclarationSyntax ef when ef.Declaration.Variables.Count > 0
                => (ef.Declaration.Variables[0].Identifier.GetLocation(),
                    ef.Declaration.Variables[0].Identifier.ValueText),
            ConstructorDeclarationSyntax c
                => (c.Identifier.GetLocation(), c.Identifier.ValueText),
            OperatorDeclarationSyntax o
                => (o.OperatorToken.GetLocation(), o.OperatorToken.ValueText),
            ConversionOperatorDeclarationSyntax co
                => (co.Type.GetLocation(), co.Type.ToString()),
            IndexerDeclarationSyntax i
                => (i.ThisKeyword.GetLocation(), "this[]"),
            EnumMemberDeclarationSyntax em
                => (em.Identifier.GetLocation(), em.Identifier.ValueText),
            _ => (null, "<unknown>"),
        };
}
