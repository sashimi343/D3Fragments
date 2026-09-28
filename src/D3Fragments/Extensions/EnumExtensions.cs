using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace D3Fragments.Extensions;

/// <summary>
///     <para xml:lang="en">Provides extension methods for enums.</para>
///     <para xml:lang="ja">列挙型に対する拡張メソッドを提供する。</para>
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Gets the display name of the enum member from its <see cref="DisplayAttribute"/> (localized when <see cref="DisplayAttribute.ResourceType"/> is set). Returns <see cref="Enum.ToString()"/> when the attribute is absent, and for undefined or combined flag values.</para>
    ///     <para xml:lang="ja">列挙子に付与された<see cref="DisplayAttribute"/>から表示名を取得する（<see cref="DisplayAttribute.ResourceType"/>指定時は多言語化される）。属性がない場合、および未定義値・複数フラグの組み合わせ値の場合は<see cref="Enum.ToString()"/>の結果を返す。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The enum value.</para>
    ///     <para xml:lang="ja">列挙子。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The display name of the enum member.</para>
    ///     <para xml:lang="ja">列挙子の表示名。</para>
    /// </returns>
    public static string GetDisplayName(this Enum value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var name = value.ToString();
        return value.GetType().GetField(name)?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? name;
    }
}
