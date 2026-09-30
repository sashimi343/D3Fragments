using Microsoft.Extensions.DependencyInjection;

namespace D3Fragments.AspNetCore;

/// <summary>
///     <para xml:lang="en">Provides extension methods that register the ASP.NET Core integration of D3Fragments.</para>
///     <para xml:lang="ja">D3FragmentsのASP.NET Core連携機能を登録する拡張メソッドを提供する。</para>
/// </summary>
public static class D3FragmentsServiceCollectionExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Registers the services that convert domain errors into Problem Details responses, and the JSON converter for value objects.</para>
    ///     <para xml:lang="ja">ドメインエラーをProblem Details応答へ変換するサービスと、値オブジェクト用のJSONコンバーターを登録する。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">This method registers the Problem Details service, an <see cref="Microsoft.AspNetCore.Diagnostics.IExceptionHandler"/> that converts <see cref="DomainException"/>, and <see cref="ValueObjectJsonConverterFactory"/> for both Minimal APIs and MVC. Call <c>app.UseExceptionHandler()</c> to enable the exception handler. Other exceptions are left to the standard exception handling. <see cref="TypeSafeEnumJsonConverterFactory"/> is not registered because its representation depends on the application.</para>
    ///     <para xml:lang="ja">このメソッドは、Problem Detailsサービス、<see cref="DomainException"/>を変換する<see cref="Microsoft.AspNetCore.Diagnostics.IExceptionHandler"/>、Minimal API・MVC両方への<see cref="ValueObjectJsonConverterFactory"/>を登録する。例外ハンドラを有効にするには<c>app.UseExceptionHandler()</c>を呼び出す。その他の例外は標準の例外処理に委ねる。<see cref="TypeSafeEnumJsonConverterFactory"/>は表現方法がアプリケーションに依存するため登録しない。</para>
    /// </remarks>
    /// <param name="services">
    ///     <para xml:lang="en">The service collection to add the services to.</para>
    ///     <para xml:lang="ja">サービスを追加するサービスコレクション。</para>
    /// </param>
    /// <param name="configure">
    ///     <para xml:lang="en">The action that configures <see cref="DomainProblemDetailsOptions"/>, or <see langword="null"/> to use the defaults.</para>
    ///     <para xml:lang="ja"><see cref="DomainProblemDetailsOptions"/>を構成するアクション。既定値を使用する場合は<see langword="null"/>。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The same service collection for chaining.</para>
    ///     <para xml:lang="ja">メソッドチェーン用の同じサービスコレクション。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="services"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="services"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    public static IServiceCollection AddD3FragmentsAspNetCore(
        this IServiceCollection services,
        Action<DomainProblemDetailsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails();
        services.AddExceptionHandler<DomainExceptionHandler>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new ValueObjectJsonConverterFactory()));
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o => o.JsonSerializerOptions.Converters.Add(new ValueObjectJsonConverterFactory()));
        return services;
    }
}
