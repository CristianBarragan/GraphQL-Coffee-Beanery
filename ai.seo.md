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

Tested with Apidog against a live PostgreSQL instance. No application-level caching active. PostgreSQL built-in query plan cache only. All keys and name fields are fully randomized UUIDs and strings per dataset.

### SQL Generation Context

The Product model spans 4 physical tables across 3 PostgreSQL schemas: Banking, Lending, Account. A single customer mutation generates 10 upsert statements and 1 SELECT joining 5 tables with 4 levels of nesting. A resolver-chain GraphQL implementation would require 5 or more sequential database round trips for the same graph. Coffee Beanery resolves it in 1.


### Startup Warmup Pipeline

Before the first request is served, Coffee Beanery executes a full mapping warmup:

1. GraphWarmup.Init scans the assembly for all IMappingSet implementations and registers them against both model and entity enum axes.
2. MappingWarmup.WarmupMap walks every FieldMap and stores resolved PropertyInfo objects in NodeMap.ModelProperties and NodeMap.EntityProperties, eliminating per-request Type.GetProperty calls.
3. BulkMapper.Compile builds Expression-based getter and setter delegates compiled to IL via Expression.Lambda.Compile(), stored in ConcurrentDictionary keyed by TypeFullName.PropertyName.
4. NodeTreeIterator.GenerateTree pre-builds the full traversal tree for every root mapping so query planning at request time walks a pre-computed structure.

At request time, the Mapper uses three ConcurrentDictionary caches (_propCache, _getterCache, _setterCache) populated during warmup. MapByAlias resolves the NodeMap by alias, iterates FieldMaps, and copies values using the pre-compiled delegates with zero reflection overhead.

### GraphQL Request Shape (Single Customer)

The input is a single GraphQL mutation that simultaneously upserts a Customer with a nested Product (spanning Contract, Account, Transaction tables) and queries the result back using a customerKey eq filter. Variables like CustomerKey1, AccountKey1, ContractKey1, TransactionKey1, and CustomerBankingRelationshipKey1 are randomized UUIDs per dataset. The query returns customerCustomerEdge > innerCustomer > product with fields: customerKey, customerType, firstNaming, fullNaming, lastNaming, contractKey, accountName, accountNumber, amount, balance.

### SQL Execution Plan (Single Customer)

Phase 1 - Leaf upserts (5 statements, no FK dependencies):
- INSERT INTO Lending.Transaction (Amount, Balance, TransactionKey) ON CONFLICT DO UPDATE
- INSERT INTO Account.Account (AccountKey, AccountName, AccountNumber) ON CONFLICT DO UPDATE
- INSERT INTO Lending.Contract (Amount, ContractKey, ContractType) ON CONFLICT DO UPDATE
- INSERT INTO Banking.CustomerBankingRelationship (CustomerBankingRelationshipKey) ON CONFLICT DO UPDATE
- INSERT INTO Banking.Customer (CustomerKey, CustomerType, FirstName, FullName, LastName) ON CONFLICT DO UPDATE

Phase 2 - FK resolution upserts (5 statements, SELECT-based):
- CustomerBankingRelationship updated with CustomerId resolved from Customer
- Contract updated with CustomerBankingRelationshipId resolved from CustomerBankingRelationship
- Contract updated with AccountId resolved from Account
- Transaction updated with ContractId resolved from Contract
- Transaction updated with AccountId resolved from Account

Phase 3 - Single batched SELECT (1 statement, 4 JOINs):
- Banking.Customer LEFT JOIN Banking.CustomerBankingRelationship JOIN Lending.Contract JOIN Account.Account JOIN Lending.Transaction
- WHERE Customer.CustomerKey = '...' (eq) or IN (...) for batch queries
- Entire object graph returned in one round trip

### Test Scenario: Single Customer (eq filter)

Query shape: GraphQL mutation upsert (10 SQL statements) + where customerKey eq filter → 1 SELECT joining Banking.Customer, Banking.CustomerBankingRelationship, Lending.Contract, Account.Account, Lending.Transaction.

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

Query shape: GraphQL mutation upsert of 3 customer edges (30 SQL upsert statements) + where customerKey in [...] filter → 1 SELECT returning all three Customer → Product graphs.

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

Scaling from 1 to 3 customers (3x entities, 3x upsert statements, 3x assertions) across a 4-table product graph increased average response time by only 3 ms (13 ms to 16 ms). Total end-to-end duration remained identical at 239 ms. The single batched SELECT execution model scales near-linearly regardless of entity count or relationship depth.

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