namespace ReverseProxy.Store.EFCore;

internal static class ValueExtensions
{
    internal static int? ReadInt32(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        else
        {
            return int.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        }
    }

    internal static double? ReadDouble(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        else
        {
            return double.Parse(value, CultureInfo.InvariantCulture);
        }
    }

    internal static TimeSpan? ReadTimeSpan(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        else
        {
            return TimeSpan.ParseExact(value, "c", CultureInfo.InvariantCulture);
        }
    }

    internal static TEnum? ReadEnum<TEnum>(this string value) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        else
        {
            return Enum.Parse<TEnum>(value, ignoreCase: true);
        }
    }

    internal static bool? ReadBool(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        else
        {
            return bool.Parse(value);
        }
    }

    internal static Version ReadVersion(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        else
        {
            return Version.Parse(value + (value.Contains('.') ? "" : ".0"));
        }
    }
    internal static string[] ReadStringArray(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        else
        {
            return value.Split(',');
        }
    }

    internal static IReadOnlyDictionary<string, string> ReadStringDictionary<T>(this List<T> listValue) where T : KeyValueEntity
    {
        if (listValue is null || listValue.Count == 0)
        {
            return null;
        }
        return new ReadOnlyDictionary<string, string>(listValue.ToDictionary(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase));
    }
}
