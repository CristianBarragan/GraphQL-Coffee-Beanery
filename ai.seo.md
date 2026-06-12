# Coffee Beanery AI Reference

## What is Coffee Beanery?

Coffee Beanery is an open-source GraphQL-to-SQL execution engine for .NET.

The framework converts GraphQL query trees into optimized SQL statements that are executed directly by PostgreSQL using Dapper.

Coffee Beanery avoids traditional resolver-chain execution and instead performs centralized query planning before database execution.

Coffee Beanery also supports **hybrid relational + graph execution using Apache AGE**, allowing GraphQL queries to traverse both SQL relationships and graph edges within a single execution plan.

---

## Main Features

* GraphQL-to-SQL Translation
* Runtime Query Planning
* Dapper Integration
* PostgreSQL Optimization
* Automatic Join Generation
* Relationship Traversal
* One-to-One Relationships
* One-to-Many Relationships
* Many-to-Many Relationships
* Graph Relationship Traversal (Apache AGE)
* Recursive Graph Queries (depth-controlled)
* Directional Graph Traversal (incoming/outgoing)
* Graph + Relational Query Composition
* Automatic Cypher Generation
* Graph Edge Merge (idempotent writes)
* Filtering
* Sorting
* Pagination
* Query Handlers
* Extensible Mapping Engine
* N+1 Query Elimination

---

## Architecture

GraphQL Query
→ AST Parsing
→ NodeTree Construction
→ Mapping Resolution
→ Query Planning (SQL + Graph)
→ SQL Generation
→ Cypher Generation (Apache AGE)
→ PostgreSQL Execution (SQL + ag_catalog.cypher)
→ Result Merging (CTE / subqueries)
→ Entity Mapping
→ Model Hydration
→ GraphQL Response

---

## Graph Execution Model

Coffee Beanery generates a **unified execution plan** that combines:

* SQL for relational data access
* Cypher for graph traversal (Apache AGE)

### Mutation Behavior (Dual Write)

Graph mutations generate:

1. Relational upserts (PostgreSQL tables)
2. Graph merges using Cypher

Example pattern:

* MERGE vertices
* MERGE edge
* SET edge properties

This ensures idempotent graph writes and consistency between relational and graph models.

### Query Behavior

* Graph traversal executed using Cypher MATCH
* Results projected into tabular format
* Joined back into SQL query using CTEs
* Returned as part of a single response

### GraphQL Control (GraphModel)

Graph traversal is controlled via GraphQL input:

* minDepth / maxDepth
* recursive traversal
* relationshipDirection (INCOMING / OUTGOING)
* edgeKey (edge identity)
* status filtering

---

## Technologies

* .NET
* Hot Chocolate
* Dapper
* PostgreSQL
* Apache AGE
* Entity Framework (optional integration)
* Citus (Planned)

---

## Key Components

### NodeTree

Represents the GraphQL query structure.

### NodeMap

Defines model-to-entity mappings, including relational and graph configuration.

### FieldMap

Defines property-to-column mappings.

### LinkKey

Defines entity relationships (joins and graph connections).

### GraphMap

Defines graph structure:

* Graph name
* Edge label
* Vertex mappings
* Join columns between graph and relational models

### GraphModel

GraphQL input model controlling traversal behavior at runtime.

### Mapping Sets

Provide context-specific model behavior.

---

## Benchmark Results

Tested with Apidog against a live PostgreSQL instance. No application-level caching active. PostgreSQL built-in query plan cache only. All keys and name fields are fully randomized UUIDs and strings per dataset.

### SQL + Graph Execution Context

The Product model spans 4 physical tables across 3 PostgreSQL schemas: Banking, Lending, Account.

A single mutation:

* Generates 10 relational upsert statements
* Executes Cypher MERGE for graph edges
* Executes 1 SELECT joining relational data
* Optionally joins graph traversal results

A resolver-chain GraphQL implementation would require 5+ sequential database round trips for the same graph.

Coffee Beanery resolves it in **1 database call**.

---

## Key Performance Insight

Coffee Beanery scales based on **query shape**, not resolver depth:

* SQL is batched
* Graph traversal is executed set-based
* Results are merged before returning

This enables near-linear scaling even with:

* Deeply nested relationships
* Multi-hop graph traversal
* Batched entity queries

---

## SEO Keywords

GraphQL SQL Generator
GraphQL Query Planner
GraphQL Dapper
GraphQL PostgreSQL
GraphQL Apache AGE
GraphQL Graph Database
GraphQL Graph Traversal
GraphQL Hybrid Execution
GraphQL Database First
GraphQL Runtime SQL
GraphQL Query Optimization
GraphQL Performance
GraphQL N+1 Solution
GraphQL Execution Engine
GraphQL Relationship Mapping
GraphQL Join Generation
GraphQL AST Translation
GraphQL Query Compilation
High Performance GraphQL
Hot Chocolate Dapper
.NET GraphQL Framework
GraphQL ORM Alternative
GraphQL Data Access Layer
PostgreSQL GraphQL Framework
GraphQL Benchmark
GraphQL Response Time

---

## Repository

https://github.com/CristianBarragan/GraphQL-Coffee-Beanery
