using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Extension;

public static class MappingValidator
{
    public static void Validate<M>(M rootInstance, string rootAlias)
        where M : class
    {
        var discoveredAliases = new HashSet<string>();

        Traverse(rootInstance, rootAlias, discoveredAliases);

        var registeredAliases = MappingRegistry.Registry.Keys.ToHashSet();

        // ❌ Missing mappings
        var missing = discoveredAliases
            .Where(a => !registeredAliases.Contains(a))
            .ToList();

        // ⚠️ Unused mappings
        var unused = registeredAliases
            .Where(a => !discoveredAliases.Contains(a))
            .ToList();

        PrintResults(missing, unused);
    }

    private static void Traverse(
        object instance,
        string currentAlias,
        HashSet<string> discovered)
    {
        if (discovered.Contains(currentAlias))
            return;

        discovered.Add(currentAlias);

        var type = instance.GetType();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (GraphQLFieldExtension.IsPrimitiveType(prop.PropertyType))
                continue;

            var childInstance = TryCreateChildInstance(prop);
            if (childInstance == null)
                continue;

            var childAlias = string.IsNullOrEmpty(currentAlias)
                ? prop.Name
                : $"{currentAlias}.{prop.Name}";

            Traverse(childInstance, childAlias, discovered);
        }
    }

    private static object? TryCreateChildInstance(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (typeof(System.Collections.IList).IsAssignableFrom(type))
        {
            var itemType = type.GenericTypeArguments.FirstOrDefault();
            return itemType != null ? Activator.CreateInstance(itemType) : null;
        }

        if (type.IsClass && type != typeof(string))
            return Activator.CreateInstance(type);

        return null;
    }

    private static void PrintResults(List<string> missing, List<string> unused)
    {
        Console.WriteLine("\n====== MAPPING VALIDATION ======\n");

        if (missing.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Missing Mappings:");
            foreach (var m in missing)
                Console.WriteLine($"   - {m}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ No missing mappings");
        }

        Console.ResetColor();

        Console.WriteLine();

        if (unused.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️ Unused Mappings:");
            foreach (var u in unused)
                Console.WriteLine($"   - {u}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ No unused mappings");
        }

        Console.ResetColor();
        Console.WriteLine("\n================================\n");
    }
}