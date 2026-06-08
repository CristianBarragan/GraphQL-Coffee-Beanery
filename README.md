# GraphQL Coffee Beanery

## Overview

CoffeeBeanery is a framework that dynamically translates GraphQL queries into raw SQL at runtime. The transformation happens on the fly, with full query capabilities available out of the box.

The framework requires only Mappings between domain models and database entities

---

## Key Capabilities

- Dynamic GraphQL-to-SQL translation at runtime
- Custome configuration based for complex model-to-entity mapping
- Allows subgraph mutations and queries using the same endpoint and wrapper object
- No Data Loaders / N + 1 issues
- Relationship-driven query generation  
- Built-in support for composing complex data graphs without manual SQL 
- Extensible architecture to integrate external or business logic out of the box
- Node types are translated into Left joins between entities.
- Edge types are translated into joins between entities.
- Paging support out of the box
- Filtering support out of the box
- Sorting support out of the box
- Alias based so there is no issues when there are multiple models / entities with the same type

---

## Status

CoffeeBeanery is actively developed and the main focus is integrating Apache AGE for graph relationship based support

---

## Contributions

Contributions, feedback, and collaboration are welcome

This includes:

- Design proposals
- Feature suggestions
- Bug reports
- Architectural feedback
- Documentation improvements
- Any ideas that help improve maturity or broaden adoption

### Running example

1. Clone repository
2. Run entity framework migrations
3. Compile and run api project
3. Use nitro IDE to create any type of graphql operation.
4. Validate data persistance and query result.

### Stack
- Hot Chocolate : Only requires lean setup for AST parsing and auth pipeline integration
- Dapper : Used to act a dynamic Data Access Layer
- PostgreSQL : Database used by the framework
- Entity Framework : Database schema maintenance
- Apache AGE : Apache AGE (In progress...)
- Citus : (TBC)

## Customizable Features

- Granular access by table/columns based on token-claims
- Data and column validations
- Query cache can be customized in multiple layers
- Query result handling can be fully customized
  
## Execution Flow

<img src="https://github.com/CristianBarragan/Coffee-Beanery/blob/main/ProcessFlow.png" alt="Execution_Flow" height="5%" width="10%">

---

## Tests

<img src="https://github.com/CristianBarragan/Coffee-Beanery/blob/main/example/HotChocolateCoffeeBeanery/Test/Test_Results.png" alt="Test_Results" height="60%" width="100%">

---

## Core Concepts

It is built on top of:

- GraphQL domain models (`Domain.Model`)
- Database entities (`Database.Entity`)
- Custom mapping engine (`CoffeeBeanery.GraphQL.Core.Mapping`)
- SQL graph construction (`NodeMap`, `FieldMap`, `LinkKey`)

The goal is to support **flexible, context-aware data access** whi

## Mapping Sets

Mapping sets define how a domain model behaves in a specific context.

### Available Sets

- InnerCustomerMappingSet → Internal perspective of a customer
- OuterCustomerMappingSet → External perspective of a customer

---

## Inner vs Outer Customer

### Inner Customer

- Represents internal actor in a relationship
- Uses:
  - InnerCustomerId
  - InnerCustomerKey
- Used when the customer is the **source** of a relationship

---

### Outer Customer

- Represents external/target actor in a relationship
- Uses:
  - OuterCustomerId
  - OuterCustomerKey
- Used when the customer is the **target** of a relationship

---

## Base Mapping

All customer mappings inherit from:

---

## CustomerBaseMapping

### Responsibilities

- Defines schema
- Defines core relationships
- Defines field mappings
- Defines upsert keys
- NodeMap

---

### NodeMap is the central structure used to build SQL queries.

- Schema
- Banking
- EntityParents

Defines upward relationships (joins to parent tables)

Customer → CustomerCustomerRelationship

Supports joins via:
Id
CustomerKey

---

### EntityChildren 

Defines one-to-many relationships

Customer → ContactPoint
Customer → CustomerBankingRelationship

---

### ModelChildren

Defines domain-level navigation relationships

Customer → Product

---

### ModelToEntityLinks

Maps domain models to database entities

Customer.CustomerKey → Customer.CustomerKey

---

### Field Mapping

Field mapping defines how domain fields map to database columns.

---

### Supported types
Scalars (string, int, Guid)
Enums
Nested relationships
Example Enum Mapping
CustomerType.Person → 0
CustomerType.Organisation → 1
Upsert Keys

---

Defines unique identity for insert/update operations.

### Purpose
- Prevent duplicate inserts
- Ensure safe updates
- Enable idempotency
- InnerCustomerMapping
- Purpose

Handles internal relationship traversal.

#### Key Relationships
Customer → CustomerCustomerRelationship (InnerCustomerId / InnerCustomerKey)
Customer → Customer table via CustomerKey
Usage

Used when querying customers as the source side of relationships.

#### OuterCustomerMapping
Purpose

Handles external relationship traversal.

#### Key Relationships
Customer → CustomerCustomerRelationship (OuterCustomerId / OuterCustomerKey)
Usage

Used when querying customers as the target side of relationships.

### ContactPoint Model

Represents customer contact information.

#### Fields
- ContactPointKey
- ContactPointType (Mobile, Landline, Email)
- ContactPointValue
- CustomerKey
- Relationship
- Customer.Id → ContactPoint.CustomerId

### [Buy me a Coffee ☕]
*I would love a 100% colombian coffee!*

<a href="https://www.buymeacoffee.com/cristianbarragan" target="_blank">
<img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174">
</a>
