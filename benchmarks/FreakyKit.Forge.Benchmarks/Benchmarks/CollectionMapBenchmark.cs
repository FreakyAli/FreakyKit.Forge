using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Mapster;

namespace ForgeBenchmarks;

/// <summary>
/// Benchmarks collection mapping with LINQ materialization (.ToArray(), .Select().ToList()).
/// Compares Forge, hand-written, AutoMapper, Mapperly, and Mapster across varying collection sizes.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class CollectionMapBenchmark
{
    [Params(1, 10, 100, 1000)]
    public int ItemCount { get; set; }

    private CollectionSource _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _ = AutoMapperSetup.Mapper;

        var tags = Enumerable.Range(0, ItemCount).Select(i => $"tag-{i}").ToList();
        var items = Enumerable.Range(0, ItemCount).Select(i => new OrderItem
        {
            Sku = $"SKU-{i:D6}",
            Quantity = i + 1,
            UnitPrice = 9.99m + i
        }).ToList();

        _source = new CollectionSource
        {
            Id = 1,
            Name = "Bulk Order",
            Tags = tags,
            Items = items
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("Collection")]
    public CollectionDestination HandWritten() => HandWrittenMappers.MapCollection(_source);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("Collection")]
    public CollectionDestination ForgeGenerated() => CollectionForges.Map(_source);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("Collection")]
    public CollectionDestination Mapperly() => MapperlyMappers.MapCollection(_source);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("Collection")]
    public CollectionDestination AutoMapper() => AutoMapperSetup.Mapper.Map<CollectionDestination>(_source);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("Collection")]
    public CollectionDestination Mapster() => _source.Adapt<CollectionDestination>();
}
