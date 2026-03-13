using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Mapster;

namespace ForgeBenchmarks;

/// <summary>
/// Benchmarks nested object mapping where a child object (Address) is mapped via a separate forge method.
/// Compares Forge, hand-written, AutoMapper, Mapperly, and Mapster.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class NestedMapBenchmark
{
    private NestedSource _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _ = AutoMapperSetup.Mapper;

        _source = new NestedSource
        {
            Id = 7,
            Name = "Alice Johnson",
            Address = new Address
            {
                Street = "123 Main St",
                City = "Springfield",
                State = "IL",
                ZipCode = "62704"
            }
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("Nested")]
    public NestedDestination HandWritten() => HandWrittenMappers.MapNested(_source);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("Nested")]
    public NestedDestination ForgeGenerated() => NestedForges.Map(_source);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("Nested")]
    public NestedDestination Mapperly() => MapperlyMappers.MapNested(_source);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("Nested")]
    public NestedDestination AutoMapper() => AutoMapperSetup.Mapper.Map<NestedDestination>(_source);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("Nested")]
    public NestedDestination Mapster() => _source.Adapt<NestedDestination>();
}
