namespace D3Fragments.Validations;

/// <summary>
///     <para xml:lang="en">Kinds of characters allowed by <see cref="StringValidator.OnlyCharacters"/>. Combine flags to allow any of them.</para>
///     <para xml:lang="ja"><see cref="StringValidator.OnlyCharacters"/>で許可する文字種。フラグを組み合わせるといずれかに該当する文字を許可する（OR条件）。</para>
/// </summary>
[Flags]
public enum CharacterKinds
{
    /// <summary>
    ///     <para xml:lang="en">No characters.</para>
    ///     <para xml:lang="ja">なし。</para>
    /// </summary>
    None = 0,

    /// <summary>
    ///     <para xml:lang="en">ASCII digits (<c>0-9</c>).</para>
    ///     <para xml:lang="ja">半角数字（<c>0-9</c>）。</para>
    /// </summary>
    AsciiDigit = 1 << 0,

    /// <summary>
    ///     <para xml:lang="en">ASCII uppercase letters (<c>A-Z</c>).</para>
    ///     <para xml:lang="ja">半角英大文字（<c>A-Z</c>）。</para>
    /// </summary>
    AsciiUppercase = 1 << 1,

    /// <summary>
    ///     <para xml:lang="en">ASCII lowercase letters (<c>a-z</c>).</para>
    ///     <para xml:lang="ja">半角英小文字（<c>a-z</c>）。</para>
    /// </summary>
    AsciiLowercase = 1 << 2,

    /// <summary>
    ///     <para xml:lang="en">ASCII printable symbols (<c>!</c> to <c>~</c>, excluding letters and digits).</para>
    ///     <para xml:lang="ja">半角記号（<c>!</c>～<c>~</c>のうち英数字以外）。</para>
    /// </summary>
    AsciiSymbol = 1 << 3,

    /// <summary>
    ///     <para xml:lang="en">ASCII space (U+0020).</para>
    ///     <para xml:lang="ja">半角スペース（U+0020）。</para>
    /// </summary>
    AsciiSpace = 1 << 4,

    /// <summary>
    ///     <para xml:lang="en">Hiragana (U+3041-U+3096, U+309D-U+309F) and the prolonged sound mark (U+30FC).</para>
    ///     <para xml:lang="ja">ひらがな（U+3041～U+3096、U+309D～U+309F）および長音記号「ー」（U+30FC）。</para>
    /// </summary>
    Hiragana = 1 << 5,

    /// <summary>
    ///     <para xml:lang="en">Full-width katakana (U+30A1-U+30FF), including the middle dot and the prolonged sound mark. Half-width katakana is not included.</para>
    ///     <para xml:lang="ja">全角カタカナ（U+30A1～U+30FF）。中黒「・」・長音記号「ー」を含む。半角カタカナは含まない。</para>
    /// </summary>
    FullWidthKatakana = 1 << 6,

    /// <summary>
    ///     <para xml:lang="en">Half-width katakana (U+FF65-U+FF9F), including the half-width middle dot, prolonged sound mark and (semi-)voiced sound marks.</para>
    ///     <para xml:lang="ja">半角カタカナ（U+FF65～U+FF9F）。半角の中黒「･」・長音「ｰ」・濁点「ﾞ」・半濁点「ﾟ」を含む。</para>
    /// </summary>
    HalfWidthKatakana = 1 << 7,

    /// <summary>
    ///     <para xml:lang="en">ASCII letters.</para>
    ///     <para xml:lang="ja">半角英字。</para>
    /// </summary>
    AsciiLetter = AsciiUppercase | AsciiLowercase,

    /// <summary>
    ///     <para xml:lang="en">ASCII letters and digits.</para>
    ///     <para xml:lang="ja">半角英数字。</para>
    /// </summary>
    AsciiAlphanumeric = AsciiLetter | AsciiDigit,

    /// <summary>
    ///     <para xml:lang="en">ASCII printable characters (U+0020-U+007E).</para>
    ///     <para xml:lang="ja">ASCII印字可能文字（U+0020～U+007E）。</para>
    /// </summary>
    AsciiPrintable = AsciiAlphanumeric | AsciiSymbol | AsciiSpace,
}
