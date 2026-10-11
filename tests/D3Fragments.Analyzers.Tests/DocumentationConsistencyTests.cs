using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace D3Fragments.Analyzers.Tests;

// 規則の仕様（docs/{lang}/analyzers/DFRxxxx.md）と診断記述子の整合性を検証する
public sealed partial class DocumentationConsistencyTests
{
    public static TheoryData<string> Languages => ["ja"];

    private static readonly DiagnosticDescriptor[] Descriptors = typeof(DiagnosticDescriptors)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
        .Select(f => (DiagnosticDescriptor)f.GetValue(null)!)
        .ToArray();

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryDescriptor_HasMatchingDocument(string language)
    {
        var culture = CultureInfo.GetCultureInfo(language);
        foreach (var descriptor in Descriptors)
        {
            var path = Path.Combine(AnalyzersDocsDirectory(language), descriptor.Id + ".md");
            Assert.True(File.Exists(path), $"{path} does not exist.");

            var lines = File.ReadAllLines(path);
            var title = descriptor.Title.ToString(culture);
            Assert.Equal($"# {descriptor.Id}: {title}", lines[0]);
            Assert.Equal(descriptor.Id, TableValue(lines, 0));
            Assert.Equal(title, TableValue(lines, 1));
            Assert.Equal(descriptor.DefaultSeverity.ToString(), TableValue(lines, 2));
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryDocument_HasMatchingDescriptor(string language)
    {
        var documented = Directory.GetFiles(AnalyzersDocsDirectory(language))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => RuleIdPattern().IsMatch(name!))
            .Order();

        Assert.Equal(Descriptors.Select(d => d.Id).Order(), documented);
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Readme_ListsEveryRule(string language)
    {
        var culture = CultureInfo.GetCultureInfo(language);
        var readme = File.ReadAllText(Path.Combine(AnalyzersDocsDirectory(language), "README.md"));
        foreach (var descriptor in Descriptors)
        {
            Assert.Contains($"[{descriptor.Id}: {descriptor.Title.ToString(culture)}]({descriptor.Id}.md)", readme, StringComparison.Ordinal);
        }
    }

    // プロパティ表の行（規則 ID / タイトル / 既定の重大度 の順）の値を取得する
    private static string TableValue(string[] lines, int row) =>
        lines.Where(l => l.StartsWith('|')).Skip(2 + row).First().Split('|', StringSplitOptions.TrimEntries)[2];

    private static string AnalyzersDocsDirectory(string language)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "D3Fragments.slnx")))
        {
            directory = directory.Parent;
        }
        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "docs", language, "analyzers");
    }

    [GeneratedRegex(@"^DFR\d{4}$")]
    private static partial Regex RuleIdPattern();
}
