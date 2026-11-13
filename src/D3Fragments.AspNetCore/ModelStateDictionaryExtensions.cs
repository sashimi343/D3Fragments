using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace D3Fragments.AspNetCore;

/// <summary>
///     <para xml:lang="en">Extension methods for reflecting domain errors in <see cref="ModelStateDictionary"/> (Razor Pages / MVC views).</para>
///     <para xml:lang="ja">ドメインエラーを<see cref="ModelStateDictionary"/>（Razor Pages / MVCのビュー）へ反映する拡張メソッド。</para>
/// </summary>
public static class ModelStateDictionaryExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Adds each error to the model state. <see cref="Error.Target"/> is used as the key; an error without a target is added as a model-level error (empty key).</para>
    ///     <para xml:lang="ja">各エラーをモデル状態に追加する。キーには<see cref="Error.Target"/>を使用し、対象のないエラーはモデル全体のエラー（空文字のキー）として追加する。</para>
    /// </summary>
    /// <param name="modelState">
    ///     <para xml:lang="en">The model state.</para>
    ///     <para xml:lang="ja">モデル状態。</para>
    /// </param>
    /// <param name="errors">
    ///     <para xml:lang="en">The errors to add, such as <see cref="Result.Errors"/> or <see cref="DomainException.Errors"/>.</para>
    ///     <para xml:lang="ja">追加するエラー。<see cref="Result.Errors"/>や<see cref="DomainException.Errors"/>など。</para>
    /// </param>
    public static void AddDomainErrors(this ModelStateDictionary modelState, IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(errors);
        foreach (var error in errors)
        {
            modelState.AddModelError(error.Target ?? string.Empty, error.Message);
        }
    }
}
