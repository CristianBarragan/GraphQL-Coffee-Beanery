namespace CoffeeBeanery.GraphQL.Core.Mapping;

public static class GuidUtility
{
    public static Guid Create(string input)
    {
        if (string.IsNullOrEmpty(input))
            return Guid.Empty;

        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return new Guid(hash);
        }
    }
}