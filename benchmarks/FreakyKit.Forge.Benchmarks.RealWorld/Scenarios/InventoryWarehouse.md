# Scenario: Inventory & Warehouse Movement

**Domain:** Warehouse management / inventory — SKU detail with multi-warehouse stock breakdown
and recent movement history.

## What this represents

A typical inventory-detail API response for a single SKU. The shape covers:

- 7 outer-level scalar properties (item identity, unit of measure, weight, handling flags)
- A `Warehouses` collection — one entry per warehouse that stocks the SKU
- Each warehouse entry contains its OWN `Bins` collection (collection-of-collections, the
  shape that most directly stresses per-element-of-per-element mapping)
- A parallel `Movements` audit collection for recent inventory transactions

## Inspiration (hybrid attribution)

Shape is representative of patterns found in:

- **[Apache OFBiz](https://github.com/apache/ofbiz-framework)** (Apache-2.0) — its inventory
  data model has the same SKU + Facility + FacilityLocation + InventoryItem nesting structure.
- **[OpenBoxes](https://github.com/openboxes/openboxes)** (Eclipse Public License — shape
  inspiration only, not copied due to licence) — its multi-warehouse stock model is a
  textbook implementation of the bin-location pattern we use.
- **[Odoo inventory module](https://github.com/odoo/odoo/tree/master/addons/stock)**
  (LGPL — shape inspiration only) — its `stock.quant` (quantity-at-location) and
  `stock.move` (movement) tables are this exact shape, simplified.
- Production WMS systems (Manhattan, Blue Yonder, NetSuite) follow this same SKU →
  Warehouse → Bin denormalisation as their API response shape.

## Why this is interesting to benchmark

- **Collection-of-collections** is the most expensive structural pattern in object mapping.
  Each outer-collection element triggers a fresh inner-collection allocation, which compounds
  the per-element overhead. This is where library implementations diverge most.
- **Mixed enum + Guid + decimal + nullable string + DateTime** in the Movement collection
  exercises every primitive copy path simultaneously.
- **Realistic skew** in inner-collection sizes (4 bins in one warehouse, 2 in another, 1 in
  the third) prevents the JIT from over-optimising a uniform shape.

## Fixture rationale

The seeded SKU is a non-hazardous, non-refrigerated industrial widget tracked across 3
warehouses (Chicago / LA / NYC, typical US 3PL footprint) with 7 total bin locations and 8
movement entries spanning a typical receive → putaway → pick → ship → cycle-count flow.
Property values mirror what a WMS like Manhattan Active or NetSuite WMS would return for a
single-SKU detail query.

## Modifications

None — POCO modelling only. Real WMS data layers often include a JSON "extended attributes"
column per record; we've omitted that to keep the benchmark focused on structural copying.
