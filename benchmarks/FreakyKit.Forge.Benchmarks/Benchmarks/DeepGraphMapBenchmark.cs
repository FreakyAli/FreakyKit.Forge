using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Mapster;

namespace ForgeBenchmarks;

/// <summary>
/// Benchmarks a realistic deep object graph: scalar props + 2 nested objects + 2 collections.
/// Compares Forge, hand-written, AutoMapper, Mapperly, and Mapster.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class DeepGraphMapBenchmark
{
    private DeepGraphSource _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _ = AutoMapperSetup.Mapper;

        _source = new DeepGraphSource
        {
            Id = 99,
            Name = "Bob Wilson",
            Email = "bob@company.com",
            Age = 45,
            IsActive = true,
            CreatedAt = new DateTime(2023, 6, 15),
            HomeAddress = new Address
            {
                Street = "456 Oak Ave",
                City = "Portland",
                State = "OR",
                ZipCode = "97201"
            },
            WorkAddress = new Address
            {
                Street = "789 Corporate Blvd",
                City = "Portland",
                State = "OR",
                ZipCode = "97204"
            },
            RecentOrders = Enumerable.Range(0, 25).Select(i => new OrderItem
            {
                Sku = $"PROD-{i:D4}",
                Quantity = i + 1,
                UnitPrice = 19.99m + i * 5
            }).ToList(),
            Tags = ["vip", "enterprise", "tier-1", "west-coast", "early-adopter"]
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("DeepGraph")]
    public DeepGraphDestination HandWritten() => HandWrittenMappers.MapDeepGraph(_source);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("DeepGraph")]
    public DeepGraphDestination ForgeGenerated() => DeepGraphForges.Map(_source);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("DeepGraph")]
    public DeepGraphDestination Mapperly() => MapperlyMappers.MapDeepGraph(_source);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("DeepGraph")]
    public DeepGraphDestination AutoMapper() => AutoMapperSetup.Mapper.Map<DeepGraphDestination>(_source);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("DeepGraph")]
    public DeepGraphDestination Mapster() => _source.Adapt<DeepGraphDestination>();
}
