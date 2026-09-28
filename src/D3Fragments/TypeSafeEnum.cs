using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Base class for a type-safe enumeration. Unlike a built-in <see langword="enum"/>, only the values defined in the derived class exist (an undefined value cannot be forged by casting), and values can have behavior (methods and properties).</para>
///     <para xml:lang="ja">型安全な列挙型の基底クラス。組み込みの<see langword="enum"/>と異なり、派生クラスで定義した値しか存在せず（キャストによる不正値を作れない）、値ごとにメソッド等の振る舞いを持てる。</para>
/// </summary>
/// <typeparam name="TSelf">
///     <para xml:lang="en">The derived type itself.</para>
///     <para xml:lang="ja">派生クラス自身の型。</para>
/// </typeparam>
/// <typeparam name="TKey">
///     <para xml:lang="en">The type of the key that identifies each value (e.g. the value persisted to a database).</para>
///     <para xml:lang="ja">各値を識別するキー（DBに永続化する値等）の型。</para>
/// </typeparam>
/// <remarks>
///     <para xml:lang="en">Define each value as a <see langword="static readonly"/> member of <typeparamref name="TSelf"/> and make its constructor <see langword="private"/>, so that no value can be created outside the definition. For per-value behavior, make <typeparamref name="TSelf"/> abstract and define private nested subclasses. Keys and names must be unique. Equality is reference equality, since each value exists as a single instance.</para>
///     <para xml:lang="ja">各値は<typeparamref name="TSelf"/>の<see langword="static readonly"/>メンバとして定義し、定義以外で値を生成できないようコンストラクタは<see langword="private"/>にすること。値ごとに振る舞いを変える場合は<typeparamref name="TSelf"/>を抽象クラスとし、privateな入れ子クラスで各値を定義する。キーと名前は一意であること。各値は単一のインスタンスとして存在するため、等価性は参照の等価性で判定する。</para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class Currency : TypeSafeEnumBase&lt;Currency, string&gt;
/// {
///     public static readonly Currency Jpy = new("JPY", "Japanese Yen", 0);
///     public static readonly Currency Usd = new("USD", "US Dollar", 2);
///
///     private Currency(string key, string name, int decimalPlaces) : base(key, name) => DecimalPlaces = decimalPlaces;
///
///     public int DecimalPlaces { get; }
/// }
/// </code>
/// </example>
public abstract class TypeSafeEnum<TSelf, TKey>
    where TSelf : TypeSafeEnum<TSelf, TKey>
    where TKey : notnull
{
    private static readonly List<TSelf> DefinedValues = [];
    private static readonly List<TKey> DefinedKeys = [];
    private static readonly IReadOnlyList<TSelf> ValuesView = DefinedValues.AsReadOnly();
    private static readonly IReadOnlyList<TKey> KeysView = DefinedKeys.AsReadOnly();
    private static readonly Dictionary<TKey, TSelf> ValuesByKey = [];
    private static readonly Dictionary<string, TSelf> ValuesByName = new(StringComparer.Ordinal);
    private static bool _initialized;

    /// <summary>
    ///     <para xml:lang="en">Initializes a new value using the string representation of <paramref name="key"/> as its name.</para>
    ///     <para xml:lang="ja">キーの文字列表現を名前として、新しい値を初期化する。</para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">The key of the value.</para>
    ///     <para xml:lang="ja">値のキー。</para>
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when the key or the name is already defined.</para>
    ///     <para xml:lang="ja">キーまたは名前がすでに定義されている場合にスローされる。</para>
    /// </exception>
    protected TypeSafeEnum(TKey key) : this(key, NameOf(key))
    {
    }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new value with the specified key and name.</para>
    ///     <para xml:lang="ja">キーと名前を指定して、新しい値を初期化する。</para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">The key of the value.</para>
    ///     <para xml:lang="ja">値のキー。</para>
    /// </param>
    /// <param name="name">
    ///     <para xml:lang="en">The name (display name) of the value.</para>
    ///     <para xml:lang="ja">値の名前（表示名）。</para>
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when the key or the name is already defined.</para>
    ///     <para xml:lang="ja">キーまたは名前がすでに定義されている場合にスローされる。</para>
    /// </exception>
    protected TypeSafeEnum(TKey key, string name)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (ValuesByKey.ContainsKey(key))
        {
            throw new ArgumentException(Format(ExceptionMessages.TypeSafeEnum_DuplicateKey, key), nameof(key));
        }
        if (ValuesByName.ContainsKey(name))
        {
            throw new ArgumentException(Format(ExceptionMessages.TypeSafeEnum_DuplicateName, name), nameof(name));
        }

        Key = key;
        Name = name;

        var self = (TSelf)this;
        DefinedValues.Add(self);
        DefinedKeys.Add(key);
        ValuesByKey.Add(key, self);
        ValuesByName.Add(name, self);
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the key of this value.</para>
    ///     <para xml:lang="ja">この値のキーを取得する。</para>
    /// </summary>
    public TKey Key { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the name (display name) of this value.</para>
    ///     <para xml:lang="ja">この値の名前（表示名）を取得する。</para>
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets all defined values in definition order.</para>
    ///     <para xml:lang="ja">定義されているすべての値を定義順で取得する。</para>
    /// </summary>
    public static IReadOnlyList<TSelf> Values => Initialized(ValuesView);

    /// <summary>
    ///     <para xml:lang="en">Gets the keys of all defined values in definition order.</para>
    ///     <para xml:lang="ja">定義されているすべての値のキーを定義順で取得する。</para>
    /// </summary>
    public static IReadOnlyList<TKey> Keys => Initialized(KeysView);

    /// <summary>
    ///     <para xml:lang="en">Gets the number of defined values.</para>
    ///     <para xml:lang="ja">定義されている値の総数を取得する。</para>
    /// </summary>
    public static int Count => Initialized(DefinedValues).Count;

    /// <summary>
    ///     <para xml:lang="en">Gets the value with the specified key.</para>
    ///     <para xml:lang="ja">指定したキーに対応する値を取得する。</para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">The key to look up.</para>
    ///     <para xml:lang="ja">検索するキー。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The value with the specified key.</para>
    ///     <para xml:lang="ja">キーに対応する値。</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when no value is defined with <paramref name="key"/>.</para>
    ///     <para xml:lang="ja"><paramref name="key"/>に対応する値が定義されていない場合にスローされる。</para>
    /// </exception>
    public static TSelf Parse(TKey key) =>
        TryParse(key, out var value) ? value : throw new ArgumentException(UndefinedKeyMessage(key), nameof(key));

    /// <summary>
    ///     <para xml:lang="en">Attempts to get the value with the specified key.</para>
    ///     <para xml:lang="ja">指定したキーに対応する値の取得を試みる。</para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">The key to look up.</para>
    ///     <para xml:lang="ja">検索するキー。</para>
    /// </param>
    /// <param name="value">
    ///     <para xml:lang="en">The value found, or <see langword="null"/> when not found.</para>
    ///     <para xml:lang="ja">取得した値。見つからない場合は<see langword="null"/>。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><see langword="true"/> when the value was found.</para>
    ///     <para xml:lang="ja">取得に成功した場合は<see langword="true"/>。</para>
    /// </returns>
    public static bool TryParse(TKey key, [NotNullWhen(true)] out TSelf? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Initialized(ValuesByKey).TryGetValue(key, out value);
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the value with the specified key as a <see cref="Result{T}"/>. Returns a validation error when no value is defined with <paramref name="key"/>; useful for keys received as external input.</para>
    ///     <para xml:lang="ja">指定したキーに対応する値を<see cref="Result{T}"/>として取得する。値が定義されていない場合はバリデーションエラーを返す。外部入力で受け取ったキーの変換に使用する。</para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">The key to look up.</para>
    ///     <para xml:lang="ja">検索するキー。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A success result holding the value, or a failure result holding a validation error.</para>
    ///     <para xml:lang="ja">成功時は値を保持する結果、失敗時はバリデーションエラーを保持する結果。</para>
    /// </returns>
    public static Result<TSelf> ParseAsResult(TKey key) =>
        TryParse(key, out var value) ? Result<TSelf>.Success(value) : Result<TSelf>.Failure(Error.Validation(UndefinedKeyMessage(key)));

    /// <summary>
    ///     <para xml:lang="en">Returns the name of this value.</para>
    ///     <para xml:lang="ja">この値の名前を返す。</para>
    /// </summary>
    public override string ToString() => Name;

    internal static bool TryParseByName(string name, [NotNullWhen(true)] out TSelf? value) =>
        Initialized(ValuesByName).TryGetValue(name, out value);

    internal static string UndefinedKeyMessage(TKey key) => Format(ExceptionMessages.TypeSafeEnum_UndefinedKey, key);

    internal static string UndefinedNameMessage(string name) => Format(ExceptionMessages.TypeSafeEnum_UndefinedName, name);

    // 値は派生クラスの静的初期化で登録されるため、参照前に確実に実行させる
    // （値の定義をprivateフィールドにしている場合でも動作するよう、特定のフィールドには依存しない）
    private static T Initialized<T>(T registry)
    {
        if (!_initialized)
        {
            RuntimeHelpers.RunClassConstructor(typeof(TSelf).TypeHandle);
            _initialized = true;
        }
        return registry;
    }

    private static string NameOf(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key.ToString() ?? string.Empty;
    }

    private static string Format(string format, object? arg) =>
        string.Format(CultureInfo.CurrentCulture, format, arg, typeof(TSelf).Name);
}
