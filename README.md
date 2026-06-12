# GraphQL Coffee Beanery

## Overview

Coffee Beanery is a high-performance GraphQL-to-SQL execution engine for .NET that transforms GraphQL query trees into optimized SQL statements executed directly by PostgreSQL.

Unlike traditional GraphQL implementations that resolve fields individually, Coffee Beanery analyzes the entire GraphQL query structure, generates a unified execution plan, and delegates execution to the database engine. This approach eliminates common GraphQL performance bottlenecks, reduces database round trips, and enables PostgreSQL to fully optimize joins, filtering, sorting, pagination, and execution strategies.

Coffee Beanery is designed for database-first architectures where performance, scalability, and complex relationship traversal are essential.

### Hybrid Relational + Graph Execution

Coffee Beanery extends this model by introducing **native graph relationship support using Apache AGE**, enabling both relational and graph workloads to be executed within the same query plan.

Instead of treating graph traversal as a separate concern, Coffee Beanery:

* Translates GraphQL queries into a **hybrid execution plan**
* Executes relational logic using SQL
* Executes graph traversal using Cypher via Apache AGE
* Merges results into a single, optimized response

This allows:

* Deeply nested object graphs spanning relational and graph data
* Recursive and multi-hop relationship traversal
* Directional graph queries (incoming/outgoing edges)
* Seamless integration of graph traversal with filtering, sorting, and pagination

By combining SQL and graph execution into a single pipeline, Coffee Beanery enables **graph-native querying within a PostgreSQL-first architecture**, without introducing additional services or data stores.


---

## Why Coffee Beanery?

Most GraphQL frameworks rely on resolver chains and DataLoaders to mitigate N+1 query issues.

Coffee Beanery takes a fundamentally different approach by generating a complete SQL execution plan from the GraphQL Abstract Syntax Tree (AST) before any database interaction occurs.

### Benefits

- Eliminates resolver-per-field execution
- Removes the need for DataLoaders
- Reduces database round trips
- Centralizes query planning and optimization
- Leverages native PostgreSQL execution capabilities
- Provides predictable performance for deeply nested object graphs
- Generates SQL dynamically without requiring manual query definitions

---

## Key Capabilities

- Dynamic GraphQL-to-SQL translation
- Dapper-first architecture
- Runtime query planning
- Model-to-entity mapping engine
- Relationship-driven query generation
- Automatic SQL join construction
- One-to-one relationship support
- One-to-many relationship support
- Many-to-many relationship support
- Built-in pagination
- Built-in filtering
- Built-in sorting
- Batched SQL execution
- Alias-based graph traversal
- Extensible execution pipeline
- Custom business logic integration
- Query handler support
- PostgreSQL query optimization
- Automatic execution plan reuse through PostgreSQL caching
- Hybrid relational + graph query execution
- Apache AGE graph traversal integration
- Cypher query generation
- Recursive and depth-controlled graph traversal
- Graph + relational query composition

---

## Advanced Graph Relationship Execution (Apache AGE)

Coffee Beanery extends beyond traditional relational query planning by introducing **first-class graph traversal support** powered by Apache AGE.

This enables hybrid query execution where **relational joins and graph traversals coexist in a single execution plan**, all generated from a GraphQL query.

---

### Why This Matters

Traditional systems treat relational and graph data separately. Coffee Beanery unifies them:

* Relational data → handled via SQL joins
* Graph relationships → executed via Apache AGE (Cypher)
* Both → composed into a **single optimized pipeline**

This allows:

* Recursive relationship traversal
* Multi-hop queries (depth-controlled)
* Directional graph exploration (incoming/outgoing)
* Seamless blending of graph + relational filtering, sorting, and pagination

---

### Core Concepts

#### 1. GraphMap Configuration

Graph behavior is defined declaratively within the mapping layer:

* `GraphName` → Apache AGE graph
* `EdgeLabel` → edge type
* `EdgeKeyColumn` → unique edge identifier
* `FromVertex` / `ToVertex` → vertex definitions
* Join columns → bridge relational ↔ graph models

This allows Coffee Beanery to:

* Generate Cypher queries automatically
* Align graph edges with relational entities
* Maintain consistency between models

---

#### 2. Dual Write Strategy (Relational + Graph)

Mutations automatically generate:

1. **Relational upserts**

   * Persist entities and relationships in PostgreSQL tables

2. **Graph merges**

   * Create/update edges using Cypher

Example (simplified):

```sql
MERGE (a:Customer { InnerCustomerKey: '...' })
MERGE (b:Customer { OuterCustomerKey: '...' })
MERGE (a)-[r:CustomerCustomerEdge]->(b)
SET r.CustomerCustomerRelationshipKey = '...'
```

This ensures:

* Graph and relational data stay synchronized
* No duplicate edges
* Idempotent operations

---

#### 3. Graph-Aware Query Execution

During query generation:

* SQL handles base entity selection and joins
* Apache AGE handles relationship traversal
* Results are merged via generated subqueries

Example traversal:

