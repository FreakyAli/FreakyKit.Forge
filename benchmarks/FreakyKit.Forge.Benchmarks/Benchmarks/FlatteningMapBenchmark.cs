using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Facet.Extensions;
using Mapster;

namespace ForgeBenchmarks;

/// <summary>
/// Benchmarks property flattening: source.HomeAddress.City -> dest.HomeAddressCity.
/// Compares Forge, hand-written, AutoMapper, Mapperly, Mapster, and Facet.
/// Note: Facet maps nested objects (not flattened) since source models can't be modified.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class FlatteningMapBenchmark
{
    private FlatteningSource _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _ = AutoMapperSetup.Mapper;

        _source = new FlatteningSource
        {
            Name = "Charlie Brown",
            HomeAddress = new Address
            {
                Street = "1 Peanuts Lane",
                City = "Minneapolis",
                State = "MN",
                ZipCode = "55401"
            }
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("Flattening")]
    public FlatteningDestination HandWritten() => HandWrittenMappers.MapFlattening(_source);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("Flattening")]
    public FlatteningDestination ForgeGenerated() => FlatteningForges.Map(_source);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("Flattening")]
    public FlatteningDestination Mapperly() => MapperlyMappers.MapFlattening(_source);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("Flattening")]
    public FlatteningDestination AutoMapper() => AutoMapperSetup.Mapper.Map<FlatteningDestination>(_source);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("Flattening")]
    public FlatteningDestination Mapster() => _source.Adapt<FlatteningDestination>();

    [Benchmark(Description = "Facet")]
    [BenchmarkCategory("Flattening")]
    public FlatteningFacetDto Facet() => _source.ToFacet<FlatteningSource, FlatteningFacetDto>();
}
