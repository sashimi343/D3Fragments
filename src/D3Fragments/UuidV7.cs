using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Represents a UUID version 7.</para>
///     <para xml:lang="ja">UUID version 7を表す構造体。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">This struct is a thin wrapper around the <see cref="Guid"/> type. This struct can only hold version 7 <see cref="Guid"/> values.</para>
///     <para xml:lang="ja">この構造体の実態は <see cref="Guid"/> 型の薄いラッパーである。version 7の <see cref="Guid"/> のみを保持できる。</para>
/// </remarks>
public readonly struct UuidV7 : IComparable, IComparable<UuidV7>, IEquatable<UuidV7>, IParsable<UuidV7>, ISpanFormattable, ISpanParsable<UuidV7>, IUtf8SpanFormattable, IUtf8SpanParsable<UuidV7>
{
    /// <summary>
    ///     <para xml:lang="en">Gets an empty <see cref="UuidV7"/> value.</para>
    ///     <para xml:lang="ja">空の <see cref="UuidV7"/> 値を取得する。</para>
    /// </summary>
    public static readonly UuidV7 Empty = default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private UuidV7(Guid value) => Value = value;

    /// <summary>
    ///     <para xml:lang="en">Gets the underlying <see cref="Guid"/> value of this <see cref="UuidV7"/>.</para>
    ///     <para xml:lang="ja">この <see cref="UuidV7"/> が保持する基になる <see cref="Guid"/> 値を取得する。</para>
    /// </summary>
    public readonly Guid Value { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets a value that indicates whether this instance is <see cref="Empty"/>.</para>
    ///     <para xml:lang="ja">このインスタンスが <see cref="Empty"/> かどうかを示す値を取得する。</para>
    /// </summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>
    ///     <para xml:lang="en">Creates a new <see cref="UuidV7"/> value.</para>
    ///     <para xml:lang="ja">新しい <see cref="UuidV7"/> 値を作成する。</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A new <see cref="UuidV7"/> value.</para>
    ///     <para xml:lang="ja">新しい <see cref="UuidV7"/> 値。</para>
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UuidV7 NewUuidV7() => new(Guid.CreateVersion7());

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="UuidV7"/> value from an existing <see cref="Guid"/> value.</para>
    ///     <para xml:lang="ja">既存の <see cref="Guid"/> 値から <see cref="UuidV7"/> 値を作成する。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The <see cref="Guid"/> value to convert.</para>
    ///     <para xml:lang="ja">変換対象の <see cref="Guid"/> 値。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A <see cref="UuidV7"/> value created from <paramref name="value"/>.</para>
    ///     <para xml:lang="ja"><paramref name="value"/> から作成された <see cref="UuidV7"/> 値。</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when <paramref name="value"/> is not a version 7 UUID.</para>
    ///     <para xml:lang="ja"><paramref name="value"/> が version 7 の UUID ではない場合にスローされる。</para>
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UuidV7 FromGuid(Guid value)
        => IsVersion7(value) ? new(value) : throw new ArgumentException(ExceptionMessages.ArgumentException_GuidNotVersion7, nameof(value));

    /// <summary>
    ///     <para xml:lang="en">Tries to create a <see cref="UuidV7"/> value from an existing <see cref="Guid"/> value.</para>
    ///     <para xml:lang="ja">既存の <see cref="Guid"/> 値から <see cref="UuidV7"/> 値の作成を試みる。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The <see cref="Guid"/> value to convert.</para>
    ///     <para xml:lang="ja">変換対象の <see cref="Guid"/> 値。</para>
    /// </param>
    /// <param name="result">
    ///     <para xml:lang="en">When this method returns <c>true</c>, contains the created <see cref="UuidV7"/> value. Otherwise, contains the default value.</para>
    ///     <para xml:lang="ja">このメソッドが <c>true</c> を返す場合は作成された <see cref="UuidV7"/> 値。<c>false</c> を返す場合は既定値。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><c>true</c> if conversion succeeds; otherwise, <c>false</c>.</para>
    ///     <para xml:lang="ja">変換に成功した場合は <c>true</c>。それ以外の場合は <c>false</c>。</para>
    /// </returns>
    public static bool TryFromGuid(Guid value, [MaybeNullWhen(false)] out UuidV7 result)
    {
        if (IsVersion7(value))
        {
            result = new(value);
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="UuidV7"/> value from the specified timestamp.</para>
    ///     <para xml:lang="ja">指定したタイムスタンプから <see cref="UuidV7"/> 値を作成する。</para>
    /// </summary>
    /// <param name="timestamp">
    ///     <para xml:lang="en">The timestamp used to create the UUID version 7 value.</para>
    ///     <para xml:lang="ja">UUID version 7 の値を作成するために使用するタイムスタンプ。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A created <see cref="UuidV7"/> value.</para>
    ///     <para xml:lang="ja">作成された <see cref="UuidV7"/> 値。</para>
    /// </returns>
    public static UuidV7 Create(DateTimeOffset timestamp) => FromGuid(Guid.CreateVersion7(timestamp));

    /// <summary>
    ///     <para xml:lang="en">Parses a string representation of a UUID version 7 value.</para>
    ///     <para xml:lang="ja">UUID version 7 値の文字列表現を解析する。</para>
    /// </summary>
    /// <param name="s">
    ///     <para xml:lang="en">The string representation to parse.</para>
    ///     <para xml:lang="ja">解析する文字列表現。</para>
    /// </param>
    /// <param name="provider">
    ///     <para xml:lang="en">An object that provides culture-specific formatting information for <paramref name="s"/>.</para>
    ///     <para xml:lang="ja"><paramref name="s"/> に対するカルチャ固有の書式情報を提供するオブジェクト。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A parsed <see cref="UuidV7"/> value.</para>
    ///     <para xml:lang="ja">解析された <see cref="UuidV7"/> 値。</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when the parsed value is not a version 7 UUID.</para>
    ///     <para xml:lang="ja">解析結果が version 7 の UUID ではない場合にスローされる。</para>
    /// </exception>
    public static UuidV7 Parse(string s, IFormatProvider? provider) => FromGuid(Guid.Parse(s, provider));

    /// <summary>
    ///     <para xml:lang="en">Parses a character span representation of a UUID version 7 value.</para>
    ///     <para xml:lang="ja">UUID version 7 値の文字スパン表現を解析する。</para>
    /// </summary>
    /// <param name="s">
    ///     <para xml:lang="en">The character span representation to parse.</para>
    ///     <para xml:lang="ja">解析する文字スパン表現。</para>
    /// </param>
    /// <param name="provider">
    ///     <para xml:lang="en">An object that provides culture-specific formatting information for <paramref name="s"/>.</para>
    ///     <para xml:lang="ja"><paramref name="s"/> に対するカルチャ固有の書式情報を提供するオブジェクト。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A parsed <see cref="UuidV7"/> value.</para>
    ///     <para xml:lang="ja">解析された <see cref="UuidV7"/> 値。</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when the parsed value is not a version 7 UUID.</para>
    ///     <para xml:lang="ja">解析結果が version 7 の UUID ではない場合にスローされる。</para>
    /// </exception>
    public static UuidV7 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => FromGuid(Guid.Parse(s, provider));

    /// <summary>
    ///     <para xml:lang="en">Parses a UTF-8 string representation of a UUID version 7 value.</para>
    ///     <para xml:lang="ja">UUID version 7 値の UTF-8 文字列表現を解析する。</para>
    /// </summary>
    /// <param name="utf8Text">
    ///     <para xml:lang="en">The UTF-8 string representation to parse.</para>
    ///     <para xml:lang="ja">解析する UTF-8 文字列表現。</para>
    /// </param>
    /// <param name="provider">
    ///     <para xml:lang="en">An object that provides culture-specific formatting information for <paramref name="utf8Text"/>.</para>
    ///     <para xml:lang="ja"><paramref name="utf8Text"/> に対するカルチャ固有の書式情報を提供するオブジェクト。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A parsed <see cref="UuidV7"/> value.</para>
    ///     <para xml:lang="ja">解析された <see cref="UuidV7"/> 値。</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when the parsed value is not a version 7 UUID.</para>
    ///     <para xml:lang="ja">解析結果が version 7 の UUID ではない場合にスローされる。</para>
    /// </exception>
    public static UuidV7 Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => FromGuid(Guid.Parse(utf8Text, provider));

    /// <summary>
    ///     <para xml:lang="en">Tries to parse a string representation of a UUID version 7 value.</para>
    ///     <para xml:lang="ja">UUID version 7 値の文字列表現の解析を試みる。</para>
    /// </summary>
    /// <param name="s">
    ///     <para xml:lang="en">The string representation to parse.</para>
    ///     <para xml:lang="ja">解析する文字列表現。</para>
    /// </param>
    /// <param name="provider">
    ///     <para xml:lang="en">An object that provides culture-specific formatting information for <paramref name="s"/>.</para>
    ///     <para xml:lang="ja"><paramref name="s"/> に対するカルチャ固有の書式情報を提供するオブジェクト。</para>
    /// </param>
    /// <param name="result">
    ///     <para xml:lang="en">When this method returns <c>true</c>, contains the parsed <see cref="UuidV7"/> value. Otherwise, contains the default value.</para>
    ///     <para xml:lang="ja">このメソッドが <c>true</c> を返す場合は解析された <see cref="UuidV7"/> 値。<c>false</c> を返す場合は既定値。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><c>true</c> if parsing succeeds and the value is a version 7 UUID; otherwise, <c>false</c>.</para>
    ///     <para xml:lang="ja">解析が成功し、値が version 7 の UUID である場合は <c>true</c>。それ以外の場合は <c>false</c>。</para>
    /// </returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out UuidV7 result)
    {
        if (Guid.TryParse(s, provider, out var guid) && IsVersion7(guid))
        {
            result = new(guid);
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Tries to parse a character span representation of a UUID version 7 value.</para>
    ///     <para xml:lang="ja">UUID version 7 値の文字スパン表現の解析を試みる。</para>
    /// </summary>
    /// <param name="s">
    ///     <para xml:lang="en">The character span representation to parse.</para>
    ///     <para xml:lang="ja">解析する文字スパン表現。</para>
    /// </param>
    /// <param name="provider">
    ///     <para xml:lang="en">An object that provides culture-specific formatting information for <paramref name="s"/>.</para>
    ///     <para xml:lang="ja"><paramref name="s"/> に対するカルチャ固有の書式情報を提供するオブジェクト。</para>
    /// </param>
    /// <param name="result">
    ///     <para xml:lang="en">When this method returns <c>true</c>, contains the parsed <see cref="UuidV7"/> value. Otherwise, contains the default value.</para>
    ///     <para xml:lang="ja">このメソッドが <c>true</c> を返す場合は解析された <see cref="UuidV7"/> 値。<c>false</c> を返す場合は既定値。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><c>true</c> if parsing succeeds and the value is a version 7 UUID; otherwise, <c>false</c>.</para>
    ///     <para xml:lang="ja">解析が成功し、値が version 7 の UUID である場合は <c>true</c>。それ以外の場合は <c>false</c>。</para>
    /// </returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out UuidV7 result)
    {
        if (Guid.TryParse(s, provider, out var guid) && IsVersion7(guid))
        {
            result = new(guid);
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Tries to parse a UTF-8 string representation of a UUID version 7 value.</para>
    ///     <para xml:lang="ja">UUID version 7 値の UTF-8 文字列表現の解析を試みる。</para>
    /// </summary>
    /// <param name="utf8Text">
    ///     <para xml:lang="en">The UTF-8 string representation to parse.</para>
    ///     <para xml:lang="ja">解析する UTF-8 文字列表現。</para>
    /// </param>
    /// <param name="provider">
    ///     <para xml:lang="en">An object that provides culture-specific formatting information for <paramref name="utf8Text"/>.</para>
    ///     <para xml:lang="ja"><paramref name="utf8Text"/> に対するカルチャ固有の書式情報を提供するオブジェクト。</para>
    /// </param>
    /// <param name="result">
    ///     <para xml:lang="en">When this method returns <c>true</c>, contains the parsed <see cref="UuidV7"/> value. Otherwise, contains the default value.</para>
    ///     <para xml:lang="ja">このメソッドが <c>true</c> を返す場合は解析された <see cref="UuidV7"/> 値。<c>false</c> を返す場合は既定値。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><c>true</c> if parsing succeeds and the value is a version 7 UUID; otherwise, <c>false</c>.</para>
    ///     <para xml:lang="ja">解析が成功し、値が version 7 の UUID である場合は <c>true</c>。それ以外の場合は <c>false</c>。</para>
    /// </returns>
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out UuidV7 result)
    {
        if (Guid.TryParse(utf8Text, provider, out var guid) && IsVersion7(guid))
        {
            result = new(guid);
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Compares this instance with a specified object and returns an indication of their relative values.</para>
    ///     <para xml:lang="ja">このインスタンスを指定したオブジェクトと比較し、相対値を示す値を返す。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The object to compare, or <c>null</c>.</para>
    ///     <para xml:lang="ja">比較対象のオブジェクト、または <c>null</c>。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A signed integer that indicates the relative order of this instance and <paramref name="value"/>.</para>
    ///     <para xml:lang="ja">このインスタンスと <paramref name="value"/> の相対順序を示す符号付き整数。</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when <paramref name="value"/> is not <c>null</c> and is not a <see cref="UuidV7"/> value.</para>
    ///     <para xml:lang="ja"><paramref name="value"/> が <c>null</c> ではなく、かつ <see cref="UuidV7"/> 値でもない場合にスローされる。</para>
    /// </exception>
    public int CompareTo(object? value)
    {
        if (value == null)
        {
            return 1;
        }
        if (value is not UuidV7 other)
        {
            throw new ArgumentException(ExceptionMessages.ArgumentException_MustBeUuidV7, nameof(value));
        }
        return CompareTo(other);
    }

    /// <summary>
    ///     <para xml:lang="en">Compares this instance with a specified <see cref="UuidV7"/> value and returns an indication of their relative values.</para>
    ///     <para xml:lang="ja">このインスタンスを指定した <see cref="UuidV7"/> 値と比較し、相対値を示す値を返す。</para>
    /// </summary>
    /// <param name="other">
    ///     <para xml:lang="en">The <see cref="UuidV7"/> value to compare.</para>
    ///     <para xml:lang="ja">比較対象の <see cref="UuidV7"/> 値。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A signed integer that indicates the relative order of this instance and <paramref name="other"/>.</para>
    ///     <para xml:lang="ja">このインスタンスと <paramref name="other"/> の相対順序を示す符号付き整数。</para>
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(UuidV7 other) => Value.CompareTo(other.Value);

    /// <summary>
    ///     <para xml:lang="en">Determines whether this instance and a specified <see cref="UuidV7"/> value represent the same value.</para>
    ///     <para xml:lang="ja">このインスタンスと指定した <see cref="UuidV7"/> 値が同じ値を表すかどうかを判定する。</para>
    /// </summary>
    /// <param name="other">
    ///     <para xml:lang="en">The <see cref="UuidV7"/> value to compare.</para>
    ///     <para xml:lang="ja">比較対象の <see cref="UuidV7"/> 値。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><c>true</c> if the values are equal; otherwise, <c>false</c>.</para>
    ///     <para xml:lang="ja">値が等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</para>
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(UuidV7 other) => Value.Equals(other.Value);

    /// <summary>
    ///     <para xml:lang="en">Determines whether this instance and a specified object represent the same value.</para>
    ///     <para xml:lang="ja">このインスタンスと指定したオブジェクトが同じ値を表すかどうかを判定する。</para>
    /// </summary>
    /// <param name="obj">
    ///     <para xml:lang="en">The object to compare, or <c>null</c>.</para>
    ///     <para xml:lang="ja">比較対象のオブジェクト、または <c>null</c>。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><c>true</c> if the values are equal; otherwise, <c>false</c>.</para>
    ///     <para xml:lang="ja">値が等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</para>
    /// </returns>
    public override bool Equals(object? obj) => obj is UuidV7 other && Equals(other);

    /// <summary>
    ///     <para xml:lang="en">Returns the hash code for this instance.</para>
    ///     <para xml:lang="ja">このインスタンスのハッシュコードを返す。</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">The hash code for this instance.</para>
    ///     <para xml:lang="ja">このインスタンスのハッシュコード。</para>
    /// </returns>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    ///     <para xml:lang="en">Converts the value of this instance to its string representation by using the specified format and format provider.</para>
    ///     <para xml:lang="ja">指定した書式指定子と書式プロバイダーを使用して、このインスタンスの値を文字列表現に変換する。</para>
    /// </summary>
    /// <param name="format">
    ///     <para xml:lang="en">A single format specifier that indicates how to format this value. Supported values are "N", "D", "B", "P", and "X". If <c>null</c> or empty, "D" is used.</para>
    ///     <para xml:lang="ja">この値の書式を示す単一の書式指定子。指定できる値は "N"、"D"、"B"、"P"、"X"。<c>null</c> または空文字列の場合は "D" が使用される。</para>
    /// </param>
    /// <param name="formatProvider">
    ///     <para xml:lang="en">An object that provides culture-specific formatting information.</para>
    ///     <para xml:lang="ja">カルチャ固有の書式情報を提供するオブジェクト。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The string representation of this value in the specified format.</para>
    ///     <para xml:lang="ja">指定した形式で表現されたこの値の文字列。</para>
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>
    ///     <para xml:lang="en">Tries to format the value of this instance into the provided character span.</para>
    ///     <para xml:lang="ja">このインスタンスの値を指定された文字スパンに書式化できるかを試みる。</para>
    /// </summary>
    /// <param name="destination">
    ///     <para xml:lang="en">The destination buffer that receives the formatted characters.</para>
    ///     <para xml:lang="ja">書式化された文字を受け取る出力バッファー。</para>
    /// </param>
    /// <param name="charsWritten">
    ///     <para xml:lang="en">When this method returns, contains the number of characters written to <paramref name="destination"/>.</para>
    ///     <para xml:lang="ja">このメソッドが戻るときに <paramref name="destination"/> に書き込まれた文字数。</para>
    /// </param>
    /// <param name="format">
    ///     <para xml:lang="en">A format specifier.</para>
    ///     <para xml:lang="ja">書式指定子。</para>
    /// </param>
    /// <param name="provider">
    ///     <para xml:lang="en">An object that supplies culture-specific formatting information.</para>
    ///     <para xml:lang="ja">カルチャ固有の書式情報を提供するオブジェクト。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><c>true</c> if formatting succeeds; otherwise, <c>false</c>.</para>
    ///     <para xml:lang="ja">書式化に成功した場合は <c>true</c>。それ以外の場合は <c>false</c>。</para>
    /// </returns>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para xml:lang="en">Tries to format the value of this instance into the provided UTF-8 byte span.</para>
    ///     <para xml:lang="ja">このインスタンスの値を指定された UTF-8 バイトスパンに書式化できるかを試みる。</para>
    /// </summary>
    /// <param name="utf8Destination">
    ///     <para xml:lang="en">The destination buffer that receives the formatted UTF-8 bytes.</para>
    ///     <para xml:lang="ja">書式化された UTF-8 バイト列を受け取る出力バッファー。</para>
    /// </param>
    /// <param name="bytesWritten">
    ///     <para xml:lang="en">When this method returns, contains the number of bytes written to <paramref name="utf8Destination"/>.</para>
    ///     <para xml:lang="ja">このメソッドが戻るときに <paramref name="utf8Destination"/> に書き込まれたバイト数。</para>
    /// </param>
    /// <param name="format">
    ///     <para xml:lang="en">A format specifier.</para>
    ///     <para xml:lang="ja">書式指定子。</para>
    /// </param>
    /// <param name="provider">
    ///     <para xml:lang="en">An object that supplies culture-specific formatting information.</para>
    ///     <para xml:lang="ja">カルチャ固有の書式情報を提供するオブジェクト。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><c>true</c> if formatting succeeds; otherwise, <c>false</c>.</para>
    ///     <para xml:lang="ja">書式化に成功した場合は <c>true</c>。それ以外の場合は <c>false</c>。</para>
    /// </returns>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsVersion7(Guid value)
    {
        return value.Version == 7;
    }
}
