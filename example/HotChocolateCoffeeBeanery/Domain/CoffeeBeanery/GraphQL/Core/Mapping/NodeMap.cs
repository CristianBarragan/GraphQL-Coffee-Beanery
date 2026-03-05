using System.Collections.Generic;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class NodeMap
    {
        public int Id { get; set; }
        
        public string Schema { get; set; } = "public";

        public bool IsModel { get; set; }
        
        public bool IsEntity { get; set; }

        public List<FieldMap> FieldMaps { get; } = new List<FieldMap>();
        public List<UpsertKey> UpsertKeys { get; private set; } = new List<UpsertKey>();
        public List<LinkKey> LinkKeys { get; private set; } = new List<LinkKey>();

        // ENUMS
        public Dictionary<string, string> FromEnum { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ToEnum { get; set; } = new Dictionary<string, string>();
        public List<string> Children { get; private set; } = new List<string>();
        
        public bool IsGraph { get; set; }
        
        public Type ModelType { get; set; }
        
        public Type EntityType { get; set; }

        public Dictionary<string, PropertyInfo> ModelProperties { get; set; }
            = new Dictionary<string, PropertyInfo>();

        public Dictionary<string, PropertyInfo> EntityProperties { get; set; }
            = new Dictionary<string, PropertyInfo>();
        
        public Func<object, object> CreateMapper { get; set; }
        
        public Action<object, object> UpdateMapper { get; set; }

    }

}