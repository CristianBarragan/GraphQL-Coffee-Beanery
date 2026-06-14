using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Mapping;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.GraphQL;

public sealed class NodeTree
{
    public ISyntaxNode SyntaxNode { get; set; }
    public string Alias { get; set; } = "";
    public string Name { get; set; } = "";

    public List<string> SelectedFields { get; set; } = new();
    public List<NodeTree> Children { get; set; } = new();
    public List<NodeTree> RelatedChildren { get; set; } = new();
    
    public List<NodeTree> Parents { get; set; } = new();
    public List<NodeTree> RelatedParents { get; set; } = new();
    
    public NodeMetadata Metadata { get; set; } = new();
    
    public bool IsEntity => EntityType != null;
    
    public string ModelName => Name;

    public NodeMap NodeMap { get; set; }

    public Type ModelType { get; set; }
    public Type EntityType { get; set; }
    
    public List<NodeRelation> Relations { get; set; } = new();
    
}

public sealed class NodeMetadata
{
    public List<string> UpsertKeys { get; set; } = new();
}

public class NodeRelation
{
    public string FromAlias { get; set; } = "";
    public string ToAlias { get; set; } = "";
    public string FromColumn { get; set; } = "";
    public string ToColumn { get; set; } = "";
}