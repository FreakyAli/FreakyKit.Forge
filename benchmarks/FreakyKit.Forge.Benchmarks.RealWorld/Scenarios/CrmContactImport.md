# Scenario: CRM Contact Import

**Domain:** Customer relationship management — imports a contact record with the full
multi-channel communication history and arbitrary key/value metadata.

## What this represents

A typical CRM contact-import pipeline. Whether the source is Salesforce, HubSpot, an internal
ERP, or a CSV upload, the shape converges on the same pattern: a flat contact header plus
collections for phones, emails, addresses, tags, and free-form custom fields. The mapping
exercises:

- 11 outer-level scalar properties
- Three parallel collections of related records (phones, emails, addresses)
- An unbounded list of free-text tags
- A `Dictionary<string, string>` of custom fields — common for "field bag" patterns
- A nullable timestamp for last-contacted (the rest of contacts may never have been touched)

## Inspiration (hybrid attribution)

The shape is representative of patterns found in:

- **[NopCommerce](https://github.com/nopSolutions/nopCommerce)** (Apache-2.0) — its `Customer`
  domain entity includes a similar split of typed contact info plus an attribute bag
  (`GenericAttribute` collection serving the same role as our `CustomFields`).
- **[SuiteCRM data exports](https://github.com/salesagility/SuiteCRM)** (AGPL — pattern only,
  not copied) — the multiple-phone-types / multiple-email-types model is the canonical CRM
  shape, with each channel marked as primary/secondary.
- Public Salesforce / HubSpot REST API responses use a near-identical shape: typed phone array,
  email array with verification status, and a "custom properties" map.

## Why this is interesting to benchmark

- **Dictionary mapping** is one of the cases where libraries diverge most. AutoMapper builds a
  shallow copy by default. Mapster has its own dictionary handling. Forge produces a
  `new Dictionary<,>(source)` copy constructor invocation. Hand-written varies by author.
- **Unbounded tag list** mimics real production data where tag counts vary wildly. The fixture
  uses 5 tags but real records easily have 50+.
- **Three parallel collections** stress the per-element forge call vs the library's batch
  handling.

## Fixture rationale

The seeded contact represents a strategic-tier B2B account — a procurement VP at an industrial
manufacturer with 3 phones (work, mobile, assistant — typical executive contact pattern), 2
emails (work + personal, with different verification statuses), 2 addresses (work + home), 5
tags representing sales-team taxonomy, and 8 custom fields covering spend band, renewal cadence,
NPS, and segmentation data. Property values mirror what a Salesforce REST API sync would emit.

## Modifications

None — POCO modelling only. No framework attributes applied or stripped.
