using System.Globalization;

namespace D3Fragments.Validations;

/// <summary>
///     <para xml:lang="en">Base class for defining and applying validation rules to a single value. Rules are declared once (typically at initialization) and the validator is then used as immutable; it may be shared, e.g. as a <see langword="static readonly"/> field.</para>
///     <para xml:lang="ja">単一の値に対するバリデーションルールを定義・適用する基底クラス。ルールは（通常は初期化時に）一度だけ宣言し、以後は不変として扱う。<see langword="static readonly"/>フィールド等で共有してよい。</para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">The type of the value to validate.</para>
///     <para xml:lang="ja">検証対象の値の型。</para>
/// </typeparam>
/// <remarks>
///     <para xml:lang="en">Messages are composite format strings: <c>{0}</c> is the display name and <c>{1}</c>, <c>{2}</c>... are the rule arguments. Default messages are looked up from resources at validation time using <see cref="CultureInfo.CurrentUICulture"/>.</para>
///     <para xml:lang="ja">メッセージは複合書式文字列で、<c>{0}</c>が表示名、<c>{1}</c>, <c>{2}</c>...がルールの引数となる。既定メッセージは検証実行時に<see cref="CultureInfo.CurrentUICulture"/>でリソースから取得される。</para>
/// </remarks>
public abstract class ValidatorBase<T>
    where T : notnull
{
    private readonly List<Func<T, string, Error?>> _rules = [];
    private Func<string, Error>? _missingError;

    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="ValidatorBase{T}"/>.</para>
    ///     <para xml:lang="ja">新しい <see cref="ValidatorBase{T}"/> を初期化する。</para>
    /// </summary>
    /// <param name="displayName">
    ///     <para xml:lang="en">The display name of the value, used in error messages.</para>
    ///     <para xml:lang="ja">エラーメッセージに使用する値の表示名。</para>
    /// </param>
    /// <param name="target">
    ///     <para xml:lang="en">An optional key set to <see cref="Error.Target"/> of the resulting errors, to associate them with an API field or UI control.</para>
    ///     <para xml:lang="ja">生成するエラーの<see cref="Error.Target"/>に設定するキー（省略可）。APIレスポンスの項目や画面コントロールとの紐づけに使用する。</para>
    /// </param>
    protected ValidatorBase(string displayName, string? target = null)
    {
        DisplayName = displayName;
        Target = target;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the display name of the value, used in error messages.</para>
    ///     <para xml:lang="ja">エラーメッセージに使用する値の表示名を取得する。</para>
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the key set to <see cref="Error.Target"/> of the resulting errors.</para>
    ///     <para xml:lang="ja">生成するエラーの<see cref="Error.Target"/>に設定するキーを取得する。</para>
    /// </summary>
    public string? Target { get; }

    /// <summary>
    ///     <para xml:lang="en">Makes a missing value an error. Without this, a missing value skips all rules and succeeds.</para>
    ///     <para xml:lang="ja">値が存在しない場合をエラーとする。これを呼ばない場合、値が存在しなければ全ルールをスキップして成功となる。</para>
    /// </summary>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message, or <see langword="null"/> to use <paramref name="defaultMessage"/>.</para>
    ///     <para xml:lang="ja">カスタムメッセージ。<see langword="null"/>の場合は<paramref name="defaultMessage"/>を使用する。</para>
    /// </param>
    /// <param name="defaultMessage">
    ///     <para xml:lang="en">Provides the default message at validation time.</para>
    ///     <para xml:lang="ja">検証実行時に既定メッセージを返す関数。</para>
    /// </param>
    protected void SetRequired(string? message, Func<string> defaultMessage) =>
        _missingError = displayName => CreateError(message ?? defaultMessage(), displayName, []);

    /// <summary>
    ///     <para xml:lang="en">Adds a rule applied to a present (non-missing) value.</para>
    ///     <para xml:lang="ja">値が存在する場合に適用するルールを追加する。</para>
    /// </summary>
    /// <param name="isValid">
    ///     <para xml:lang="en">Returns <see langword="true"/> when the value is valid.</para>
    ///     <para xml:lang="ja">値が妥当な場合に<see langword="true"/>を返す関数。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message, or <see langword="null"/> to use <paramref name="defaultMessage"/>.</para>
    ///     <para xml:lang="ja">カスタムメッセージ。<see langword="null"/>の場合は<paramref name="defaultMessage"/>を使用する。</para>
    /// </param>
    /// <param name="defaultMessage">
    ///     <para xml:lang="en">Provides the default message at validation time.</para>
    ///     <para xml:lang="ja">検証実行時に既定メッセージを返す関数。</para>
    /// </param>
    /// <param name="args">
    ///     <para xml:lang="en">The rule arguments embedded into the message as <c>{1}</c>, <c>{2}</c>...</para>
    ///     <para xml:lang="ja">メッセージに<c>{1}</c>, <c>{2}</c>...として埋め込むルールの引数。</para>
    /// </param>
    protected void AddRule(Func<T, bool> isValid, string? message, Func<string> defaultMessage, params object?[] args) =>
        _rules.Add((value, displayName) => isValid(value) ? null : CreateError(message ?? defaultMessage(), displayName, args));

    /// <summary>
    ///     <para xml:lang="en">Validates the value against the declared rules. All rules are evaluated and all errors are returned.</para>
    ///     <para xml:lang="ja">宣言されたルールで値を検証する。全ルールを評価し、すべてのエラーを返す。</para>
    /// </summary>
    /// <param name="hasValue">
    ///     <para xml:lang="en">Whether the value is present. When <see langword="false"/>, only the required rule is evaluated.</para>
    ///     <para xml:lang="ja">値が存在するかどうか。<see langword="false"/>の場合は必須チェックのみ評価する。</para>
    /// </param>
    /// <param name="value">
    ///     <para xml:lang="en">The value to validate. Ignored when <paramref name="hasValue"/> is <see langword="false"/>.</para>
    ///     <para xml:lang="ja">検証対象の値。<paramref name="hasValue"/>が<see langword="false"/>の場合は無視される。</para>
    /// </param>
    /// <param name="displayName">
    ///     <para xml:lang="en">The display name used in error messages.</para>
    ///     <para xml:lang="ja">エラーメッセージに使用する表示名。</para>
    /// </param>
    protected Result Validate(bool hasValue, T value, string displayName)
    {
        if (!hasValue)
        {
            return _missingError is null ? Result.Success() : Result.Failure(_missingError(displayName));
        }

        var errors = _rules.Select(rule => rule(value, displayName)).OfType<Error>().ToArray();
        return errors.Length == 0 ? Result.Success() : Result.Failure(errors);
    }

    private Error CreateError(string format, string displayName, object?[] args) =>
        Error.Validation(string.Format(CultureInfo.CurrentCulture, format, [displayName, .. args]), target: Target);
}
