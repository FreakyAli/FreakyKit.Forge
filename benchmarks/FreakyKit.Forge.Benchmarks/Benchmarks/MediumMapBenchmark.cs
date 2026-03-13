using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Mapster;

namespace ForgeBenchmarks;

/// <summary>
/// Benchmarks a flat 10-property mapping with mixed types (string, int, DateTime, decimal, bool).
/// Compares Forge, hand-written, AutoMapper, Mapperly, and Mapster.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class MediumMapBenchmark
{
    private MediumSource _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _ = AutoMapperSetup.Mapper;

        _source = new MediumSource
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Smith",
            Age = 28,
            Email = "jane@example.com",
            Phone = "+1-555-0199",
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0),
            IsActive = true,
            Balance = 1234.56m,
            Notes = "VIP customer with a long note that tests string copy performance across mapping boundaries"
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("Medium")]
    public MediumDestination HandWritten() => HandWrittenMappers.MapMedium(_source);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("Medium")]
    public MediumDestination ForgeGenerated() => MediumForges.Map(_source);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("Medium")]
    public MediumDestination Mapperly() => MapperlyMappers.MapMedium(_source);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("Medium")]
    public MediumDestination AutoMapper() => AutoMapperSetup.Mapper.Map<MediumDestination>(_source);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("Medium")]
    public MediumDestination Mapster() => _source.Adapt<MediumDestination>();
}
