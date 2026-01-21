public static class EnumHelper
{
    // For non-nullable enums
    public static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct, Enum
    {
        return Enum.TryParse(value, out result);
    }

    // For nullable enums
    public static bool TryParseNullableEnum<TEnum>(string value, out TEnum? result) where TEnum : struct, Enum
    {
        result = null;

        if (string.IsNullOrEmpty(value) || value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
        {
            return true; // Treat empty or "NULL" as a valid nullable enum
        }

        if (Enum.TryParse(value, out TEnum temp))
        {
            result = temp;
            return true;
        }

        return false;
    }

    // For non-nullable enum mapping to string (DB storage)
    public static string MapEnumValueToDatabaseValue<TEnum>(TEnum enumValue) where TEnum : Enum
    {
        return enumValue.ToString();
    }

    // For nullable enum mapping to string (DB storage)
    public static string MapNullableEnumValueToDatabaseValue<TEnum>(TEnum? enumValue) where TEnum : struct, Enum
    {
        return enumValue.HasValue ? enumValue.Value.ToString() : "NULL";
    }
}