# Scenario: Public REST API Response

**Domain:** Public web API — paginated collection envelope with HATEOAS links and metadata.

## What this represents

A typical paged-list response from a public REST API. The shape covers:

- An outer envelope with 6 pagination scalars (Page, PageSize, TotalCount, TotalPages,
  GeneratedAt, RequestId)
- Envelope-level `Links` (first/prev/next/last self-navigation)
- A `Meta` block with API version, deprecation notices, and rate-limit info
- An `Items` collection of 20 resources (typical page size)
- Each resource has its own 11-property scalar surface AND its own Links collection AND its
  own tag/category list — dual-level nested collections

## Inspiration (hybrid attribution)

Shape is representative of patterns found in:

- **[GitHub REST API](https://docs.github.com/en/rest)** (documentation: CC-BY) — its
  `Link` headers + per-resource `_links` blocks established the de facto HATEOAS pattern that
  most public APIs follow.
- **[JSON:API specification](https://jsonapi.org/)** (CC-BY) — its `data` + `meta` + `links`
  separation matches our envelope shape, simplified to remove the `included` resource
  side-loading.
- **[Stripe API responses](https://stripe.com/docs/api)** (commercial spec, public docs) —
  the pagination shape we use (`data` array + `has_more` semantically equivalent to our
  Page/TotalPages) is the standard for paginated commercial APIs.
- **[Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines)** (CC-BY) —
  the `@nextLink` / `@previousLink` pattern in the official Microsoft guidance is our
  link-collection shape.

## Why this is interesting to benchmark

- **Generic envelope wrapping a collection** is the single most common API shape in any
  REST service. If your API returns a list, it's almost certainly wrapped like this.
- **Dual-level Links collections** (envelope + per-resource) test how mappers handle the
  same DTO type used at two depths in a single mapping pass.
- **20 items per page** is the typical default page size — large enough that per-item cost
  matters, small enough that the per-envelope overhead is not amortised away.
- **Mixed nullable strings** in the metadata + link blocks reflect real-world API responses
  where most fields populate and some don't (Method, Title, Warning).

## Fixture rationale

The seeded response is page 3 of a 21-page article list (412 total items, 20-per-page). All
five pagination links populate (we're not on the first or last page). Rate-limit meta shows
near-full quota remaining. Items follow a realistic distribution of visibility levels and
versioning. Property values mirror what GitHub's or Stripe's paginated endpoints would emit
on a typical query.

## Modifications

None — POCO modelling only. We've omitted the JSON:API `included` side-loading pattern (which
would add a third top-level collection of referenced resources) because it adds
JSON-deserialisation complexity that's orthogonal to the mapping benchmark. Real public APIs
that use side-loading would map to a separate scenario.
