namespace D3Fragments.Validations;

/// <summary>
///     <para xml:lang="en">Specifies how <see cref="StringValidator"/> counts the length of a string.</para>
///     <para xml:lang="ja"><see cref="StringValidator"/>が文字数を数える単位を指定する。</para>
/// </summary>
public enum StringLengthUnit
{
    /// <summary>
    ///     <para xml:lang="en">UTF-16 code units (<see cref="string.Length"/>). Matches SQL Server <c>nvarchar(n)</c>. A surrogate pair counts as 2.</para>
    ///     <para xml:lang="ja">UTF-16コード単位（<see cref="string.Length"/>）。SQL Serverの<c>nvarchar(n)</c>と一致する。サロゲートペアは2文字と数える。</para>
    /// </summary>
    Utf16CodeUnit,

    /// <summary>
    ///     <para xml:lang="en">Unicode code points (<see cref="System.Text.Rune"/>). Matches PostgreSQL / MySQL <c>varchar(n)</c>.</para>
    ///     <para xml:lang="ja">Unicodeコードポイント（<see cref="System.Text.Rune"/>）。PostgreSQL・MySQLの<c>varchar(n)</c>と一致する。</para>
    /// </summary>
    CodePoint,

    /// <summary>
    ///     <para xml:lang="en">Text elements (grapheme clusters), i.e. characters as users perceive them. May exceed a database column limit counted in other units.</para>
    ///     <para xml:lang="ja">テキスト要素（書記素クラスタ）。利用者の見た目どおりの文字数。他の単位で数えるDBの桁数制限を超える場合がある点に注意。</para>
    /// </summary>
    TextElement,
}
