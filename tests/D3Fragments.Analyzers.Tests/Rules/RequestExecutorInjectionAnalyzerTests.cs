using Verifier = D3Fragments.Analyzers.Tests.Helpers.AnalyzerVerifier<D3Fragments.Analyzers.Rules.RequestExecutorInjectionAnalyzer>;

namespace D3Fragments.Analyzers.Tests.Rules;

public sealed class RequestExecutorInjectionAnalyzerTests
{
    // 実行される側の Command・Query と、ハンドラが扱うリクエスト・イベント
    private const string Requests = """
        using System.Threading;
        using System.Threading.Tasks;
        using D3Fragments;
        using D3Fragments.Application;

        public sealed record AddLog : ICommand<int>;
        public sealed record CountLogs : IQuery<int>;
        public sealed record AddShelf : ICommand<int>;
        public sealed record GetShelf : IQuery<int>;
        public sealed record ShelfAdded(System.DateTimeOffset OccurredAt) : IDomainEvent;

        """;

    private static Task VerifyAsync(string source, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected) =>
        Verifier.VerifyAsync(Requests + source, ArchitectureLayerType.Application, expected);

    [Fact]
    public async Task CommandHandlerInjectingRequestExecutorOfCommand_ReportsDfr0018WithMessage()
    {
        const string source = """
            public sealed class AddShelfHandler({|#0:IRequestExecutor<AddLog, int>|} addLog) : IRequestHandler<AddShelf, int>
            {
                public Task<int> HandleAsync(AddShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
            }
            """;

        await VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0018)
            .WithLocation(0)
            .WithArguments("AddShelfHandler", "IRequestExecutor<AddLog, int>", "AddLog"));
    }

    [Fact]
    public async Task BeforeCommitHandlerInjectingRequestExecutorOfCommand_ReportsDfr0018()
    {
        const string source = """
            public sealed class LogShelfAdded({|#0:IRequestExecutor<AddLog, int>|} addLog) : IBeforeCommitDomainEventHandler<ShelfAdded>
            {
                public Task HandleAsync(ShelfAdded domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """;

        await VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0018)
            .WithLocation(0)
            .WithArguments("LogShelfAdded", "IRequestExecutor<AddLog, int>", "AddLog"));
    }

    [Fact]
    public async Task ImplementationOfRequestExecutorInEveryConstructor_ReportsDfr0018()
    {
        const string source = """
            public sealed class AddShelfHandler : IRequestHandler<AddShelf, int>
            {
                public AddShelfHandler({|#0:RequestExecutor<AddLog, int>|} addLog) { }

                public AddShelfHandler({|#1:IRequestExecutor<AddLog, int>|} addLog, System.TimeProvider timeProvider) { }

                public Task<int> HandleAsync(AddShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
            }
            """;

        await VerifyAsync(source,
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0018).WithLocation(0).WithArguments("AddShelfHandler", "RequestExecutor<AddLog, int>", "AddLog"),
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0018).WithLocation(1).WithArguments("AddShelfHandler", "IRequestExecutor<AddLog, int>", "AddLog"));
    }

    [Fact]
    public async Task QueryHandlerInjectingIsolatedRequestExecutorOfCommand_ReportsDfr0019WithMessage()
    {
        const string source = """
            public sealed class GetShelfHandler({|#0:IIsolatedRequestExecutor<AddLog, int>|} addLog) : IRequestHandler<GetShelf, int>
            {
                public Task<int> HandleAsync(GetShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
            }
            """;

        await VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0019)
            .WithLocation(0)
            .WithArguments("GetShelfHandler", "IIsolatedRequestExecutor<AddLog, int>", "AddLog"));
    }

    // 同じスコープの Executor でも、DFR0018 と重ねて報告しない
    [Fact]
    public async Task QueryHandlerInjectingRequestExecutorOfCommand_ReportsOnlyDfr0019()
    {
        const string source = """
            public sealed class GetShelfHandler({|#0:IRequestExecutor<AddLog, int>|} addLog) : IRequestHandler<GetShelf, int>
            {
                public Task<int> HandleAsync(GetShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
            }
            """;

        await VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0019)
            .WithLocation(0)
            .WithArguments("GetShelfHandler", "IRequestExecutor<AddLog, int>", "AddLog"));
    }

    // 最初に見つかる Executor が Query 用でも、Command 用の Executor を見逃さない
    [Fact]
    public async Task ExecutorOfQueryAndCommand_ReportsViolatingContract()
    {
        const string source = """
            public interface ILogExecutors : IRequestExecutor<CountLogs, int>, IRequestExecutor<AddLog, int> { }

            public sealed class AddShelfHandler({|#0:ILogExecutors|} logs) : IRequestHandler<AddShelf, int>
            {
                public Task<int> HandleAsync(AddShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
            }

            public sealed class GetShelfHandler({|#1:ILogExecutors|} logs) : IRequestHandler<GetShelf, int>
            {
                public Task<int> HandleAsync(GetShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
            }
            """;

        await VerifyAsync(source,
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0018).WithLocation(0).WithArguments("AddShelfHandler", "ILogExecutors", "AddLog"),
            Verifier.Diagnostic(DiagnosticDescriptors.DFR0019).WithLocation(1).WithArguments("GetShelfHandler", "ILogExecutors", "AddLog"));
    }

    // 別の Command を別スコープで実行できても、同じスコープで実行する Command の違反は除外しない
    [Fact]
    public async Task IsolatedExecutorOfAnotherCommand_ReportsDfr0018()
    {
        const string source = """
            public interface IShelfExecutors : IRequestExecutor<AddLog, int>, IIsolatedRequestExecutor<AddShelf, int> { }

            public sealed class AddShelfHandler({|#0:IShelfExecutors|} executors) : IRequestHandler<AddShelf, int>
            {
                public Task<int> HandleAsync(AddShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
            }
            """;

        await VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0018)
            .WithLocation(0)
            .WithArguments("AddShelfHandler", "IShelfExecutors", "AddLog"));
    }

    [Fact]
    public async Task CommandSideInjectingIsolatedRequestExecutorOfCommand_ReportsNothing()
    {
        const string source = """
            public sealed class AddShelfHandler(IIsolatedRequestExecutor<AddLog, int> addLog, IsolatedRequestExecutor<AddLog, int> addLog2) : IRequestHandler<AddShelf, int>
            {
                public Task<int> HandleAsync(AddShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
            }

            public sealed class LogShelfAdded(IIsolatedRequestExecutor<AddLog, int> addLog) : IBeforeCommitDomainEventHandler<ShelfAdded>
            {
                public Task HandleAsync(ShelfAdded domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """;

        await VerifyAsync(source);
    }

    // Commit 後のハンドラは、同じスコープで別の Command を実行する書き方を案内している
    [Fact]
    public async Task AfterCommitHandlerInjectingRequestExecutorOfCommand_ReportsNothing()
    {
        const string source = """
            public sealed class LogShelfAdded(IRequestExecutor<AddLog, int> addLog) : IAfterCommitDomainEventHandler<ShelfAdded>
            {
                public Task HandleAsync(ShelfAdded domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task HandlersInjectingRequestExecutorOfQuery_ReportsNothing()
    {
        const string source = """
            public sealed class AddShelfHandler(IRequestExecutor<CountLogs, int> countLogs) : IRequestHandler<AddShelf, int>
            {
                public Task<int> HandleAsync(AddShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
            }

            public sealed class GetShelfHandler(IRequestExecutor<CountLogs, int> countLogs, IIsolatedRequestExecutor<CountLogs, int> countLogs2) : IRequestHandler<GetShelf, int>
            {
                public Task<int> HandleAsync(GetShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task RequestOfTypeParameter_ReportsNothing()
    {
        const string source = """
            public sealed class AddShelfHandler<TCommand>(IRequestExecutor<TCommand, int> executor) : IRequestHandler<AddShelf, int>
                where TCommand : ICommand<int>
            {
                public Task<int> HandleAsync(AddShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task NonHandlerInjectingRequestExecutorOfCommand_ReportsNothing()
    {
        const string source = """
            public sealed class ShelfService(IRequestExecutor<AddLog, int> addLog) { }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task AssemblyWithoutAttribute_ReportsNothing()
    {
        const string source = Requests + """
            public sealed class AddShelfHandler(IRequestExecutor<AddLog, int> addLog) : IRequestHandler<AddShelf, int>
            {
                public Task<int> HandleAsync(AddShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
            }
            """;

        await Verifier.VerifyAsync(source);
    }
}
