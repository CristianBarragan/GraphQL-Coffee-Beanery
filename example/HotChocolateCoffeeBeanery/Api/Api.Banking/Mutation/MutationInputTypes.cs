using Domain.Model;
using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace Api.Banking.Mutation;

public class WrapperInputType : InputObjectType<Wrapper>
{
    protected override void Configure(IInputObjectTypeDescriptor<Wrapper> inputObjectTypeDescriptor)
    {
    }
}

public class SortDefinition
{
    public string Name { get; set; } = default!;

    public bool IsLeaf { get; set; }

    public List<SortDefinition> Children { get; set; } = [];
}

public class SortDefinitionProvider : ISortDefinitionProvider
{
    public Task<SortDefinition> GetSortTreeAsync(
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            new SortDefinition
            {
                Name = "Wrapper",
                Children =
                [
                    new()
                    {
                        Name = "customer",
                        Children =
                        [
                            new()
                            {
                                Name = "firstNaming",
                                IsLeaf = true
                            },
                            new()
                            {
                                Name = "contactPoint",
                                Children =
                                [
                                    new()
                                    {
                                        Name = "contactPointValue",
                                        IsLeaf = true
                                    },
                                    new()
                                    {
                                        Name = "contactPointType",
                                        IsLeaf = true
                                    }
                                ]
                            }
                        ]
                    }
                ]
            });
    }
}

public enum SortDirection
{
    ASC,
    DESC
}

public interface ISortDefinitionProvider
{
    Task<SortDefinition> GetSortTreeAsync(
        CancellationToken cancellationToken);
}

public class DynamicSortModule : ITypeModule
{
    private readonly ISortDefinitionProvider _provider;

    public DynamicSortModule(ISortDefinitionProvider provider)
    {
        _provider = provider;
    }

    public event EventHandler<EventArgs>? TypesChanged;

    public async ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken)
    {
        var root = await _provider.GetSortTreeAsync(cancellationToken);

        var allNodes = new List<SortDefinition>();
        Collect(root, allNodes);

        var types = new Dictionary<string, InputObjectTypeDefinition>();

        // STEP 1: create ALL type shells first
        foreach (var node in allNodes)
        {
            if (node.IsLeaf) continue;

            var type = new InputObjectTypeDefinition(node.Name + "SortInput");
            types[type.Name] = type;
        }

        // STEP 2: fill fields AFTER all types exist
        foreach (var node in allNodes)
        {
            if (node.IsLeaf) continue;

            var type = types[node.Name + "SortInput"];

            foreach (var child in node.Children)
            {
                if (child.IsLeaf)
                {
                    type.Fields.Add(new InputFieldDefinition
                    {
                        Name = child.Name,
                        Type = TypeReference.Create("SortDirection", TypeContext.Input)
                    });
                }
                else
                {
                    type.Fields.Add(new InputFieldDefinition
                    {
                        Name = child.Name,
                        Type = TypeReference.Create(child.Name + "SortInput", TypeContext.Input)
                    });
                }
            }
        }

        return types.Values
            .Select(InputObjectType.CreateUnsafe)
            .ToArray();
    }

    public class SortInput
    {
        public Model Model { get; set; }
        public string Path { get; set; } = default!;
        public SortDirection Direction { get; set; }
    }
    
    private void Collect(SortDefinition node, List<SortDefinition> list)
    {
        list.Add(node);

        foreach (var child in node.Children)
            Collect(child, list);
    }
}

// public class DynamicWrapperSortType : SortInputType<Wrapper>
// {
//     public DynamicWrapperSortType()
//     {
//     }
//
//     protected override void Configure(ISortInputTypeDescriptor<Wrapper> descriptor)
//     {
//         // 1. Tell Hot Chocolate to skip scanning C# model reflection properties
//         descriptor.BindFieldsExplicitly();
//     }
//
//     protected override void OnCompleteType(
//         ITypeCompletionContext context, 
//         InputObjectTypeDefinition definition)
//     {
//         base.OnCompleteType(context, definition);
//
//         // 2. Fetch your field provider cleanly using Hot Chocolate's type context
//         var provider = context.Services.GetRequiredService<IDynamicFieldProvider>();
//
//         // 3. Resolve your fields synchronously using task unrolling
//         var dynamicFields = Task.Run(async () => 
//             await provider.GetSortableFieldsAsync(CancellationToken.None)
//         ).GetAwaiter().GetResult();
//
//         // 4. Manually construct and bind fields directly to the schema definition
//         foreach (var field in dynamicFields)
//         {
//             var fieldDefinition = new InputFieldDefinition
//             {
//                 Name = field,
//                 // FIXED: Use the string-name syntax factory method. 
//                 // This points the runtime configuration safely to the standard "SortEnumType".
//                 Type = TypeReference.Create("SortEnumType", TypeContext.Input)
//             };
//
//             definition.Fields.Add(fieldDefinition);
//         }
//     }
// }
//
//
//
public interface IDynamicFieldProvider
{
    Task<List<string>> GetSortableFieldsAsync(CancellationToken cancellationToken);
}
//
// public enum SortDirection
// {
//     ASC,
//     DESC
// }
//
// public class SortDefinition
// {
//     public string Name { get; set; } = default!;
//
//     public bool IsLeaf { get; set; }
//
//     public List<SortDefinition> Children { get; set; } = [];
// }
//
// public class SortNode
// {
//     public string Field { get; set; }
//
//     public string Direction { get; set; }
//
//     public List<SortNode> Children { get; set; }
// }
//
// public interface ISortDefinitionProvider
// {
//     Task<SortDefinition> GetSortTreeAsync(
//         CancellationToken cancellationToken);
// }
// public class DynamicWrapperSortType : SortInputType<Wrapper>
// {
//     // Keeping a parameterless constructor allows Hot Chocolate to 
//     // cleanly instantiate this when it reads your [UseSorting] attribute.
//     public DynamicWrapperSortType()
//     {
//     }
//
//     protected override void Configure(ISortInputTypeDescriptor<Wrapper> descriptor)
//     {
//         // 1. Tell Hot Chocolate to skip scanning static C# class properties
//         descriptor.BindFieldsExplicitly();
//     }
//
//     protected override void OnRegisterDependencies(
//         ITypeDiscoveryContext context, 
//         InputObjectTypeDefinition definition)
//     {
//         base.OnRegisterDependencies(context, definition);
//
//         // 2. Resolve your Dynamic Field Provider safely out of the Schema Services container
//         var fieldProvider = context.Services.GetRequiredService<IDynamicFieldProvider>();
//
//         // 3. Block-synchronise the asynchronous fetch task safely to prevent type ambiguity
//         List<string> dynamicFields = Task.Run(async () => 
//         {
//             return await fieldProvider.GetSortableFieldsAsync(CancellationToken.None);
//         }).GetAwaiter().GetResult();
//
//         // 4. Manually inject the dynamic string names into the underlying Type Definition fields
//         foreach (var field in dynamicFields)
//         {
//             // Instantiate a new sort descriptor mapped directly to the string field
//             var fieldDescriptor = SortInputFieldDescriptor.New(context.DescriptorContext, field);
//             
//             // Set the runtime input type explicitly to SortEnumType (ASC/DESC)
//             fieldDescriptor.Type<SortEnumType>();
//             
//             // Append it directly to the finalized definition collection
//             definition.Fields.Add(fieldDescriptor.CreateDefinition());
//         }
//     }
// }

