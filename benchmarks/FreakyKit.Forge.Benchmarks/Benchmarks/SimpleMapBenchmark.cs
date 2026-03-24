using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Facet.Extensions;
using Mapster;

namespace ForgeBenchmarks;

/// <summary>
/// Benchmarks a flat 4-property mapping: the simplest scenario.
/// Compares Forge, hand-written, AutoMapper (reflection), Mapperly (source-gen), and Mapster (codegen).
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class SimpleMapBenchmark
{
    private SimpleSource _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _ = AutoMapperSetup.Mapper;

        _source = new SimpleSource
        {
            Id = 42,
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com"
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("Simple")]
    public SimpleDestination HandWritten() => HandWrittenMappers.MapSimple(_source);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("Simple")]
    public SimpleDestination ForgeGenerated() => SimpleForges.Map(_source);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("Simple")]
    public SimpleDestination Mapperly() => MapperlyMappers.MapSimple(_source);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("Simple")]
    public SimpleDestination AutoMapper() => AutoMapperSetup.Mapper.Map<SimpleDestination>(_source);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("Simple")]
    public SimpleDestination Mapster() => _source.Adapt<SimpleDestination>();

    [Benchmark(Description = "Facet")]
    [BenchmarkCategory("Simple")]
    public SimpleFacetDto Facet() => _source.ToFacet<SimpleSource, SimpleFacetDto>();
}
