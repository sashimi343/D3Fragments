using System.ComponentModel;
using System.Globalization;

namespace D3Fragments;

// UuidV7に[TypeConverter]属性で適用するため、利用者による登録は不要。
// ASP.NET Core IdentityのキーやConfigurationのバインドなど、TypeDescriptor経由で文字列から変換する処理が使う
internal sealed class UuidV7TypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    // 形式が不正な場合とversion 7でない場合を区別せず、GuidConverterと同じFormatExceptionにそろえる
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string text)
        {
            return base.ConvertFrom(context, culture, value);
        }

        return UuidV7.TryParse(text, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new FormatException(ExceptionMessages.FormatException_InvalidUuidV7);
    }
}
