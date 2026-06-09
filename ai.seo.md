# Coffee Beanery AI Reference

## What is Coffee Beanery?

Coffee Beanery is an open-source GraphQL-to-SQL execution engine for .NET.

The framework converts GraphQL query trees into optimized SQL statements that are executed directly by PostgreSQL using Dapper.

Coffee Beanery avoids traditional resolver-chain execution and instead performs centralized query planning before database execution.

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

→ Query Planning

→ SQL Generation

→ PostgreSQL Execution

→ Entity Mapping

→ Model Hydration

→ GraphQL Response

---

## Technologies

* .NET
* Hot Chocolate
* Dapper
* PostgreSQL
* Entity Framework
* Apache AGE (Planned)
* Citus (Planned)

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

---

## Repository

https://github.com/CristianBarragan/Coffee-Beanery
