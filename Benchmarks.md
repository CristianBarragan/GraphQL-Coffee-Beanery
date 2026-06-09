# Coffee Beanery — Benchmark Results

> **Conditions:** No application-level caching. PostgreSQL built-in query cache only.
> **Tool:** Apidog
> **Date:** June 2026

---

## Overview

Each test executes a full round-trip against a live PostgreSQL instance:

1. **Mutation (upsert)** — inserts or updates all entities across the full relationship graph using `INSERT ... ON CONFLICT DO UPDATE`
2. **Filtered query** — a single batched `SELECT` with nested `LEFT JOIN` / `JOIN` chains that resolves the entire object graph in one database call

The `Product` model in these tests spans **4 physical tables** (`Banking.CustomerBankingRelationship`, `Lending.Contract`, `Account.Account`, `Lending.Transaction`). A single GraphQL query touching one customer with one product generates:

- **10 upsert statements** (writes across all 4 tables + relationship resolution steps)
- **1 SELECT** joining 5 tables across 3 schemas with 4 levels of nesting — resolved in a single database round trip

All datasets use fully randomized keys (UUIDs) and values. Assertions validate both structure and field values.

---

## What the Generated SQL Looks Like

A single-customer query produces this execution plan:

```
Upserts (10 statements):
  Lending.Transaction          — insert amount, balance, transactionKey
  Account.Account              — insert accountKey, accountName, accountNumber
  Lending.Contract             — insert amount, contractKey, contractType
  Banking.CustomerBankingRelationship  — insert key
  Banking.Customer             — insert customerKey, type, name fields
  Banking.CustomerBankingRelationship  — resolve customerId FK
  Lending.Contract             — resolve customerBankingRelationshipId FK
  Lending.Contract             — resolve accountId FK
  Lending.Transaction          — resolve contractId FK
  Lending.Transaction          — resolve accountId FK

SELECT (1 statement, 4 JOINs):
  Banking.Customer
    LEFT JOIN Banking.CustomerBankingRelationship
      JOIN Lending.Contract
        JOIN Account.Account
          JOIN Lending.Transaction
```

In a resolver-chain architecture this graph would require **5+ sequential round trips** per entity. Coffee Beanery resolves it in **1**.

---

## Test 1 — Single Customer (`eq` filter)

**Scenario:** One customer with one product per dataset. Fully random data per iteration.

**Query shape:** `mutation` upsert + `where: { customerKey: { eq: "..." } }` → returns `customerCustomerEdge → innerCustomer → product` (4 tables)

| Metric              | Value  |
|---------------------|--------|
| Datasets            | 5      |
| Iterations Executed | 5      |
| Iterations Failed   | 0      |
| Assertions Executed | 10     |
| Assertions Failed   | 0      |
| Pass Rate           | 100%   |
| Total Duration      | 239 ms |
| Max Response Time   | 67 ms  |
| Avg Response Time   | 13 ms  |

**Per-dataset response times:**

| Dataset   | Response Time |
|-----------|---------------|
| Dataset-1 | 15 ms         |
| Dataset-2 | 13 ms         |
| Dataset-3 | 13 ms         |
| Dataset-4 | 12 ms         |
| Dataset-5 | 14 ms         |

---

## Test 2 — Three Customers (`in` filter, batch)

**Scenario:** Three customers each with one product per dataset. All three upserted and queried in a single GraphQL operation.

**Query shape:** `mutation` upsert (3 edges, 30 upsert statements) + `where: { customerKey: { in: [...] } }` → returns all three `innerCustomer` nodes with their `product` arrays across 4 tables each

| Metric              | Value  |
|---------------------|--------|
| Datasets            | 5      |
| Iterations Executed | 5      |
| Iterations Failed   | 0      |
| Assertions Executed | 30     |
| Assertions Failed   | 0      |
| Pass Rate           | 100%   |
| Total Duration      | 239 ms |
| Max Response Time   | 78 ms  |
| Avg Response Time   | 16 ms  |

**Per-dataset response times:**

| Dataset   | Response Time |
|-----------|---------------|
| Dataset-1 | 14 ms         |
| Dataset-2 | 20 ms         |
| Dataset-3 | 17 ms         |
| Dataset-4 | 14 ms         |
| Dataset-5 | 13 ms         |

---

## Observations

- The `Product` model spans **4 physical tables** across 3 PostgreSQL schemas. In a resolver-chain GraphQL implementation this relationship alone would trigger N+1 queries at every nesting level. Coffee Beanery compiles the entire graph into **1 SELECT** regardless of depth.
- Scaling from 1 to 3 customers (3× entities, 3× upserts, 3× assertions) added only **3 ms** to average response time (13 ms → 16 ms). Total end-to-end duration remained **identical at 239 ms**.
- Max response time increased by only **11 ms** (67 ms → 78 ms) when handling 3× the data — the batched SQL execution plan scales near-linearly.
- **0 assertion failures** across all 40 assertions (10 + 30) on fully randomized UUID data.

---

## Environment Notes

- No application-level or HTTP caching active during tests
- PostgreSQL built-in execution plan cache active (plans reused after first execution)
- All keys (customer, account, contract, transaction, banking relationship) are random UUIDs per dataset
- All name fields (first, full, last) are random strings per dataset
- Schemas involved: `Banking`, `Lending`, `Account`
- Relationship traversal depth: `Wrapper → CustomerCustomerEdge → InnerCustomer → Product (Contract + Account + Transaction)`