using System.Text.Json;

namespace D3Fragments.Extensions;

/// <summary>
///     <para xml:lang="en">Provides extension methods for strings.</para>
///     <para xml:lang="ja">文字列に対する拡張メソッドを提供する。</para>
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Converts the string to lower snake case, treating consecutive uppercase letters as one word (e.g. <c>"HTMLParser"</c> to <c>"html_parser"</c>, <c>"userID"</c> to <c>"user_id"</c>). Uses the same rules as <see cref="JsonNamingPolicy.SnakeCaseLower"/>.</para>
    ///     <para xml:lang="ja">文字列をスネークケース（小文字）に変換する。連続する大文字は1単語として扱う（例：<c>"HTMLParser"</c>→<c>"html_parser"</c>、<c>"userID"</c>→<c>"user_id"</c>）。変換規則は<see cref="JsonNamingPolicy.SnakeCaseLower"/>と同じ。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The string to convert.</para>
    ///     <para xml:lang="ja">変換する文字列。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The snake-cased string.</para>
    ///     <para xml:lang="ja">スネークケースに変換された文字列。</para>
    /// </returns>
    public static string ToSnakeCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonNamingPolicy.SnakeCaseLower.ConvertName(value);
    }
}
