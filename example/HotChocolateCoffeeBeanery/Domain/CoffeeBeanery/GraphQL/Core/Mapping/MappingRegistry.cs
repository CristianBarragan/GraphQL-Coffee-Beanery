using CoffeeBeanery.GraphQL.Core.Mapping;

public static class MappingRegistry
{
    public static Dictionary<string, NodeMap> Registry { get; } = new();
    private static int _idCounter = 0;

    public static NodeMap Register(
        Type modelType,
        Type? entityType,
        NodeMap map,
        string alias)
    {
        map.ModelType  = modelType;
        map.EntityType = entityType;

        var simpleKey = alias;

        // FIX: previously a collision (two distinct Register() calls resolving to the same
        // RegistrationKey) was logged but then the code proceeded to overwrite the existing
        // entry unconditionally anyway (`Registry[simpleKey] = map;` ran regardless). That
        // meant the warning was purely cosmetic - the "first" registration's FieldMaps,
        // GraphMap, ModelToEntity, everything, was discarded every time, and whichever
        // Register() call happened to run last silently won. This is exactly what was
        // breaking CustomerCustomerEdge: if anything calls Register() for the same key more
        // than once (whether due to the mapping pipeline running more than once, or a
        // second/duplicate call site nobody's found yet), the real mapping kept getting
        // clobbered no matter how many call-site guards were added upstream.
        //
        // Now: on a genuine collision (same key, different NodeMap instance), KEEP the
        // existing registration and discard the new one, instead of the other way round.
        // This makes registration idempotent against duplicate calls - whichever NodeMap
        // was built first and successfully registered stays authoritative - and turns a
        // previously silent data-loss bug into, at worst, a harmless redundant BuildMap()
        // call whose result is thrown away.
        if (Registry.TryGetValue(simpleKey, out var existing) && existing != map)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                $"[ERROR] MappingRegistry key collision on '{simpleKey}': a mapping for " +
                $"ModelType '{existing.ModelType?.Name}' / EntityType '{existing.EntityType?.Name}' " +
                $"already exists. The new registration for ModelType '{modelType.Name}' / " +
                $"EntityType '{entityType?.Name}' is being DISCARDED (keeping the original) - " +
                $"give the duplicate registration a distinct alias to disambiguate, or find " +
                $"and remove the call site causing the duplicate Register() call.");
            Console.ResetColor();

            return existing;
        }

        Registry[simpleKey] = map;

        return map;
    }

    public static IReadOnlyDictionary<string, NodeMap> GetAll()
    {
        return Registry;
    }
}