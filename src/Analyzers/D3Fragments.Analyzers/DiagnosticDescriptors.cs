using System.Resources;
using Microsoft.CodeAnalysis;

namespace D3Fragments.Analyzers;

// 全規則の診断記述子。仕様は docs/ja/analyzers/DFRxxxx.md を正本とする
internal static class DiagnosticDescriptors
{
    // ponytail: GitHub Pages のパス構成が未確定のため仮置き。確定したらここだけ差し替える
    private const string HelpLinkBase = "https://sashimi343.github.io/D3Fragments/";

    private const string ArchitectureCategory = "Architecture";
    private const string DesignCategory = "Design";

    // コード修正のタイトルもこのリソースから取得する
    internal static readonly ResourceManager Resources =
        new("D3Fragments.Analyzers.Resources.DiagnosticResources", typeof(DiagnosticDescriptors).Assembly);

    // プロジェクト単位の診断のため、コンパイル完了時に報告する
    public static readonly DiagnosticDescriptor DFR0001 = Create("DFR0001", ArchitectureCategory, WellKnownDiagnosticTags.CompilationEnd);
    public static readonly DiagnosticDescriptor DFR0002 = Create("DFR0002", ArchitectureCategory);
    public static readonly DiagnosticDescriptor DFR0003 = Create("DFR0003", DesignCategory);
    public static readonly DiagnosticDescriptor DFR0004 = Create("DFR0004", DesignCategory);
    public static readonly DiagnosticDescriptor DFR0005 = Create("DFR0005", DesignCategory);
    public static readonly DiagnosticDescriptor DFR0006 = Create("DFR0006", DesignCategory);
    public static readonly DiagnosticDescriptor DFR0007 = Create("DFR0007", DesignCategory);
    public static readonly DiagnosticDescriptor DFR0008 = Create("DFR0008", DesignCategory);
    public static readonly DiagnosticDescriptor DFR0009 = Create("DFR0009", DesignCategory);
    public static readonly DiagnosticDescriptor DFR0010 = Create("DFR0010", DesignCategory);

    private static DiagnosticDescriptor Create(string id, string category, params string[] customTags) =>
        new(
            id,
            new LocalizableResourceString(id + "_Title", Resources, typeof(DiagnosticDescriptors)),
            new LocalizableResourceString(id + "_MessageFormat", Resources, typeof(DiagnosticDescriptors)),
            category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLinkBase + "en/analyzers/" + id + ".html",
            customTags: customTags);
}
