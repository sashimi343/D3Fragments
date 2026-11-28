using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace D3Fragments.Application.Tests;

public class IsolatedRequestExecutorTests
{
    // Handlerが解決されたスコープを識別するためのScopedサービス
    public sealed class ScopeProbe : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    public sealed record GetProbeQuery : IQuery<ScopeProbe>;

    public sealed class GetProbeQueryHandler(ScopeProbe probe) : IRequestHandler<GetProbeQuery, ScopeProbe>
    {
        public Task<ScopeProbe> HandleAsync(GetProbeQuery request, CancellationToken cancellationToken) => Task.FromResult(probe);
    }

    public sealed record FailingQuery : IQuery<int>;

    public sealed class FailingQueryHandler(ScopeProbe probe) : IRequestHandler<FailingQuery, int>
    {
        public static ScopeProbe? LastProbe { get; private set; }

        public Task<int> HandleAsync(FailingQuery request, CancellationToken cancellationToken)
        {
            LastProbe = probe;
            throw new DomainException(Error.Conflict("conflict", code: "E001"));
        }
    }

    private sealed class NullUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        public void DiscardChanges()
        {
        }

        public Task<TResponse> ExecuteInTransactionAsync<TResponse>(Func<CancellationToken, Task<TResponse>> action, CancellationToken cancellationToken) =>
            action(cancellationToken);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<IUnitOfWork, NullUnitOfWork>();
        services.AddScoped<ScopeProbe>();
        services.AddD3FragmentsApplication([typeof(IsolatedRequestExecutorTests).Assembly]);
        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public async Task 呼び出し元とは別のスコープでHandlerを実行する()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var callerProbe = scope.ServiceProvider.GetRequiredService<ScopeProbe>();

        var isolated = scope.ServiceProvider.GetRequiredService<IIsolatedRequestExecutor<GetProbeQuery, ScopeProbe>>();
        var probe = await isolated.ExecuteAsync(new GetProbeQuery(), CancellationToken.None);

        Assert.NotSame(callerProbe, probe);
        Assert.True(probe.Disposed);
        Assert.False(callerProbe.Disposed);
    }

    [Fact]
    public async Task 通常のExecutorは呼び出し元と同じスコープでHandlerを実行する()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var callerProbe = scope.ServiceProvider.GetRequiredService<ScopeProbe>();

        var executor = scope.ServiceProvider.GetRequiredService<IRequestExecutor<GetProbeQuery, ScopeProbe>>();
        var probe = await executor.ExecuteAsync(new GetProbeQuery(), CancellationToken.None);

        Assert.Same(callerProbe, probe);
    }

    [Fact]
    public async Task TryExecuteAsyncでDomainExceptionを失敗の結果に変換しスコープを破棄する()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var isolated = scope.ServiceProvider.GetRequiredService<IIsolatedRequestExecutor<FailingQuery, int>>();
        var result = await isolated.TryExecuteAsync(new FailingQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("E001", Assert.Single(result.Errors).Code);
        Assert.True(FailingQueryHandler.LastProbe!.Disposed);
    }

    [Fact]
    public void AddD3FragmentsApplicationでIsolatedRequestExecutorが登録される()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var isolated = scope.ServiceProvider.GetRequiredService<IIsolatedRequestExecutor<GetProbeQuery, ScopeProbe>>();

        Assert.IsType<IsolatedRequestExecutor<GetProbeQuery, ScopeProbe>>(isolated);
    }
}
