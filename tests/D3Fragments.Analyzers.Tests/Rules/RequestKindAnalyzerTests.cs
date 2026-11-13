using Verifier = D3Fragments.Analyzers.Tests.Helpers.AnalyzerVerifier<D3Fragments.Analyzers.Rules.RequestKindAnalyzer>;

namespace D3Fragments.Analyzers.Tests.Rules;

public sealed class RequestKindAnalyzerTests
{
    private const string Usings = """
        using D3Fragments.Application;

        """;

    [Fact]
    public async Task RequestImplementingIRequestDirectly_ReportsDfr0016WithMessage()
    {
        const string source = Usings + """
            public sealed record {|#0:GetShelf|} : IRequest<int>;
            """;

        await Verifier.VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0016)
            .WithLocation(0)
            .WithArguments("GetShelf"));
    }

    [Fact]
    public async Task RequestImplementingBothCommandAndQuery_ReportsDfr0016()
    {
        const string source = Usings + """
            public sealed class {|DFR0016:AddShelf|} : ICommand<int>, IQuery<int> { }
            public struct {|DFR0016:CountShelves|} : IRequest<int> { }
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task RequestImplementingEitherCommandOrQuery_DoesNotReport()
    {
        const string source = Usings + """
            public sealed record AddShelf : ICommand<int>;
            public sealed record GetShelf : IQuery<int>;
            public sealed record MoveShelf : IRequest<int>, ITransactionalCommand;
            public abstract record ShelfRequest : IRequest<int>;
            public sealed record RemoveShelf : ShelfRequest, ICommand<int>;
            public interface IShelfRequest : IRequest<int> { }
            public sealed class NotARequest { }
            """;

        await Verifier.VerifyAsync(source);
    }
}
