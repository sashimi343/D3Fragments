// Static checks for unit test quality. Run from the repository root:
//   dotnet run scripts/TestQualityCheck.cs [repoRoot]
// Exit code 1 when any finding exists, so that it can be used as a gate later.
//
// Checks
//   NoAssertion       : a test ([Fact]/[Theory]) that reaches no assertion (directly or via helpers in the same file)
//   Sleep             : Thread.Sleep / Task.Delay in test code (timing dependent tests)
//   ConditionalLogic  : if / for / foreach / while / switch inside a test method body
//   Skipped           : a test with Skip = ...
//   DiagnosticNoPositive : a diagnostic ID that no test expects to be reported
//   AnalyzerNoNegative   : an analyzer without a test that expects no diagnostics
//   CodeFixUntested      : a CodeFixProvider not referenced by any test
//   PublicTypeUntested   : a public top-level type in src that no test references by name
//                          (static classes / interfaces: by any of their method or property names; a heuristic)
#:package Microsoft.CodeAnalysis.CSharp
#:property PublishAot=false
#:property TreatWarningsAsErrors=false
#:property EnableNETAnalyzers=false

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var root = Path.GetFullPath(args.Length > 0 ? args[0] : Directory.GetCurrentDirectory());
var findings = new List<(string Kind, string Where, string What)>();

static IEnumerable<(string Path, SyntaxNode Root)> Parse(string dir) =>
    Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
        .Where(p => !Regex.IsMatch(p, @"[\\/](bin|obj)[\\/]"))
        .Select(p => (p, CSharpSyntaxTree.ParseText(File.ReadAllText(p), path: p).GetRoot()));

var tests = Parse(Path.Combine(root, "tests")).ToList();
var sources = Parse(Path.Combine(root, "src")).ToList();

string Loc(SyntaxNode n) =>
    $"{Path.GetRelativePath(root, n.SyntaxTree.FilePath).Replace('\\', '/')}:{n.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";

static bool IsTest(MethodDeclarationSyntax m) =>
    m.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() is "Fact" or "Theory" or "Xunit.Fact" or "Xunit.Theory");

static string? InvokedName(InvocationExpressionSyntax i) => i.Expression switch
{
    MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
    IdentifierNameSyntax id => id.Identifier.Text,
    GenericNameSyntax g => g.Identifier.Text,
    _ => null,
};

// Assert.X / ResultAssert.X / *Assert.X, or Verify* / RunAsync (Roslyn analyzer testing) / Assert*
static bool IsAssertion(InvocationExpressionSyntax i) =>
    (i.Expression is MemberAccessExpressionSyntax { Expression: var recv } && recv.ToString().EndsWith("Assert", StringComparison.Ordinal))
    || InvokedName(i) is { } n && (n.StartsWith("Verify", StringComparison.Ordinal) || n.StartsWith("Assert", StringComparison.Ordinal) || n == "RunAsync");

// ---- per test method checks ----
foreach (var (_, fileRoot) in tests)
{
    var methods = fileRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

    // helper methods (by name) that reach an assertion, resolved to a fixed point within the file
    var asserting = new HashSet<string>();
    for (var changed = true; changed;)
    {
        changed = false;
        foreach (var m in methods.Where(m => !asserting.Contains(m.Identifier.Text)))
        {
            if (m.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i => IsAssertion(i) || InvokedName(i) is { } n && asserting.Contains(n)))
                changed = asserting.Add(m.Identifier.Text) || changed;
        }
    }

    foreach (var m in methods.Where(IsTest))
    {
        var name = $"{(m.Parent as TypeDeclarationSyntax)?.Identifier.Text}.{m.Identifier.Text}";
        if (!asserting.Contains(m.Identifier.Text))
            findings.Add(("NoAssertion", Loc(m), name));
        // statements inside lambdas / local functions (e.g. fake handlers) are not the test's own logic
        foreach (var s in m.Body?.DescendantNodes(n => n is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)).Where(n => n is IfStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax or SwitchStatementSyntax) ?? [])
            findings.Add(("ConditionalLogic", Loc(s), $"{name}: {s.Kind()}"));
        if (m.AttributeLists.SelectMany(a => a.Attributes).SelectMany(a => a.ArgumentList?.Arguments ?? default).Any(a => a.NameEquals?.Name.Identifier.Text == "Skip"))
            findings.Add(("Skipped", Loc(m), name));
    }

    foreach (var i in fileRoot.DescendantNodes().OfType<InvocationExpressionSyntax>())
    {
        var s = i.Expression.ToString();
        if (s.EndsWith("Thread.Sleep", StringComparison.Ordinal) || s.EndsWith("Task.Delay", StringComparison.Ordinal))
            findings.Add(("Sleep", Loc(i), s));
    }
}

