using AutoMapper;

namespace ForgeBenchmarks.RealWorld.PublicApiResponse;

// ─── Forge ───────────────────────────────────────────────────────────────────

[global::FreakyKit.Forge.Forge]
public static partial class PublicApiForges
{
    public static partial LinkDto MapLink(LinkEntity source);
    public static partial MetaDto MapMeta(MetaEntity source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial ResourceDto MapResource(ResourceEntity source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial PagedResponseDto MapResponse(PagedResponseEntity source);
}

// ─── Hand-written baseline ───────────────────────────────────────────────────

public static class PublicApiHandWritten
{
    public static PagedResponseDto MapResponse(PagedResponseEntity s)
    {
        var dto = new PagedResponseDto
        {
            Page = s.Page,
            PageSize = s.PageSize,
            TotalCount = s.TotalCount,
            TotalPages = s.TotalPages,
            GeneratedAt = s.GeneratedAt,
            RequestId = s.RequestId,
            Meta = new MetaDto
            {
                ApiVersion = s.Meta.ApiVersion,
                Deprecation = s.Meta.Deprecation,
                Warning = s.Meta.Warning,
                RateLimitRemaining = s.Meta.RateLimitRemaining,
                RateLimitTotal = s.Meta.RateLimitTotal,
                RateLimitResetAt = s.Meta.RateLimitResetAt,
            },
            Links = new List<LinkDto>(s.Links.Count),
            Items = new List<ResourceDto>(s.Items.Count),
        };
        foreach (var l in s.Links)
            dto.Links.Add(new LinkDto { Rel = l.Rel, Href = l.Href, Method = l.Method, Title = l.Title });
        foreach (var r in s.Items)
        {
            var rDto = new ResourceDto
            {
                Id = r.Id,
                Type = r.Type,
                Title = r.Title,
                Description = r.Description,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                Author = r.Author,
                Version = r.Version,
                Visibility = r.Visibility,
                Links = new List<LinkDto>(r.Links.Count),
                Categories = new List<string>(r.Categories),
            };
            foreach (var l in r.Links)
                rDto.Links.Add(new LinkDto { Rel = l.Rel, Href = l.Href, Method = l.Method, Title = l.Title });
            dto.Items.Add(rDto);
        }
        return dto;
    }
}

// ─── Mapperly ────────────────────────────────────────────────────────────────

[Riok.Mapperly.Abstractions.Mapper]
public static partial class PublicApiMapperly
{
    public static partial LinkDto MapLink(LinkEntity source);
    public static partial MetaDto MapMeta(MetaEntity source);
    public static partial ResourceDto MapResource(ResourceEntity source);
    public static partial PagedResponseDto MapResponse(PagedResponseEntity source);
}

// ─── AutoMapper profile ──────────────────────────────────────────────────────

public class PublicApiAutoMapperProfile : Profile
{
    public PublicApiAutoMapperProfile()
    {
        CreateMap<LinkEntity, LinkDto>();
        CreateMap<MetaEntity, MetaDto>();
        CreateMap<ResourceEntity, ResourceDto>();
        CreateMap<PagedResponseEntity, PagedResponseDto>();
    }
}

// ─── Mapster registration ────────────────────────────────────────────────────

public static class PublicApiMapsterConfig
{
    public static void Register()
    {
        Mapster.TypeAdapterConfig<LinkEntity, LinkDto>.NewConfig();
        Mapster.TypeAdapterConfig<MetaEntity, MetaDto>.NewConfig();
        Mapster.TypeAdapterConfig<ResourceEntity, ResourceDto>.NewConfig();
        Mapster.TypeAdapterConfig<PagedResponseEntity, PagedResponseDto>.NewConfig();
    }
}
