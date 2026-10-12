using D3Fragments.Analyzers.Tests.Helpers;
using Verifier = D3Fragments.Analyzers.Tests.Helpers.AnalyzerVerifier<D3Fragments.Analyzers.Rules.ControllerInjectionAnalyzer>;

namespace D3Fragments.Analyzers.Tests.Rules;

public sealed class ControllerInjectionAnalyzerTests
{
    // ドメイン層単独のアセンブリ。ドメインサービス・TypeSafeEnum は Domain_Other と判定される
    private const string DomainSource = """
        public sealed class Shelf : D3Fragments.AggregateRoot { }
        public interface IShelfPricing { int Price(Shelf shelf); }
        public sealed class ShelfPricing : IShelfPricing { public int Price(Shelf shelf) => 0; }
        public sealed class ShelfSize : D3Fragments.TypeSafeEnum<ShelfSize, int>
        {
            public static readonly ShelfSize Small = new(1);
            private ShelfSize(int key) : base(key) { }
        }
        """;

    private static async Task VerifyWithDomainAsync(string source, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
    {
        var test = Verifier.CreateTest(source, ArchitectureLayerType.Presentation);
        test.AddReferencedProject("Domain", DomainSource, ArchitectureLayerType.Domain);
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ControllerInjectingDomainService_ReportsDiagnosticWithMessage()
    {
        const string source = """
            public sealed class ShelvesController({|#0:IShelfPricing|} pricing) : Microsoft.AspNetCore.Mvc.ControllerBase { }
            """;

        await VerifyWithDomainAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0011)
            .WithLocation(0)
            .WithArguments("ShelvesController", "IShelfPricing"));
    }

    [Fact]
    public async Task EveryConstructorParameter_IsReported()
    {
        const string source = """
            public sealed class ShelvesController : Microsoft.AspNetCore.Mvc.ControllerBase
            {
                public ShelvesController({|#0:ShelfPricing|} pricing, System.TimeProvider timeProvider) { }

                public ShelvesController({|#1:IShelfPricing|} pricing) { }
            }
            """;

        await VerifyWithDomainAsync(source,
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0011).WithLocation(0).WithArguments("ShelvesController", "ShelfPricing"),
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0011).WithLocation(1).WithArguments("ShelvesController", "IShelfPricing"));
    }

    // TypeSafeEnum 等の Domain_Other は、コンストラクタ以外での利用を許可する
    [Fact]
    public async Task DomainOtherOutsideConstructor_ReportsNothing()
    {
        const string source = """
            public sealed class ShelvesController : Microsoft.AspNetCore.Mvc.ControllerBase
            {
                public int Get(int size) => ShelfSize.Parse(size) == ShelfSize.Small ? 1 : 0;
            }
            """;

        await VerifyWithDomainAsync(source);
    }

    [Fact]
    public async Task NonControllerInjectingDomainService_ReportsNothing()
    {
        const string source = """
            public sealed class ShelfPresenter(IShelfPricing pricing) { }
            """;

        await VerifyWithDomainAsync(source);
    }

    // 複数層のアセンブリでは Other と判定され、ドメイン層のクラスか特定できない
    [Fact]
    public async Task DomainServiceInMultiLayerAssembly_ReportsNothing()
    {
        const string source = DomainSource + """
            public sealed class ShelvesController(IShelfPricing pricing) : Microsoft.AspNetCore.Mvc.ControllerBase { }
            """;

        await Verifier.VerifyAsync(source, ArchitectureLayerType.Presentation | ArchitectureLayerType.Domain);
    }

    [Fact]
    public async Task AssemblyWithoutAttribute_ReportsNothing()
    {
        const string source = DomainSource + """
            public sealed class ShelvesController(IShelfPricing pricing) : Microsoft.AspNetCore.Mvc.ControllerBase { }
            """;

        await Verifier.VerifyAsync(source);
    }
}
