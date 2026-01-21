// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using CoffeeBeanery.GraphQL.SourceGen.Models;
// using System.Collections.Generic;
// using System.Linq;
//
// namespace CoffeeBeanery.GraphQL.SourceGen
// {
//     public sealed class SyntaxReceiver : ISyntaxReceiver
//     {
//         public List<GraphEdge> GraphEdges { get; } = new();
//         public List<Mapping> Mappings { get; } = new();
//
//         public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
//         {
//             // collect mappings
//             if (syntaxNode is ObjectCreationExpressionSyntax obj &&
//                 obj.Type.ToString().Contains("PropertyMapping"))
//             {
//                 var modelType = obj.Type.ToString().Split('<')[1].Split(',')[0].Trim();
//                 var entityType = obj.Type.ToString().Split(',')[1].TrimEnd('>').Trim();
//
//                 var argList = obj.ArgumentList?.Arguments;
//
//                 if (argList != null && argList.Count >= 2)
//                 {
//                     var modelProp = argList[0].ToString().Split("=>")[1].Trim();
//                     var entityProp = argList[1].ToString().Split("=>")[1].Trim();
//
//                     if (!string.IsNullOrEmpty(modelProp) && !string.IsNullOrEmpty(entityProp))
//                     {
//                         Mappings.Add(new Mapping
//                         {
//                             Name = $"{modelType}To{entityType}",
//                             Model = modelType,
//                             Entity = entityType,
//                             ModelProp = modelProp,
//                             EntityProp = entityProp
//                         });
//                     }
//                 }
//             }
//
//             // collect graph edges if class has attribute [GraphKey]
//             if (syntaxNode is ClassDeclarationSyntax classDecl)
//             {
//                 var hasGraphKey = classDecl.AttributeLists
//                     .SelectMany(x => x.Attributes)
//                     .Any(a => a.Name.ToString().Contains("GraphKey"));
//
//                 if (hasGraphKey)
//                 {
//                     GraphEdges.Add(new GraphEdge
//                     {
//                         Key = classDecl.Identifier.Text,
//                         Entity = classDecl.Identifier.Text,
//                         Column = "Id",
//                         IsGraph = true,
//                         RelationshipKey = $"{classDecl.Identifier.Text}~Id"
//                     });
//                 }
//             }
//         }
//     }
// }
