using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Facet.Extensions;
using ForgeBenchmarks.RealWorld.PublicApiResponse;
using Mapster;

namespace ForgeBenchmarks.RealWorld.Benchmarks;

/// <summary>
/// Public REST API paged envelope — generic wrapper around a resource collection with
/// HATEOAS-style links at both envelope and resource level, plus metadata. Tests
/// generic-wrapper + nested resource list + dual-level Links collections. See
/// Scenarios/PublicApiResponse.md.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class PublicApiResponseBenchmark
{
    private PagedResponseEntity _response = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterSetup.Configure();
        _ = AutoMapperSetup.Mapper;

        _response = new PagedResponseEntity
        {
            Page = 3,
            PageSize = 20,
            TotalCount = 412,
            TotalPages = 21,
            GeneratedAt = new DateTime(2024, 9, 18, 16, 22, 5),
            RequestId = "req-7c3f8a91-2244-4eaf-bd1f-29b88c4d3a7e",
            Links = new List<LinkEntity>
            {
                new() { Rel = "self", Href = "/api/v3/articles?page=3&pageSize=20", Method = "GET", Title = null },
                new() { Rel = "first", Href = "/api/v3/articles?page=1&pageSize=20", Method = "GET", Title = null },
                new() { Rel = "prev", Href = "/api/v3/articles?page=2&pageSize=20", Method = "GET", Title = null },
                new() { Rel = "next", Href = "/api/v3/articles?page=4&pageSize=20", Method = "GET", Title = null },
                new() { Rel = "last", Href = "/api/v3/articles?page=21&pageSize=20", Method = "GET", Title = null },
            },
            Meta = new MetaEntity
            {
                ApiVersion = "3.4.0",
                Deprecation = "v2 endpoints sunset 2025-06-01",
                Warning = null,
                RateLimitRemaining = 4827,
                RateLimitTotal = 5000,
                RateLimitResetAt = new DateTime(2024, 9, 18, 17, 0, 0),
            },
            Items = Enumerable.Range(0, 20).Select(i => new ResourceEntity
            {
                Id = $"art-{1000 + i:D5}",
                Type = "Article",
                Title = $"Sample article #{i + 1}",
                Description = "Short summary of the article content; usually 1–2 sentences for list views.",
                CreatedAt = new DateTime(2024, 8, 1).AddHours(i * 6),
                UpdatedAt = new DateTime(2024, 9, 10).AddHours(i * 2),
                Author = $"Author {(char)('A' + i % 8)}",
                Version = 1 + (i % 3),
                Visibility = (ResourceVisibility)(i % 3),
                Links = new List<LinkEntity>
                {
                    new() { Rel = "self", Href = $"/api/v3/articles/art-{1000 + i:D5}", Method = "GET", Title = null },
                    new() { Rel = "author", Href = $"/api/v3/authors/{i % 8}", Method = "GET", Title = "View author profile" },
                    new() { Rel = "comments", Href = $"/api/v3/articles/art-{1000 + i:D5}/comments", Method = "GET", Title = null },
                },
                Categories = new List<string> { "engineering", "scaling", i % 2 == 0 ? "performance" : "architecture" },
            }).ToList(),
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("PublicApiResponse")]
    public PagedResponseDto HandWritten() => PublicApiHandWritten.MapResponse(_response);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("PublicApiResponse")]
    public PagedResponseDto ForgeGenerated() => PublicApiForges.MapResponse(_response);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("PublicApiResponse")]
    public PagedResponseDto Mapperly() => PublicApiMapperly.MapResponse(_response);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("PublicApiResponse")]
    public PagedResponseDto AutoMapper() => AutoMapperSetup.Mapper.Map<PagedResponseDto>(_response);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("PublicApiResponse")]
    public PagedResponseDto Mapster() => _response.Adapt<PagedResponseDto>();

    [Benchmark(Description = "Facet")]
    [BenchmarkCategory("PublicApiResponse")]
    public PagedResponseFacetDto Facet() => _response.ToFacet<PagedResponseEntity, PagedResponseFacetDto>();
}
