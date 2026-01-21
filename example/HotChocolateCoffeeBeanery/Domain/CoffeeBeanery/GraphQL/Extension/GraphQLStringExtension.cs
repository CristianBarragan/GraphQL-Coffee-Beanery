namespace CoffeeBeanery.GraphQL.Helper
{
    public static class StringExtensions
    {
        public static bool Matches(this string a, string b) =>
            string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);

        public static string ToSnakeCase(this string value, string suffix = "")
            => $"{value}_{suffix}".ToLowerInvariant();

        public static string ToUpperCamelCase(this string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

        public static string Sanitize(this string s)
            => s.Replace("\"", "").Replace("'", "");
    }
}