```cypher
MATCH (a:Customer)-[r:CustomerCustomerEdge]->(b:Customer)
RETURN a, b, r
```

The result is materialized and joined back into the SQL execution plan.

---

#### 4. GraphModel in GraphQL

Graph traversal is controlled directly from GraphQL:

```graphql
graphModel: {
  edgeKey: "..."
  minDepth: 1
  maxDepth: 1
  recursive: true
  relationshipDirection: OUTGOING
  status: ACTIVE
}
```

This enables:

* Depth-limited traversal
* Recursive graph exploration
* Directional filtering
* Runtime control of graph execution behavior

---

### Execution Flow

1. GraphQL query is parsed into AST
2. Coffee Beanery builds a unified execution plan
3. Relational operations compiled into SQL
4. Graph traversal compiled into Cypher
5. Cypher executed via `ag_catalog.cypher`
6. Results merged into SQL query using CTEs
7. Final result returned as a single response

---

### Example: Hybrid Query Plan

A single GraphQL request can:

* Upsert relational entities
* Merge graph edges
* Traverse graph relationships
* Join results with relational data
* Apply filtering, sorting, and pagination

All executed in **one round-trip to PostgreSQL**.

---

### Key Advantages

* Eliminates need for separate graph services
* No impedance mismatch between SQL and graph models
* Fully dynamic graph traversal from GraphQL
* Consistent mapping and execution pipeline
* Leverages PostgreSQL query planner + AGE engine

---

### When to Use Graph Relationships

Use Apache AGE integration when:

* Relationships are recursive or hierarchical
* Traversal depth is dynamic
* Relationships are many-to-many and evolving
* You need graph algorithms or path-based queries
* Relational joins become inefficient or complex

---

### Summary

Coffee Beanery’s Apache AGE integration transforms it from a SQL query generator into a **hybrid relational-graph execution engine**, enabling:

* Unified data modeling
* Advanced relationship traversal
* High-performance query execution
* Fully dynamic GraphQL-driven graph operations

This is not just graph support—it is **graph-native query planning inside a SQL-first architecture**.

---

## Comparison

| Feature                   | Coffee Beanery   | Hot Chocolate | GraphQL.NET   |
| ------------------------- | ---------------- | ------------- | ------------- |
| Dapper First              | ✅ Yes            | ⚠️ Partial    | ⚠️ Partial    |
| GraphQL-to-SQL Generation | ✅ Yes            | ⚠️ Partial    | ❌ No          |
| Runtime Query Planning    | ✅ Yes            | ❌ No          | ❌ No          |
| Automatic Join Generation | ✅ Yes            | ❌ No          | ❌ No          |
| N+1 Elimination           | ✅ Database-Level | ⚠️ DataLoader | ⚠️ DataLoader |
| Source Customization      | ✅ Full           | ⚠️ Limited    | ⚠️ Limited    |
| PostgreSQL Optimization   | ✅ Yes            | ⚠️ Partial    | ❌ No          |

---

## Benchmarks

