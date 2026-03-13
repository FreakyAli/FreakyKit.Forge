using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Mapster;

namespace ForgeBenchmarks;

/// <summary>
/// Throughput benchmark: maps N objects in a tight loop.
/// Measures sustained mapping throughput and total memory pressure under batch workloads.
/// Compares Forge, hand-written, AutoMapper, Mapperly, and Mapster.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class ThroughputBenchmark
{
    [Params(100, 1_000, 10_000)]
    public int BatchSize { get; set; }

    private MediumSource[] _sources = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _ = AutoMapperSetup.Mapper;

        _sources = Enumerable.Range(0, BatchSize).Select(i => new MediumSource
        {
            Id = i,
            FirstName = $"First-{i}",
            LastName = $"Last-{i}",
            Age = 20 + (i % 60),
            Email = $"user{i}@example.com",
            Phone = $"+1-555-{i:D4}",
            CreatedAt = DateTime.UtcNow.AddDays(-i),
            IsActive = i % 3 != 0,
            Balance = 100m + i * 1.5m,
            Notes = $"Note for user {i} with some extra text to simulate real data"
        }).ToArray();
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("Throughput")]
    public MediumDestination[] HandWritten()
    {
        var results = new MediumDestination[_sources.Length];
        for (var i = 0; i < _sources.Length; i++)
            results[i] = HandWrittenMappers.MapMedium(_sources[i]);
        return results;
    }

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("Throughput")]
    public MediumDestination[] ForgeGenerated()
    {
        var results = new MediumDestination[_sources.Length];
        for (var i = 0; i < _sources.Length; i++)
            results[i] = MediumForges.Map(_sources[i]);
        return results;
    }

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("Throughput")]
    public MediumDestination[] MapperlyBench()
    {
        var results = new MediumDestination[_sources.Length];
        for (var i = 0; i < _sources.Length; i++)
            results[i] = MapperlyMappers.MapMedium(_sources[i]);
        return results;
    }

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("Throughput")]
    public MediumDestination[] AutoMapperBench()
    {
        var results = new MediumDestination[_sources.Length];
        for (var i = 0; i < _sources.Length; i++)
            results[i] = AutoMapperSetup.Mapper.Map<MediumDestination>(_sources[i]);
        return results;
    }

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("Throughput")]
    public MediumDestination[] MapsterBench()
    {
        var results = new MediumDestination[_sources.Length];
        for (var i = 0; i < _sources.Length; i++)
            results[i] = _sources[i].Adapt<MediumDestination>();
        return results;
    }
}
