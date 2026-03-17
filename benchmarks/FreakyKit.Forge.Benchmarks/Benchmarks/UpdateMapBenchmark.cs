using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Mapster;

namespace ForgeBenchmarks;

/// <summary>
/// Benchmarks update (void) mapping: assigns source props onto an existing object, no allocation.
/// Compares Forge, hand-written, AutoMapper, and Mapster.
/// Mapperly: excluded — doesn't support void update out of the box.
/// Facet: excluded — creates new objects only, doesn't support in-place mutation.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class UpdateMapBenchmark
{
    private SimpleSource _source = null!;
    private SimpleDestination _existing = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _ = AutoMapperSetup.Mapper;

        _source = new SimpleSource
        {
            Id = 1,
            Name = "Updated Name",
            Age = 35,
            Email = "updated@example.com"
        };
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _existing = new SimpleDestination
        {
            Id = 0,
            Name = "Old Name",
            Age = 25,
            Email = "old@example.com"
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("Update")]
    public void HandWritten() => HandWrittenMappers.UpdateSimple(_source, _existing);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("Update")]
    public void ForgeGenerated() => UpdateForges.Update(_source, _existing);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("Update")]
    public void AutoMapper() => AutoMapperSetup.Mapper.Map(_source, _existing);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("Update")]
    public void Mapster() => _source.Adapt(_existing);
}
