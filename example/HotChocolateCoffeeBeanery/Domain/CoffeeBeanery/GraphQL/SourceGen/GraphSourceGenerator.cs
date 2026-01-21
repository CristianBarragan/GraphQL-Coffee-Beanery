// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using CoffeeBeanery.GraphQL.SourceGen.Models;
// using System.Text;
// using System.Linq;
//
// namespace CoffeeBeanery.GraphQL.SourceGen;
//
// [Generator]
// public sealed class GraphSourceGenerator : ISourceGenerator
// {
//     public void Initialize(GeneratorInitializationContext context)
//     {
//         context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
//     }
//
//     public void Execute(GeneratorExecutionContext context)
//     {
//         if (context.SyntaxReceiver is not SyntaxReceiver receiver)
//             return;
//
//         GenerateSqlNodes(context, receiver);
//         GenerateMappings(context, receiver);
//         GenerateRuntimeNodes(context, receiver);
//         GenerateRuntimeFactory(context);
//     }
//
//     private static void GenerateSqlNodes(
//         GeneratorExecutionContext ctx,
//         SyntaxReceiver receiver)
//     {
//         var sb = new StringBuilder();
//
//         sb.AppendLine("namespace CoffeeBeanery.GraphQL.Generated;");
//         sb.AppendLine("using CoffeeBeanery.GraphQL.Core.Sql;");
//         sb.AppendLine("public static class SqlNodeRegistry");
//         sb.AppendLine("{");
//         sb.AppendLine("    public static readonly Dictionary<string, SqlNode> Nodes = new()");
//         sb.AppendLine("    {");
//
//         foreach (var edge in receiver.GraphEdges)
//         {
//             sb.AppendLine($"""
//                 ["{edge.Key}"] = new SqlNode
//                 {{
//                     Entity = "{edge.Entity}",
//                     Column = "{edge.Column}",
//                     SqlNodeType = Core.Sql.SqlNodeType.Node,
//                     IsGraph = {edge.IsGraph.ToString().ToLower()},
//                     FromEnumeration = {{ }},
//                     RelationshipKey = "{edge.RelationshipKey}"
//                 }},
//             """);
//         }
//
//         sb.AppendLine("    };");
//         sb.AppendLine("}");
//
//         ctx.AddSource("SqlNodeRegistry.g.cs", sb.ToString());
//     }
//
//     private static void GenerateMappings(
//         GeneratorExecutionContext ctx,
//         SyntaxReceiver receiver)
//     {
//         var sb = new StringBuilder();
//
//         sb.AppendLine("namespace CoffeeBeanery.GraphQL.Generated;");
//         sb.AppendLine("using CoffeeBeanery.GraphQL.Helper.LastVersion;");
//         sb.AppendLine("public static class MappingRegistry");
//         sb.AppendLine("{");
//
//         foreach (var map in receiver.Mappings)
//         {
//             sb.AppendLine($"""
//                 public static readonly PropertyMapping<{map.Model},{map.Entity}> {map.Name} =
//                     new (
//                         x => x.{map.ModelProp},
//                         x => x.{map.EntityProp}
//                     );
//             """);
//         }
//
//         sb.AppendLine("}");
//
//         ctx.AddSource("MappingRegistry.g.cs", sb.ToString());
//     }
//
//     private static void GenerateRuntimeNodes(
//         GeneratorExecutionContext ctx,
//         SyntaxReceiver receiver)
//     {
//         var sb = new StringBuilder();
//
//         sb.AppendLine("namespace CoffeeBeanery.GraphQL.Generated;");
//         sb.AppendLine("using CoffeeBeanery.GraphQL.Core.Runtime;");
//         sb.AppendLine("using CoffeeBeanery.GraphQL.Core.Sql;");
//         sb.AppendLine("using System.Collections.Generic;");
//
//         sb.AppendLine("public static class RuntimeNodeRegistry");
//         sb.AppendLine("{");
//         sb.AppendLine("    public static readonly Dictionary<string, RuntimeNode> Nodes = new()");
//         sb.AppendLine("    {");
//
//         // for every SqlNode, create RuntimeNode
//         foreach (var edge in receiver.GraphEdges)
//         {
//             sb.AppendLine($"""
//                 ["{edge.Key}"] = new RuntimeNode
//                 {{
//                     SqlNode = SqlNodeRegistry.Nodes["{edge.Key}"]
//                 }},
//             """);
//         }
//
//         sb.AppendLine("    };");
//         sb.AppendLine("}");
//
//         ctx.AddSource("RuntimeNodeRegistry.g.cs", sb.ToString());
//     }
//
//     private static void GenerateRuntimeFactory(GeneratorExecutionContext ctx)
//     {
//         ctx.AddSource("GraphRuntimeFactory.g.cs", """
//             namespace CoffeeBeanery.GraphQL.Generated;
//             using CoffeeBeanery.GraphQL.Core.Runtime;
//             using CoffeeBeanery.GraphQL.Core.Sql;
//
//             public static class GraphRuntimeFactory
//             {
//                 public static GraphRuntime Create()
//                 {
//                     return new GraphRuntime(
//                         SqlNodeRegistry.Nodes,
//                         RuntimeNodeRegistry.Nodes
//                     );
//                 }
//             }
//         """);
//     }
// }
