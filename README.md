# GraphQL Coffee Beanery

## Overview

Coffee Beanery is a high-performance GraphQL-to-SQL execution engine for .NET that transforms GraphQL query trees into optimized SQL statements executed directly by PostgreSQL.

Unlike traditional GraphQL implementations that resolve fields individually, Coffee Beanery analyzes the entire GraphQL query structure, generates an optimized execution plan, and delegates execution to the database engine. This approach eliminates common GraphQL performance bottlenecks, reduces database round trips, and enables PostgreSQL to optimize joins, filtering, sorting, pagination, and execution plans.

Coffee Beanery is designed for database-first architectures where performance, scalability, and complex relationship traversal are essential.

---

## Why Coffee Beanery?

Most GraphQL frameworks rely on resolver chains and DataLoaders to mitigate N+1 query issues.

Coffee Beanery takes a fundamentally different approach by generating a complete SQL execution plan from the GraphQL Abstract Syntax Tree (AST) before any database interaction occurs.

### Benefits

* Eliminates resolver-per-field execution
* Removes the need for DataLoaders
* Reduces database round trips
* Centralizes query planning and optimization
* Leverages native PostgreSQL execution capabilities
* Provides predictable performance for deeply nested object graphs
* Generates SQL dynamically without requiring manual query definitions

---

## Key Capabilities

* Dynamic GraphQL-to-SQL translation
* Dapper-first architecture
* Runtime query planning
* Model-to-entity mapping engine
* Relationship-driven query generation
* Automatic SQL join construction
* One-to-one relationship support
* One-to-many relationship support
* Many-to-many relationship support
* Built-in pagination
* Built-in filtering
* Built-in sorting
* Batched SQL execution
* Alias-based graph traversal
* Extensible execution pipeline
* Custom business logic integration
* Query handler support
* PostgreSQL query optimization
* Automatic execution plan reuse through PostgreSQL caching

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
| PostgreSQL Optimization   | ✅ Yes            | ⚠️ Partial    | ⚠️ Partial    |

---

## Status

Coffee Beanery is actively under development. The current focus is integrating Apache AGE to provide native graph relationship support alongside relational query capabilities.

---

## Quick Start

### Clone the Repository

```bash
git clone https://github.com/CristianBarragan/Coffee-Beanery.git
```

### Apply Database Migrations

```bash
dotnet ef database update
```

### Run the Example Application

```bash
dotnet run
```

### Explore the API

Open Nitro or your preferred GraphQL IDE and execute queries and mutations against the configured endpoint.

---

## Technology Stack

Coffee Beanery is built using:

* Hot Chocolate
* Dapper
* PostgreSQL
* Entity Framework
* Apache AGE (In Progress)
* Citus (Planned)

---

## Customization

Coffee Beanery provides multiple extension points for adapting the framework to your business requirements.

### Supported Customizations

* Column-level security
* Table-level security
* Claims-based authorization
* Data validation
* Query caching strategies
* Result transformation pipelines
* Custom execution handlers
* Domain-specific query processing

---

## How It Works

### Execution Flow

<img src="https://github.com/CristianBarragan/Coffee-Beanery/blob/main/ProcessFlow.png" alt="Execution_Flow" height="5%" width="10%">

---

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

## Tests

<img src="https://github.com/CristianBarragan/Coffee-Beanery/blob/main/example/HotChocolateCoffeeBeanery/Test/Test_Results.png" alt="Test_Results" height="60%" width="100%">

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

* InnerCustomerMappingSet
* OuterCustomerMappingSet

Mapping sets allow the same domain model to behave differently depending on the execution context.

---

## Roadmap

### Current Features

* Runtime Query Planning
* SQL Generation Engine
* Mapping Engine
* Dapper Integration
* PostgreSQL Support

### In Progress

* Apache AGE Integration
* Native Graph Relationship Support

### Planned

* Citus Integration
* Distributed Query Planning
* Performance Analytics
* Advanced Query Diagnostics

---

## Contributing

Contributions, feedback, and collaboration are welcome.

### Ways to Contribute

* Feature requests
* Bug reports
* Performance improvements
* Documentation enhancements
* Architecture proposals
* New mapping strategies
* Testing improvements

Whether you're improving documentation or proposing major architectural changes, every contribution helps improve the project.

---

## Support

If Coffee Beanery helps your team build faster and more scalable GraphQL APIs, consider supporting the project.

[Buy me a Coffee ☕]
*I would love a 100% colombian coffee!*

<a href="https://www.buymeacoffee.com/cristianbarragan" target="_blank">
<img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174">
</a>

---

## Keywords

GraphQL SQL Generator • GraphQL Query Planner • GraphQL Dapper • GraphQL PostgreSQL • GraphQL Database First • GraphQL Runtime SQL • GraphQL Query Optimization • GraphQL Performance • GraphQL N+1 Solution • GraphQL Execution Engine • GraphQL Relationship Mapping • GraphQL Join Generation • GraphQL AST Translation • High Performance GraphQL • Hot Chocolate Dapper • .NET GraphQL Framework • PostgreSQL GraphQL Framework
Relationship Mapping • GraphQL Join Generation • GraphQL AST Translation • High Performance GraphQL • Hot Chocolate Dapper • .NET GraphQL Framework • PostgreSQL GraphQL Framework
