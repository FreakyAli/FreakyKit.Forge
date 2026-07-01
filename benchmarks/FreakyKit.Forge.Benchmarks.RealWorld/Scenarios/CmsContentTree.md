# Scenario: CMS Content Tree

**Domain:** Headless CMS / content management — page record with nested content blocks and
i18n locale variants.

## What this represents

A typical headless CMS API response for a published page. The shape covers:

- 9 outer-level scalar properties for the page header
- A flat block list with `ParentBlockId` for tree reconstruction at render time (canonical CMS
  denormalisation; the SQL row representation matches the API response shape)
- A locale-variants collection for i18n translations
- 9 mixed nullable properties across block types (image dimensions, media metadata, CSS class)
- 12 blocks of varied types (Heading, Paragraph, Image, Container, etc.) — typical article
  length

## Inspiration (hybrid attribution)

Shape is representative of patterns found in:

- **[Strapi](https://github.com/strapi/strapi)** (MIT) — Strapi's dynamic-zones content model
  matches our `BlockEntity[]` with `Type` discriminator approach.
- **[Sitecore JSS](https://github.com/Sitecore/jss)** (Apache-2.0) — Sitecore's component-tree
  rendering uses an almost identical flat list + parent-id pattern at the API layer.
- **[Umbraco CMS](https://github.com/umbraco/Umbraco-CMS)** (MIT) — its block-list editor
  generates exactly this shape: ordered blocks with optional parent references for nested
  containers.
- **[Contentful Delivery API](https://www.contentful.com/developers/docs/references/content-delivery-api/)**
  (commercial spec, public docs) — its `linkedEntries` + `references` pattern is the same
  conceptual shape.

## Why this is interesting to benchmark

- **High nullable-property density** — Block has 6 nullable scalar fields (TextContent,
  MediaUrl, MediaAltText, MediaWidth, MediaHeight, CssClass). Each block type uses a different
  subset. Tests nullable-copy overhead.
- **Mixed block types** — variation in which fields are populated stresses the mapper's per-row
  consistency.
- **Locale variants** — separate collection, plain DTO, exercises a second concurrent collection
  alongside the primary block list.
- **Realistic block count** — 12 blocks is typical for a medium-length blog post or product page.

## Fixture rationale

Seeded page is a fictional blog post about object mapper scaling. Blocks form a realistic
shape: hero image, H1, body container with paragraphs + heading + code block + quote +
divider, then a CTA container with H3 + paragraph. The locale variants include three
translations with realistic-looking (auto-translated and human-translated) content. Property
values mirror what Contentful or Strapi would return for a published article.

## Modifications

None — POCO modelling only. Note that real CMS systems often have a Json column for free-form
block properties (Newtonsoft `JObject`-shaped). We've kept ours strongly typed because the
benchmark focuses on field-level copying overhead, not JSON deserialisation.
