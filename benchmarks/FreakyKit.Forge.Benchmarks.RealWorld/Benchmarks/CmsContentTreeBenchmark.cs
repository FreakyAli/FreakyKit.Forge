using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Facet.Extensions;
using ForgeBenchmarks.RealWorld.CmsContentTree;
using Mapster;

namespace ForgeBenchmarks.RealWorld.Benchmarks;

/// <summary>
/// CMS content tree scenario — page with a flat-but-parented block list (canonical CMS layout
/// pattern), locale variants for i18n, and mixed nullable media metadata. See
/// Scenarios/CmsContentTree.md.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class CmsContentTreeBenchmark
{
    private PageEntity _page = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterSetup.Configure();
        _ = AutoMapperSetup.Mapper;

        var heroId = Guid.NewGuid();
        var bodyContainerId = Guid.NewGuid();
        var ctaId = Guid.NewGuid();

        _page = new PageEntity
        {
            Id = Guid.Parse("a1b2c3d4-1111-2222-3333-444455556666"),
            Slug = "/blog/2024/how-we-scaled-our-mapper",
            Title = "How We Scaled Our Object Mapper",
            Locale = "en-US",
            Status = PageStatus.Published,
            CreatedAt = new DateTime(2024, 9, 10, 11, 30, 0),
            PublishedAt = new DateTime(2024, 9, 12, 14, 0, 0),
            AuthorName = "Jordan Lee",
            SeoDescription = "A deep-dive into refactoring our mapping layer for 10x throughput.",
            Blocks = new List<BlockEntity>
            {
                new() { Id = heroId, ParentBlockId = null, Type = BlockType.Image, OrderIndex = 0, MediaUrl = "/img/hero-mapper.jpg", MediaAltText = "Diagram of mapper architecture", MediaWidth = 1600, MediaHeight = 900, CssClass = "hero" },
                new() { Id = Guid.NewGuid(), ParentBlockId = null, Type = BlockType.Heading, OrderIndex = 1, TextContent = "How We Scaled Our Object Mapper", CssClass = "h1" },
                new() { Id = bodyContainerId, ParentBlockId = null, Type = BlockType.Container, OrderIndex = 2, CssClass = "body" },
                new() { Id = Guid.NewGuid(), ParentBlockId = bodyContainerId, Type = BlockType.Paragraph, OrderIndex = 0, TextContent = "Mapping objects used to be 30% of our request budget. Here's how we got it under 1%." },
                new() { Id = Guid.NewGuid(), ParentBlockId = bodyContainerId, Type = BlockType.Heading, OrderIndex = 1, TextContent = "The problem with reflection", CssClass = "h2" },
                new() { Id = Guid.NewGuid(), ParentBlockId = bodyContainerId, Type = BlockType.Paragraph, OrderIndex = 2, TextContent = "Every call paid for type lookup, member discovery, and emission cache misses..." },
                new() { Id = Guid.NewGuid(), ParentBlockId = bodyContainerId, Type = BlockType.CodeBlock, OrderIndex = 3, TextContent = "public PersonDto ToDto(Person p) => new PersonDto { Name = p.Name, Age = p.Age };", CssClass = "lang-csharp" },
                new() { Id = Guid.NewGuid(), ParentBlockId = bodyContainerId, Type = BlockType.Quote, OrderIndex = 4, TextContent = "If your hot path can't tolerate reflection, generate the code." },
                new() { Id = Guid.NewGuid(), ParentBlockId = bodyContainerId, Type = BlockType.Divider, OrderIndex = 5 },
                new() { Id = ctaId, ParentBlockId = null, Type = BlockType.Container, OrderIndex = 3, CssClass = "cta" },
                new() { Id = Guid.NewGuid(), ParentBlockId = ctaId, Type = BlockType.Heading, OrderIndex = 0, TextContent = "Try it yourself", CssClass = "h3" },
                new() { Id = Guid.NewGuid(), ParentBlockId = ctaId, Type = BlockType.Paragraph, OrderIndex = 1, TextContent = "Install the package from NuGet and follow the quick-start guide." },
            },
            LocaleVariants = new List<LocaleVariantEntity>
            {
                new() { Locale = "es-MX", Title = "Cómo escalamos nuestro mapeador de objetos", SeoDescription = "Un análisis a fondo del rediseño de nuestra capa de mapeo para un rendimiento 10x.", IsAutoTranslated = true },
                new() { Locale = "de-DE", Title = "Wie wir unseren Objekt-Mapper skalierten", SeoDescription = "Ein Deep-Dive zum Refactoring unserer Mapping-Schicht für 10-fachen Durchsatz.", IsAutoTranslated = true },
                new() { Locale = "ja-JP", Title = "オブジェクトマッパーを10倍にスケールした方法", SeoDescription = "マッピング層をリファクタリングして10倍のスループットを実現した方法の詳細解説。", IsAutoTranslated = false },
            },
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("CmsContentTree")]
    public PageDto HandWritten() => CmsHandWritten.MapPage(_page);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("CmsContentTree")]
    public PageDto ForgeGenerated() => CmsForges.MapPage(_page);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("CmsContentTree")]
    public PageDto Mapperly() => CmsMapperly.MapPage(_page);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("CmsContentTree")]
    public PageDto AutoMapper() => AutoMapperSetup.Mapper.Map<PageDto>(_page);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("CmsContentTree")]
    public PageDto Mapster() => _page.Adapt<PageDto>();

    [Benchmark(Description = "Facet")]
    [BenchmarkCategory("CmsContentTree")]
    public PageFacetDto Facet() => _page.ToFacet<PageEntity, PageFacetDto>();
}
