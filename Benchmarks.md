# Coffee Beanery — Benchmark Results

> **Conditions:** No application-level caching. PostgreSQL built-in query cache only.
> **Tool:** Apidog
> **Date:** June 2026

---

## Overview

All tests execute a full round-trip: GraphQL mutation (upsert) + filtered query returning nested entities across a `Customer → Product` relationship graph. Each dataset uses fully randomized data. Assertions validate both structure and values.

---

## Test 1 — Single Customer

**Scenario:** One customer with one product per dataset. Random data per iteration.

**Query shape:** `mutation` upsert + `where: { customerKey: { eq: ... } }` filter → returns `customerCustomerEdge → innerCustomer → product`

| Metric               | Value  |
|----------------------|--------|
| Datasets             | 5      |
| Iterations Executed  | 5      |
| Iterations Failed    | 0      |
| Assertions Executed  | 10     |
| Assertions Failed    | 0      |
| Pass Rate            | 100%   |
| Total Duration       | 239 ms |
| Max Response Time    | 67 ms  |
| Avg Response Time    | 13 ms  |

**Per-dataset response times:**

| Dataset   | Response Time |
|-----------|---------------|
| Dataset-1 | 15 ms         |
| Dataset-2 | 13 ms         |
| Dataset-3 | 13 ms         |
| Dataset-4 | 12 ms         |
| Dataset-5 | 14 ms         |

---

## Test 2 — Three Customers (Batch / `in` filter)

**Scenario:** Three customers each with one product per dataset. All three upserted and queried in a single operation.

**Query shape:** `mutation` upsert (3 edges) + `where: { customerKey: { in: [...] } }` filter → returns all three `innerCustomer` nodes with their `product` arrays

| Metric               | Value  |
|----------------------|--------|
| Datasets             | 5      |
| Iterations Executed  | 5      |
| Iterations Failed    | 0      |
| Assertions Executed  | 30     |
| Assertions Failed    | 0      |
| Pass Rate            | 100%   |
| Total Duration       | 239 ms |
| Max Response Time    | 78 ms  |
| Avg Response Time    | 16 ms  |

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

- Scaling from 1 to 3 customers (3× the graph depth and assertion count) added only **~3 ms** to the average response time (13 ms → 16 ms), demonstrating near-linear cost growth rather than the exponential growth typical of resolver-chain or N+1 architectures.
- Max response time increased by **11 ms** (67 ms → 78 ms) while handling 3× the entities and 3× the assertions — the SQL execution plan scales efficiently because joins are pre-compiled into a single batched query.
- Total end-to-end duration remained identical at **239 ms** across both scenarios.
- **0 assertion failures** across 40 total assertions (10 + 30) on fully randomized data.

---

## Environment Notes

- No application-level or HTTP caching layer active during tests
- PostgreSQL built-in plan cache active (execution plans reused after first execution)
- All customer keys, names, account keys, contract keys, and transaction keys are randomized per dataset
- Relationship traversal: `Wrapper → CustomerCustomerEdge → InnerCustomer → Product`