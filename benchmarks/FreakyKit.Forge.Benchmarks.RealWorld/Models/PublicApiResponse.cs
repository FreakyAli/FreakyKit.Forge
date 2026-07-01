namespace ForgeBenchmarks.RealWorld.PublicApiResponse;

// ─── Source entities ─────────────────────────────────────────────────────────

public class PagedResponseEntity
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string RequestId { get; set; } = "";
    public List<LinkEntity> Links { get; set; } = new();
    public List<ResourceEntity> Items { get; set; } = new();
    public MetaEntity Meta { get; set; } = null!;
}

public class ResourceEntity
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Author { get; set; } = "";
    public int Version { get; set; }
    public ResourceVisibility Visibility { get; set; }
    public List<LinkEntity> Links { get; set; } = new();
    public List<string> Categories { get; set; } = new();
}

public class LinkEntity
{
    public string Rel { get; set; } = "";
    public string Href { get; set; } = "";
    public string? Method { get; set; }
    public string? Title { get; set; }
}

public class MetaEntity
{
    public string ApiVersion { get; set; } = "";
    public string Deprecation { get; set; } = "";
    public string? Warning { get; set; }
    public int RateLimitRemaining { get; set; }
    public int RateLimitTotal { get; set; }
    public DateTime RateLimitResetAt { get; set; }
}

public enum ResourceVisibility { Public, Restricted, Internal }

// ─── DTOs ────────────────────────────────────────────────────────────────────

public class PagedResponseDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string RequestId { get; set; } = "";
    public List<LinkDto> Links { get; set; } = new();
    public List<ResourceDto> Items { get; set; } = new();
    public MetaDto Meta { get; set; } = null!;
}

public class ResourceDto
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Author { get; set; } = "";
    public int Version { get; set; }
    public ResourceVisibility Visibility { get; set; }
    public List<LinkDto> Links { get; set; } = new();
    public List<string> Categories { get; set; } = new();
}

public class LinkDto
{
    public string Rel { get; set; } = "";
    public string Href { get; set; } = "";
    public string? Method { get; set; }
    public string? Title { get; set; }
}

public class MetaDto
{
    public string ApiVersion { get; set; } = "";
    public string Deprecation { get; set; } = "";
    public string? Warning { get; set; }
    public int RateLimitRemaining { get; set; }
    public int RateLimitTotal { get; set; }
    public DateTime RateLimitResetAt { get; set; }
}

[Facet.Facet(typeof(LinkEntity))]
public partial class LinkFacetDto;

[Facet.Facet(typeof(MetaEntity))]
public partial class MetaFacetDto;

[Facet.Facet(typeof(ResourceEntity), NestedFacets = [typeof(LinkFacetDto)])]
public partial class ResourceFacetDto;

[Facet.Facet(typeof(PagedResponseEntity), NestedFacets = [typeof(LinkFacetDto), typeof(MetaFacetDto), typeof(ResourceFacetDto)])]
public partial class PagedResponseFacetDto;