Tested with [Apidog](https://apidog.com) against a live PostgreSQL instance. No application-level caching — PostgreSQL built-in query cache only. All datasets use fully randomized UUID data.

The `Product` model in these tests spans **4 physical tables** across 3 schemas (`Banking`, `Lending`, `Account`). A single customer query generates 10 upsert statements and **1 SELECT** joining all 5 tables with 4 levels of nesting — resolved in a single database round trip. A resolver-chain architecture would require 5+ sequential round trips for the same graph.

Response times are this low because three sources of overhead are eliminated before the first request is served. At startup, `GraphWarmup.Init` discovers all `IMappingSet` implementations, `MappingWarmup` pre-caches every `PropertyInfo`, and `BulkMapper.Compile` compiles `Expression`-based getter/setter delegates to IL. By request time there is no reflection in the mapping layer — every property read and write goes through a cached compiled delegate.

This GraphQL mutation:

```graphql
mutation a {
  wrapper(
    wrapper: {
      model: INNER_CUSTOMER
      customerCustomerEdge: [{
        innerCustomer: {
          customerKey: "{{CustomerKey1}}"
          customerType: PERSON
          firstNaming: "{{FirstNaming1}}"
          product: [{
            accountKey: "{{AccountKey1}}"
            contractKey: "{{ContractKey1}}"
            transactionKey: "{{TransactionKey1}}"
            productType: CREDIT_CARD
            ...
          }]
        }
      }]
    }
    where: {
      customerCustomerEdge: {
        some: { innerCustomer: { customerKey: { eq: "{{CustomerKey1}}" } } }
      }
    }
  ) {
    edges { node { customerCustomerEdge { innerCustomer {
      customerKey customerType firstNaming
      product { contractKey accountName amount balance }
    }}}}
  }
}
```

Compiles into 10 upserts + this single SELECT across 3 schemas:

```sql
SELECT Customer.*, CBR.*, Contract.*, Account.*, Transaction.*
FROM "Banking"."Customer" Customer
LEFT JOIN "Banking"."CustomerBankingRelationship" CBR ON Customer."Id" = CBR."CustomerId"
  JOIN "Lending"."Contract" Contract ON CBR."Id" = Contract."CustomerBankingRelationshipId"
    JOIN "Account"."Account" Account ON Contract."AccountId" = Account."Id"
      JOIN "Lending"."Transaction" Transaction ON Account."Id" = Transaction."AccountId"
WHERE (Customer."CustomerKey" = '...');
```

### Single Customer — `eq` filter

5 datasets · 5 iterations · 10 assertions · **0 failures**

| Metric            | Value  |
|-------------------|--------|
| Avg Response Time | 13 ms  |
| Max Response Time | 67 ms  |
| Total Duration    | 239 ms |
| Pass Rate         | 100%   |

### Three Customers — `in` filter (batch)

5 datasets · 5 iterations · 30 assertions · **0 failures**

| Metric            | Value  |
|-------------------|--------|
| Avg Response Time | 16 ms  |
| Max Response Time | 78 ms  |
| Total Duration    | 239 ms |
| Pass Rate         | 100%   |

Scaling from 1 to 3 customers (3× entities, 30 upserts, 3× assertions) across a 4-table product graph added only **3 ms** to average response time. Total execution duration remained identical at **239 ms** — the SELECT shape stays the same, only the `WHERE ... IN (...)` clause grows.

→ Full GraphQL requests, complete generated SQL, and per-dataset breakdown: [BENCHMARKS.md](./BENCHMARKS.md)

---

## Quick Start

### Clone the Repository

```
git clone https://github.com/CristianBarragan/GraphQL-Coffee-Beanery.git
```

### Apply Database Migrations

```
dotnet ef database update
```

### Run the Example Application

```
dotnet run
```

### Explore the API

Open Nitro or your preferred GraphQL IDE and execute queries and mutations against the configured endpoint.

---

## Technology Stack

Coffee Beanery is built using:

- Hot Chocolate
- Dapper
- PostgreSQL
- Entity Framework
- Apache AGE (Graph Execution Engine)
- Citus (Planned)

---

## Customization

Coffee Beanery provides multiple extension points for adapting the framework to your business requirements.

### Supported Customizations

- Column-level security
- Table-level security
- Claims-based authorization
- Data validation
- Query caching strategies
- Result transformation pipelines
- Custom execution handlers
- Domain-specific query processing

---

## How It Works

### Execution Flow

[![Execution_Flow](./ProcessFlow.png)](./ProcessFlow.png)

1. A GraphQL query is parsed by Hot Chocolate.
2. Coffee Beanery converts the AST into an internal NodeTree representation.
3. Mapping sets resolve model-to-entity relationships.
4. The query planner generates optimized SQL statements.
5. PostgreSQL executes the generated SQL in batches.
6. Results are mapped back to domain entities and models.
7. Optional query handlers enrich or customize execution.
8. The GraphQL response is returned to the client.

### Result

This architecture avoids resolver chains, eliminates N+1 query patterns, and allows PostgreSQL to optimize execution plans, joins, and caching strategies.

---

## Core Concepts

### NodeTree

Represents the GraphQL query structure and serves as the foundation for query planning.

### NodeMap

Defines how domain models map to one or more database entities.

### FieldMap

Maps model properties to database columns and controls field-level translation.

### LinkKey

Defines relationships and join paths between entities.

### Mapping Sets

Provide context-aware mappings for a domain model.

#### Examples

- InnerCustomerMappingSet
- OuterCustomerMappingSet

Mapping sets allow the same domain model to behave differently depending on the execution context.

## Roadmap

### Current Features

- Runtime Query Planning
- SQL Generation Engine
- Mapping Engine
- Dapper Integration
- PostgreSQL Support

### Planned

- Citus Integration
- Distributed Query Planning
- Performance Analytics
- Advanced Query Diagnostics

---

## Contributing

Contributions, feedback, and collaboration are welcome.

### Ways to Contribute

- Feature requests
- Bug reports
- Performance improvements
- Documentation enhancements
- Architecture proposals
- New mapping strategies
- Testing improvements

Whether you're improving documentation or proposing major architectural changes, every contribution helps improve the project.

---

## Support

If Coffee Beanery helps your team build faster and more scalable GraphQL APIs, consider supporting the project.

[Buy me a Coffee ☕] *I would love a 100% colombian coffee!*

[![Buy Me A Coffee](https://cdn.buymeacoffee.com/buttons/default-orange.png)](https://www.buymeacoffee.com/cristianbarragan)

---

## Keywords

GraphQL SQL Generator • GraphQL Query Planner • GraphQL Dapper • GraphQL PostgreSQL • GraphQL Database First • GraphQL Runtime SQL • GraphQL Query Optimization • GraphQL Performance • GraphQL N+1 Solution • GraphQL Execution Engine • GraphQL Relationship Mapping • GraphQL Join Generation • GraphQL AST Translation • High Performance GraphQL • Hot Chocolate Dapper • .NET GraphQL Framework • PostgreSQL GraphQL Framework

---

## AI Documentation

- [llms.txt](./llms.txt)
- [ai.seo.md](./ai.seo.md)