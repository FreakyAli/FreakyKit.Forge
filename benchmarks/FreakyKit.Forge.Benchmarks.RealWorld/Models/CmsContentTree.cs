namespace ForgeBenchmarks.RealWorld.CmsContentTree;

// ─── Source entities ─────────────────────────────────────────────────────────

public class PageEntity
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Locale { get; set; } = "";
    public PageStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string AuthorName { get; set; } = "";
    public string SeoDescription { get; set; } = "";
    public List<BlockEntity> Blocks { get; set; } = new();
    public List<LocaleVariantEntity> LocaleVariants { get; set; } = new();
}

/// <summary>
/// Content block — flat children list (no recursion at the mapper level to keep types
/// EF/JSON-friendly). Real CMS layouts often denormalise this way and reconstruct the tree
/// at render time using ParentBlockId.
/// </summary>
public class BlockEntity
{
    public Guid Id { get; set; }
    public Guid? ParentBlockId { get; set; }
    public BlockType Type { get; set; }
    public int OrderIndex { get; set; }
    public string? TextContent { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaAltText { get; set; }
    public int? MediaWidth { get; set; }
    public int? MediaHeight { get; set; }
    public string? CssClass { get; set; }
}

public class LocaleVariantEntity
{
    public string Locale { get; set; } = "";
    public string Title { get; set; } = "";
    public string SeoDescription { get; set; } = "";
    public bool IsAutoTranslated { get; set; }
}

public enum PageStatus { Draft, Review, Published, Archived }
public enum BlockType { Heading, Paragraph, Image, Video, Quote, CodeBlock, Divider, Container }

// ─── DTOs ────────────────────────────────────────────────────────────────────

public class PageDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Locale { get; set; } = "";
    public PageStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string AuthorName { get; set; } = "";
    public string SeoDescription { get; set; } = "";
    public List<BlockDto> Blocks { get; set; } = new();
    public List<LocaleVariantDto> LocaleVariants { get; set; } = new();
}

public class BlockDto
{
    public Guid Id { get; set; }
    public Guid? ParentBlockId { get; set; }
    public BlockType Type { get; set; }
    public int OrderIndex { get; set; }
    public string? TextContent { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaAltText { get; set; }
    public int? MediaWidth { get; set; }
    public int? MediaHeight { get; set; }
    public string? CssClass { get; set; }
}

public class LocaleVariantDto
{
    public string Locale { get; set; } = "";
    public string Title { get; set; } = "";
    public string SeoDescription { get; set; } = "";
    public bool IsAutoTranslated { get; set; }
}

[Facet.Facet(typeof(BlockEntity))]
public partial class BlockFacetDto;

[Facet.Facet(typeof(LocaleVariantEntity))]
public partial class LocaleVariantFacetDto;

[Facet.Facet(typeof(PageEntity), NestedFacets = [typeof(BlockFacetDto), typeof(LocaleVariantFacetDto)])]
public partial class PageFacetDto;
