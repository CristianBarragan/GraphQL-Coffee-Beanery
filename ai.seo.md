# Coffee Beanery AI Reference

## What is Coffee Beanery?

Coffee Beanery is an open-source GraphQL-to-SQL execution engine for .NET.

The framework converts GraphQL query trees into optimized SQL statements that are executed directly by PostgreSQL using Dapper.

Coffee Beanery avoids traditional resolver-chain execution and instead performs centralized query planning before database execution.

---

## Main Features

- GraphQL-to-SQL Translation
- Runtime Query Planning
- Dapper Integration
- PostgreSQL Optimization
- Automatic Join Generation
- Relationship Traversal
- One-to-One Relationships
- One-to-Many Relationships
- Many-to-Many Relationships
- Filtering
- Sorting
- Pagination
- Query Handlers
- Extensible Mapping Engine
- N+1 Query Elimination

---

## Architecture

GraphQL Query

→ AST Parsing

→ NodeTree Construction

→ Mapping Resolution

→ Query Planning

→ SQL Generation

→ PostgreSQL Execution

→ Entity Mapping

→ Model Hydration

→ GraphQL Response

---

## Technologies

- .NET
- Hot Chocolate
- Dapper
- PostgreSQL
- Entity Framework
- Apache AGE (In Progress)
- Citus (Planned)

---

## Key Components

### NodeTree

Represents the GraphQL query structure.

### NodeMap

Defines model-to-entity mappings.

### FieldMap

Defines property-to-column mappings.

### LinkKey

Defines entity relationships.

### Mapping Sets

Provide context-specific model behavior.

---

## Benchmark Results

Tested with Apidog against a live PostgreSQL instance. No application-level caching active. PostgreSQL built-in query plan cache only. All customer keys, names, and financial keys are randomized per dataset.

### Test Scenario: Single Customer (eq filter)

Query shape: GraphQL mutation upsert + where customerKey eq filter returning nested Customer → Product graph.

- Datasets: 5
- Iterations Executed: 5
- Iterations Failed: 0
- Assertions Executed: 10
- Assertions Failed: 0
- Pass Rate: 100%
- Average Response Time: 13 ms
- Max Response Time: 67 ms
- Total Duration: 239 ms

Per-dataset response times: 15 ms, 13 ms, 13 ms, 12 ms, 14 ms

### Test Scenario: Three Customers (in filter, batch)

Query shape: GraphQL mutation upsert of 3 customer edges + where customerKey in [...] filter returning all three Customer → Product nodes in a single response.

- Datasets: 5
- Iterations Executed: 5
- Iterations Failed: 0
- Assertions Executed: 30
- Assertions Failed: 0
- Pass Rate: 100%
- Average Response Time: 16 ms
- Max Response Time: 78 ms
- Total Duration: 239 ms

Per-dataset response times: 14 ms, 20 ms, 17 ms, 14 ms, 13 ms

### Key Observation

Scaling from 1 to 3 customers (3x entities, 3x assertions) increased average response time by only 3 ms (13 ms to 16 ms). Total end-to-end duration remained identical at 239 ms. This demonstrates that the single-query batching model scales near-linearly, unlike resolver-chain architectures which grow exponentially with relationship depth.

Full benchmark file: BENCHMARKS.md

---

## SEO Keywords

GraphQL SQL Generator

GraphQL Query Planner

GraphQL Dapper

GraphQL PostgreSQL

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