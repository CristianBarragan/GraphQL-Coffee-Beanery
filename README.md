# CoffeeBeanery Mapping System

## Overview

CoffeeBeanery is a **GraphQL-to-SQL mapping framework** that dynamically translates, maps from/to domain models and entities into SQL queries using a declarative mapping system. All statements are batched to take advantage of the database cache.

It is built on top of:

- GraphQL domain models (`Domain.Model`)
- Database entities (`Database.Entity`)
- Custom mapping engine (`CoffeeBeanery.GraphQL.Core.Mapping`)
- SQL graph construction (`NodeMap`, `FieldMap`, `LinkKey`)

The goal is to support **flexible, context-aware data access** while maintaining a strict separation between:

- Domain models (GraphQL layer)
- Database schema
- Query generation logic

### Stack

- Hot Chocolate : Only requires lean setup
- Dapper : Used to act a dynamic Data Access Layer
- PostgreSQL : Database used by the framework
- Entity Framework
- Apache AGE : In the near future will also support Relationship Base Graphs along with the other features

---

## Architecture

GraphQL Request
↓
Resolver
↓
MappingSet (Inner / Outer context)
↓
NodeMap (relationship graph)
↓
FieldMap + LinkKey resolution
↓
SQL generation
↓
Database (PostgreSQL via Npgsql/Dapper)
↓
Mapped Domain Model

---

## Core Concepts

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

## Query Execution Flow

GraphQL request received → Resolver invoked → MappingSet selected (Inner / Outer) → NodeMap constructed → FieldMaps applied → LinkKeys generate SQL joins → SQL executed via Dapper/Npgsql → Results mapped back to omain model
