using System.Globalization;

using Microsoft.EntityFrameworkCore.Metadata;

namespace D3Fragments.EFCore;

// 値オブジェクトをマッピングする拡張メソッドが共通で使う検査
internal static class ValueObjectMapping
{
    // type が自身を TSelf として実装している IValueObject<,> を返す。実装していなければ、呼び出したメソッド名を含む例外を送出する
    internal static Type GetValueObjectInterface(Type type, string methodName) =>
        type.GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IValueObject<,>) && x.GenericTypeArguments[0] == type)
        ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ExceptionMessages.InvalidOperationException_NotValueObject, type, methodName));

    // setter もバッキングフィールドもないと、モデル構築は通るのに保存・読み込み時に原因の分かりにくい例外になるため、構成時に検出する。
    // バッキングフィールドの照合は EF Core の規約に任せる（ComplexCollection / PrimitiveCollection の呼び出し直後に設定される）
    internal static void EnsureWritable(IReadOnlyPropertyBase property)
    {
        if (property.FieldInfo is null && property.PropertyInfo?.SetMethod is null)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                ExceptionMessages.InvalidOperationException_CollectionPropertyNotWritable,
                $"{property.DeclaringType.ClrType.Name}.{property.Name}"));
        }
    }
}
