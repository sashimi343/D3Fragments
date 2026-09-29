using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">An EF Core value converter that persists a <see cref="UuidV7"/> as a <see cref="Guid"/>. Each database provider maps <see cref="Guid"/> to its native type (e.g. <c>uuid</c> on PostgreSQL, <c>uniqueidentifier</c> on SQL Server).</para>
///     <para xml:lang="ja"><see cref="UuidV7"/>を<see cref="Guid"/>として永続化するEF Core用の値コンバーター。<see cref="Guid"/>は各DBプロバイダがネイティブ型（PostgreSQLの<c>uuid</c>、SQL Serverの<c>uniqueidentifier</c>等）にマッピングする。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en"><see cref="D3DbContext"/> registers this converter for all <see cref="UuidV7"/> properties. Reading a value that is not a version 7 UUID from the database throws an <see cref="ArgumentException"/>.</para>
///     <para xml:lang="ja"><see cref="D3DbContext"/>はすべての<see cref="UuidV7"/>プロパティにこのコンバーターを登録する。DBからversion 7以外のUUIDを読み取った場合は<see cref="ArgumentException"/>をスローする。</para>
/// </remarks>
public sealed class UuidV7Converter : ValueConverter<UuidV7, Guid>
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new converter.</para>
    ///     <para xml:lang="ja">新しいコンバーターを初期化する。</para>
    /// </summary>
    public UuidV7Converter() : base(value => value.Value, guid => UuidV7.FromGuid(guid))
    {
    }
}
