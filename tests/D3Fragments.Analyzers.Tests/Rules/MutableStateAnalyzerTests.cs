using D3Fragments.Analyzers.CodeFixes;
using D3Fragments.Analyzers.Rules;
using D3Fragments.Analyzers.Tests.Helpers;
using Verifier = D3Fragments.Analyzers.Tests.Helpers.AnalyzerVerifier<D3Fragments.Analyzers.Rules.MutableStateAnalyzer>;

namespace D3Fragments.Analyzers.Tests.Rules;

public sealed class MutableStateAnalyzerTests
{
    private const string Usings = """
        using System;
        using System.Collections.Generic;
        using System.Collections.Immutable;
        using System.Collections.ObjectModel;
        using D3Fragments;

        """;

    [Fact]
    public async Task EntityWithPublicSetter_ReportsDfr0003WithMessage()
    {
        const string source = Usings + """
            public sealed class TodoItem : Entity
            {
                public bool {|#0:IsDone|} { get; set; }
            }
            """;

        await Verifier.VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0003).WithLocation(0).WithArguments("TodoItem", "IsDone"));
    }

    [Fact]
    public async Task EntityMembers_ReportsOnlyExternallyWritableMembers()
    {
        const string source = Usings + """
            public interface IHasTitle { string Title { get; set; } }

            public sealed class TodoItem : AggregateRoot, IHasTitle
            {
                public const int MaxLength = 100;

                public bool {|DFR0003:IsDone|} { get; set; }
                public string {|DFR0003:Name|} { get; init; } = "";
                public int {|DFR0003:Priority|} { get; internal set; }
                public int {|DFR0003:Order|} { get; protected internal set; }
                public int {|DFR0003:Score|};
                internal int {|DFR0003:Rank|};
                public static int {|DFR0003:Counter|};

                public int Size { get; private set; }
                public int Weight { get; protected set; }
                public int Height { get; private protected set; }
                public int Width { get; }
                public int Depth => Width;
                public readonly int Code;
                protected int Level;
                private int _value;
                string IHasTitle.Title { get; set; } = "";
            }

            // 間接的な継承、internal クラスも対象
            internal class Base : EntityBase<int> { protected Base() : base(1) { } }
            internal sealed class Derived : Base { public int {|DFR0003:Value|} { get; set; } }
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task NonEntityClass_ReportsNothing()
    {
        const string source = Usings + """
            public sealed class Dto
            {
                public bool IsDone { get; set; }
                public List<int> Items { get; set; } = [];
            }
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task EntityExposingCollections_ReportsDfr0009ForMutableCollections()
    {
        const string source = Usings + """
            public sealed class Bookshelf : AggregateRoot
            {
                private readonly List<int> _items = [];

                public List<int> {|#0:Items|} { get; private set; } = [];
                public int[] {|DFR0009:Codes|} { get; private set; } = [];
                public IList<int> {|DFR0009:List|} => _items;
                public ICollection<int> {|DFR0009:Collection|} => _items;
                public Dictionary<int, string> {|DFR0009:Map|} { get; } = [];
                public HashSet<int> {|DFR0009:Set|} { get; } = [];
                public readonly List<int> {|DFR0009:Field|} = [];

                public IReadOnlyList<int> ReadOnlyItems => _items;
                public IEnumerable<int> Enumerable => _items;
                public ReadOnlyCollection<int> Wrapped => _items.AsReadOnly();
                public ReadOnlyDictionary<int, string> WrappedMap { get; } = new(new Dictionary<int, string>());
                public ImmutableArray<int> Immutable { get; } = [];
                public ImmutableList<int> ImmutableList { get; } = [];
                public string Text { get; } = "";
                protected List<int> Protected { get; } = [];
            }
            """;

        await Verifier.VerifyAsync(source,
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0009).WithLocation(0).WithArguments("Bookshelf", "Items", "List<int>"));
    }

    [Fact]
    public async Task EntityWithPublicSetterOfMutableCollection_ReportsBoth()
    {
        const string source = Usings + """
            public sealed class Bookshelf : AggregateRoot
            {
                public List<int> {|#0:Items|} { get; set; } = [];
            }
            """;

        await Verifier.VerifyAsync(source,
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0003).WithLocation(0).WithArguments("Bookshelf", "Items"),
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0009).WithLocation(0).WithArguments("Bookshelf", "Items", "List<int>"));
    }

    [Fact]
    public async Task ValueObjectWithPublicSetter_ReportsDfr0010()
    {
        const string source = Usings + """
            public sealed class Email : ValueObjectBase<Email, string>, IValueObject<Email, string>
            {
                private Email(string value) : base(value) { }

                public string {|#0:Domain|} { get; set; } = "";
                public string Local { get; } = "";
                public List<int> Items { get; } = [];

                public static Email Create(string value) => new(value);
                public static bool ValidateValue(string value, out string? errorMessage) { errorMessage = null; return true; }
            }

            // IValueObject のみを実装した位置指定レコード
            public sealed record Code(string {|DFR0010:Value|}) : IValueObject<Code, string>
            {
                public static Code Create(string value) => new(value);
                public static bool ValidateValue(string value, out string? errorMessage) { errorMessage = null; return true; }
            }
            """;

        await Verifier.VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0010).WithLocation(0).WithArguments("Email", "Domain"));
    }

    [Fact]
    public async Task CodeFix_ChangesSetterToPrivateSet()
    {
        const string source = Usings + """
            public sealed class TodoItem : Entity
            {
                public bool {|DFR0003:IsDone|} { get; set; }
                public string {|DFR0003:Name|} { get; init; } = "";
                public int {|DFR0003:Priority|} { get; internal set; }
                private int _order;
                public int {|DFR0003:Order|}
                {
                    get => _order;
                    set => _order = value;
                }
                public int {|DFR0003:Score|};
            }
            """;
        const string fixedSource = Usings + """
            public sealed class TodoItem : Entity
            {
                public bool IsDone { get; private set; }
                public string Name { get; private set; } = "";
                public int Priority { get; private set; }
                private int _order;
                public int Order
                {
                    get => _order;
                    private set => _order = value;
                }
                public int {|DFR0003:Score|};
            }
            """;

        await CodeFixVerifier<MutableStateAnalyzer, SetterAccessibilityCodeFixProvider>.VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ValueObject_ChangesSetterToPrivateSet()
    {
        const string source = Usings + """
            public sealed class Email : ValueObjectBase<Email, string>, IValueObject<Email, string>
            {
                private Email(string value) : base(value) { }
                public string {|DFR0010:Domain|} { get; set; } = "";
                public static Email Create(string value) => new(value);
                public static bool ValidateValue(string value, out string? errorMessage) { errorMessage = null; return true; }
            }
            """;
        const string fixedSource = Usings + """
            public sealed class Email : ValueObjectBase<Email, string>, IValueObject<Email, string>
            {
                private Email(string value) : base(value) { }
                public string Domain { get; private set; } = "";
                public static Email Create(string value) => new(value);
                public static bool ValidateValue(string value, out string? errorMessage) { errorMessage = null; return true; }
            }
            """;

        await CodeFixVerifier<MutableStateAnalyzer, SetterAccessibilityCodeFixProvider>.VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task CodeFix_KeepsIndentationAndAttributesOfAccessor()
    {
        const string source = Usings + """
            public sealed class TodoItem : Entity
            {
                private int _total;
                public int {|DFR0003:Total|}
                {
                    get => _total;
                    internal set => _total = value;
                }
                public int {|DFR0003:Level|}
                {
                    get;
                    [System.Obsolete] set;
                }
            }
            """;
        const string fixedSource = Usings + """
            public sealed class TodoItem : Entity
            {
                private int _total;
                public int Total
                {
                    get => _total;
                    private set => _total = value;
                }
                public int Level
                {
                    get;
                    [System.Obsolete] private set;
                }
            }
            """;

        await CodeFixVerifier<MutableStateAnalyzer, SetterAccessibilityCodeFixProvider>.VerifyAsync(source, fixedSource);
    }
}
