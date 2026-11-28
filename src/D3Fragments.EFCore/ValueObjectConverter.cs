using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace D3Fragments.EFCore;

// 値オブジェクトとラップしている値を相互に変換する。読み込み時は TValueObject.Create を通すため ValidateValue で検証される。
// 書き方を1つにするため公開せず、ValueObjectConversionExtensions の HasValueObjectConversion / HasValueObjectCollectionConversion からだけ使う
internal sealed class ValueObjectConverter<TValueObject, TPrimitive>()
    : ValueConverter<TValueObject, TPrimitive>(value => value.Value, value => Create(value))
    where TValueObject : IValueObject<TValueObject, TPrimitive>
{
    // 式ツリーは静的抽象メンバーを直接呼び出せないため、通常の静的メソッドを経由する
    private static TValueObject Create(TPrimitive value) => TValueObject.Create(value);
}
