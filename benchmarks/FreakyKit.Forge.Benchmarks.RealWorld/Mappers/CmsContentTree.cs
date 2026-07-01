using AutoMapper;

namespace ForgeBenchmarks.RealWorld.CmsContentTree;

// ─── Forge ───────────────────────────────────────────────────────────────────

[global::FreakyKit.Forge.Forge]
public static partial class CmsForges
{
    public static partial BlockDto MapBlock(BlockEntity source);
    public static partial LocaleVariantDto MapLocale(LocaleVariantEntity source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial PageDto MapPage(PageEntity source);
}

// ─── Hand-written baseline ───────────────────────────────────────────────────

public static class CmsHandWritten
{
    public static PageDto MapPage(PageEntity s)
    {
        var dto = new PageDto
        {
            Id = s.Id,
            Slug = s.Slug,
            Title = s.Title,
            Locale = s.Locale,
            Status = s.Status,
            CreatedAt = s.CreatedAt,
            PublishedAt = s.PublishedAt,
            AuthorName = s.AuthorName,
            SeoDescription = s.SeoDescription,
            Blocks = new List<BlockDto>(s.Blocks.Count),
            LocaleVariants = new List<LocaleVariantDto>(s.LocaleVariants.Count),
        };
        foreach (var b in s.Blocks)
            dto.Blocks.Add(new BlockDto
            {
                Id = b.Id,
                ParentBlockId = b.ParentBlockId,
                Type = b.Type,
                OrderIndex = b.OrderIndex,
                TextContent = b.TextContent,
                MediaUrl = b.MediaUrl,
                MediaAltText = b.MediaAltText,
                MediaWidth = b.MediaWidth,
                MediaHeight = b.MediaHeight,
                CssClass = b.CssClass,
            });
        foreach (var v in s.LocaleVariants)
            dto.LocaleVariants.Add(new LocaleVariantDto
            {
                Locale = v.Locale,
                Title = v.Title,
                SeoDescription = v.SeoDescription,
                IsAutoTranslated = v.IsAutoTranslated,
            });
        return dto;
    }
}

// ─── Mapperly ────────────────────────────────────────────────────────────────

[Riok.Mapperly.Abstractions.Mapper]
public static partial class CmsMapperly
{
    public static partial BlockDto MapBlock(BlockEntity source);
    public static partial LocaleVariantDto MapLocale(LocaleVariantEntity source);
    public static partial PageDto MapPage(PageEntity source);
}

// ─── AutoMapper profile ──────────────────────────────────────────────────────

public class CmsAutoMapperProfile : Profile
{
    public CmsAutoMapperProfile()
    {
        CreateMap<BlockEntity, BlockDto>();
        CreateMap<LocaleVariantEntity, LocaleVariantDto>();
        CreateMap<PageEntity, PageDto>();
    }
}

// ─── Mapster registration ────────────────────────────────────────────────────

public static class CmsMapsterConfig
{
    public static void Register()
    {
        Mapster.TypeAdapterConfig<BlockEntity, BlockDto>.NewConfig();
        Mapster.TypeAdapterConfig<LocaleVariantEntity, LocaleVariantDto>.NewConfig();
        Mapster.TypeAdapterConfig<PageEntity, PageDto>.NewConfig();
    }
}
