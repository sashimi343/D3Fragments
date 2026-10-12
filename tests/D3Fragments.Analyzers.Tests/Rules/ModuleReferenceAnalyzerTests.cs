using D3Fragments.Analyzers.Tests.Helpers;
using Verifier = D3Fragments.Analyzers.Tests.Helpers.AnalyzerVerifier<D3Fragments.Analyzers.Rules.ModuleReferenceAnalyzer>;

namespace D3Fragments.Analyzers.Tests.Rules;

public sealed class ModuleReferenceAnalyzerTests
{
    private const ArchitectureLayerType All =
        ArchitectureLayerType.Presentation | ArchitectureLayerType.Application | ArchitectureLayerType.Domain | ArchitectureLayerType.Infrastructure;

    // 各モジュールレイヤーの型。テストごとに参照元の型を追加する
    private const string Types = """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using D3Fragments;
        using D3Fragments.Application;
        using Microsoft.AspNetCore.Mvc;

        public sealed class Shelf : AggregateRoot { }
        public interface IShelfRepository : IRepository<Shelf> { }
        public sealed record GetShelf(UuidV7 Id) : IQuery<int>;
        public sealed class GetShelfHandler : IRequestHandler<GetShelf, int>
        {
            public Task<int> HandleAsync(GetShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
        }
        public sealed class Helper { }
        """;

    [Fact]
    public async Task ControllerInjectingRepository_ReportsDiagnosticWithMessage()
    {
        const string source = Types + """
            public sealed class ShelvesController({|#0:IShelfRepository|} repository) : ControllerBase { }
            """;

        await Verifier.VerifyAsync(source, All, Verifier.Diagnostic(DiagnosticDescriptors.DFR0002)
            .WithLocation(0)
            .WithArguments("Presentation_Controller", "ShelvesController", "Domain_RepositoryInterface", "IShelfRepository"));
    }

    [Fact]
    public async Task ControllerUsingRequestExecutor_ReportsNothing()
    {
        const string source = Types + """
            public sealed class ShelvesController(IRequestExecutor<GetShelf, int> executor) : ControllerBase
            {
                public Task<int> Get(UuidV7 id) => executor.ExecuteAsync(new GetShelf(id), CancellationToken.None);
            }
            """;

        await Verifier.VerifyAsync(source, All);
    }

    [Fact]
    public async Task ReferencesInVariousPositions_ReportEach()
    {
        const string source = Types + """
            public sealed class ShelvesController : ControllerBase
            {
                private readonly List<{|#0:IShelfRepository|}> _repositories = [];

                public object Run()
                {
                    {|#1:GetShelfHandler|} handler = new {|#2:GetShelfHandler|}();
                    return typeof({|#3:IShelfRepository|});
                }
            }
            """;

        await Verifier.VerifyAsync(source, All,
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0002).WithLocation(0).WithArguments("Presentation_Controller", "ShelvesController", "Domain_RepositoryInterface", "IShelfRepository"),
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0002).WithLocation(1).WithArguments("Presentation_Controller", "ShelvesController", "Application_RequestHandler", "GetShelfHandler"),
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0002).WithLocation(2).WithArguments("Presentation_Controller", "ShelvesController", "Application_RequestHandler", "GetShelfHandler"),
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0002).WithLocation(3).WithArguments("Presentation_Controller", "ShelvesController", "Domain_RepositoryInterface", "IShelfRepository"));
    }

    [Fact]
    public async Task EntityReferencingApplicationHandler_ReportsDiagnostic()
    {
        const string source = Types + """
            public sealed class Book : Entity
            {
                public void Run() => new {|#0:GetShelfHandler|}();
            }
            """;

        await Verifier.VerifyAsync(source, ArchitectureLayerType.Application | ArchitectureLayerType.Domain,
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0002).WithLocation(0).WithArguments("Domain_Entity", "Book", "Application_RequestHandler", "GetShelfHandler"));
    }

    [Fact]
    public async Task DomainRuleReferencingRepository_ReportsDiagnostic()
    {
        const string source = Types + """
            public sealed class ShelfRule({|#0:IShelfRepository|} repository) : IDomainRule<Shelf>
            {
                public Result Check(Shelf context) => Result.Success();
            }
            """;

        await Verifier.VerifyAsync(source, ArchitectureLayerType.Domain | ArchitectureLayerType.Application,
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0002).WithLocation(0).WithArguments("Domain_DomainRule", "ShelfRule", "Domain_RepositoryInterface", "IShelfRepository"));
    }

    [Fact]
    public async Task RepositoryInterfaceReferencingRequest_ReportsDiagnostic()
    {
        const string source = Types + """
            public interface IBookRepository : IRepository<Shelf>
            {
                Task<Shelf?> FindAsync({|#0:GetShelf|} query);
            }
            """;

        await Verifier.VerifyAsync(source, All,
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0002).WithLocation(0).WithArguments("Domain_RepositoryInterface", "IBookRepository", "Application_Request", "GetShelf"));
    }

    [Fact]
    public async Task ReferenceInNameof_ReportsNothing()
    {
        const string source = Types + """
            public sealed class Book : Entity
            {
                public string Name => nameof(IShelfRepository);
            }
            """;

        await Verifier.VerifyAsync(source, All);
    }

    // 複数層のアセンブリで Other と判定される型は検証できない
    [Fact]
    public async Task EntityReferencingOtherInMultiLayerAssembly_ReportsNothing()
    {
        const string source = Types + """
            public sealed class Book : Entity
            {
                public Helper? Helper { get; private set; }
            }
            """;

        await Verifier.VerifyAsync(source, ArchitectureLayerType.Application | ArchitectureLayerType.Domain);
    }

    [Fact]
    public async Task AssemblyWithoutAttribute_ReportsNothing()
    {
        const string source = Types + """
            public sealed class ShelvesController(IShelfRepository repository) : ControllerBase { }
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task ReferenceToTypeInReferencedAssembly_UsesLayerOfThatAssembly()
    {
        const string source = """
            public sealed class ShelvesController({|#0:IShelfRepository|} repository) : Microsoft.AspNetCore.Mvc.ControllerBase { }
            """;
        const string domainSource = """
            public sealed class Shelf : D3Fragments.AggregateRoot { }
            public interface IShelfRepository : D3Fragments.IRepository<Shelf> { }
            """;

        var test = Verifier.CreateTest(source, ArchitectureLayerType.Presentation);
        test.AddReferencedProject("Domain", domainSource, ArchitectureLayerType.Domain);
        test.ExpectedDiagnostics.Add(Verifier.Diagnostic(DiagnosticDescriptors.DFR0002)
            .WithLocation(0)
            .WithArguments("Presentation_Controller", "ShelvesController", "Domain_RepositoryInterface", "IShelfRepository"));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
