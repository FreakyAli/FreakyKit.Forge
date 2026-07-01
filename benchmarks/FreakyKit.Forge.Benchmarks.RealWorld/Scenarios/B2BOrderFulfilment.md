# Scenario: B2B Order Fulfilment

**Domain:** Order management for business-to-business commerce / ERP-style systems.

## What this represents

A typical order-management API mapping an internal `OrderEntity` (with full audit trail and
fulfilment event history) to an `OrderDto` for customer-facing or partner-facing consumption.
The shape exercises:

- Large flat property surface (~20 props on the outer order)
- Nested customer record with its own complex shape
- Two address blocks (ship-to + bill-to) with identical structure
- Variable-length line-item collection
- Audit/event collection that grows over the order lifecycle
- Mixed nullable timestamps (`ApprovedAt`, `ShippedAt`, `DeliveredAt`)
- Enums for status (`OrderStatus`, `FulfilmentEventType`, `CustomerTier`)
- Currency-tagged decimals for monetary values

## Inspiration (hybrid attribution)

Types in this scenario are not extracted verbatim from any single project. The shape is
representative of the order/customer/line-item/event patterns found in mature open-source
reference architectures:

- **[eShopOnContainers](https://github.com/dotnet-architecture/eShopOnContainers)** (MIT) — the
  canonical .NET reference architecture for commerce. Its `Order` and `OrderItem` aggregates
  inspired the line-item and customer modelling here, though our `OrderEntity` is broader and
  includes the fulfilment event audit trail that eShopOnContainers tracks via domain events
  rather than a flat collection.
- **[OpenIddict samples](https://github.com/openiddict/openiddict-core)** (Apache-2.0) — its
  application/authorization entity-to-DTO mapping patterns informed our use of mixed nullable
  timestamps for lifecycle events.
- Production B2B ERP integrations (Acumatica/Dynamics 365-style) commonly include a
  `PurchaseOrderNumber`, `PaymentTerms`, `WarehouseCode` triplet alongside the order header.

## Why this isn't a duplicate of the existing `EcommerceOrder` benchmark

The existing synthetic `EcommerceOrder` benchmark covers a shorter consumer-facing flow
(customer + ship-to + 5 line items + tags). This scenario differs by:

- Audit event collection that scales independently of line items
- A second address block (bill-to)
- Larger line-item count (12 vs 5)
- Customer tier enum + tax ID for B2B-specific fields
- Approvals timestamp lifecycle (consumer orders skip the approval gate)

## Fixture rationale

The seeded order represents a mid-size industrial-supply order from a Preferred-tier customer:
12 line items, 6 fulfilment events spanning a 3-day pick/pack/ship cycle, billing and shipping
addresses at the same physical location (common for warehouses), one not-yet-completed event
(`DeliveredAt = null`). Decimal values use realistic price points and discounts. Property values
mirror what an EDI import + warehouse management system would emit in production.

## Modifications

None — the types compile against BCL + System.ComponentModel.DataAnnotations only. No framework
attributes were stripped because none were applied to begin with (this is plain POCO modelling).