// ---- analyzers / code fixes ----
static bool Derives(ClassDeclarationSyntax c, string baseName) =>
    c.BaseList?.Types.Any(t => t.Type.ToString().EndsWith(baseName, StringComparison.Ordinal)) == true;

var testMethods = tests.SelectMany(t => t.Root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(IsTest).Select(m => (t.Root, Method: m))).ToList();
static bool MentionsDiagnostic(SyntaxNode n) => n.DescendantNodes().OfType<IdentifierNameSyntax>().Any(id => Regex.IsMatch(id.Identifier.Text, @"^DFR\d+$"));

foreach (var analyzer in sources.SelectMany(s => s.Root.DescendantNodes().OfType<ClassDeclarationSyntax>()).Where(c => Derives(c, "DiagnosticAnalyzer")))
{
    var name = analyzer.Identifier.Text;
    var ids = analyzer.DescendantNodes().OfType<IdentifierNameSyntax>().Select(i => i.Identifier.Text).Where(t => Regex.IsMatch(t, @"^DFR\d+$")).Distinct().Order();
    // test files that verify this analyzer: AnalyzerVerifier<...Analyzer> / CodeFixVerifier<...Analyzer, ...>
    var mine = testMethods.Where(t => t.Root.DescendantNodes().OfType<GenericNameSyntax>()
        .Any(g => g.Identifier.Text.EndsWith("Verifier", StringComparison.Ordinal) && g.TypeArgumentList.Arguments.Any(a => a.ToString().EndsWith(name, StringComparison.Ordinal)))).ToList();

    foreach (var id in ids)
    {
        if (!mine.Any(t => t.Method.DescendantNodes().OfType<IdentifierNameSyntax>().Any(i => i.Identifier.Text == id)))
            findings.Add(("DiagnosticNoPositive", Loc(analyzer), $"{name}: {id}"));
    }
    if (!mine.Any(t => !MentionsDiagnostic(t.Method)))
        findings.Add(("AnalyzerNoNegative", Loc(analyzer), name));
}

var testIdentifiers = tests.SelectMany(t => t.Root.DescendantNodes().OfType<SimpleNameSyntax>()).Select(n => n.Identifier.Text).ToHashSet();
foreach (var fix in sources.SelectMany(s => s.Root.DescendantNodes().OfType<ClassDeclarationSyntax>()).Where(c => Derives(c, "CodeFixProvider")))
{
    if (!testIdentifiers.Contains(fix.Identifier.Text))
        findings.Add(("CodeFixUntested", Loc(fix), fix.Identifier.Text));
}

// ---- public types (analyzers / code fixes are covered above) ----
var srcTypes = sources.SelectMany(s => s.Root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()).ToList();
// names used as a base type in src: abstract bases are tested through their derived types
var srcBaseNames = srcTypes.SelectMany(t => t.BaseList?.Types ?? default)
    .Select(b => b.Type is GenericNameSyntax g ? g.Identifier.Text : b.Type.ToString().Split('.')[^1]).ToHashSet();
foreach (var type in srcTypes)
{
    if (type.Parent is BaseTypeDeclarationSyntax || !type.Modifiers.Any(SyntaxKind.PublicKeyword))
        continue;
    if (type.Modifiers.Any(SyntaxKind.AbstractKeyword) && srcBaseNames.Contains(type.Identifier.Text))
        continue;
    if (type is ClassDeclarationSyntax c && (Derives(c, "DiagnosticAnalyzer") || Derives(c, "CodeFixProvider")))
        continue;
    var name = type.Identifier.Text;
    // static classes (extension methods) and interfaces are used through their members, not their names
    var members = type is InterfaceDeclarationSyntax || type.Modifiers.Any(SyntaxKind.StaticKeyword)
        ? type.DescendantNodes().OfType<MemberDeclarationSyntax>().Select(m => m switch
        {
            MethodDeclarationSyntax x => x.Identifier.Text,
            PropertyDeclarationSyntax x => x.Identifier.Text,
            _ => null,
        }).OfType<string>()
        : [];
    if (!testIdentifiers.Contains(name) && !members.Any(testIdentifiers.Contains)
        && !(name.EndsWith("Attribute", StringComparison.Ordinal) && testIdentifiers.Contains(name[..^"Attribute".Length])))
        findings.Add(("PublicTypeUntested", Loc(type), name));
}

// ---- report ----
foreach (var g in findings.GroupBy(f => f.Kind))
{
    Console.WriteLine($"## {g.Key} ({g.Count()})");
    foreach (var f in g)
        Console.WriteLine($"  {f.Where}  {f.What}");
}
Console.WriteLine($"Test methods: {testMethods.Count}, findings: {findings.Count}");
return findings.Count == 0 ? 0 : 1;
