using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Facet.Extensions;
using Mapster;

namespace ForgeBenchmarks;

/// <summary>
/// Real-world ORM scenario: maps a database entity with many nullable columns
/// (int?, DateTime?, bool?, decimal?, string?) to a DTO.
/// This is what every Entity Framework / Dapper query produces.
/// Tests two data shapes: fully populated and sparse (many nulls).
/// Compares Forge, hand-written, AutoMapper, Mapperly, and Mapster.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class NullableEntityBenchmark
{
    private NullableUserEntity _fullyPopulated = null!;
    private NullableUserEntity _sparse = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _ = AutoMapperSetup.Mapper;

        // Fully populated — all nullable fields have values
        _fullyPopulated = new NullableUserEntity
        {
            Id = 42,
            Username = "jdoe",
            DisplayName = "John Doe",
            Email = "john@example.com",
            AvatarUrl = "https://avatars.example.com/jdoe.png",
            Bio = "Software engineer who loves mapping benchmarks",
            Age = 32,
            DateOfBirth = new DateTime(1992, 5, 15),
            LastLoginAt = new DateTime(2024, 3, 10, 8, 30, 0),
            CreatedAt = new DateTime(2020, 1, 1),
            IsVerified = true,
            IsAdmin = false,
            AccountBalance = 1234.56m,
            LoginCount = 847,
            Timezone = "America/New_York",
            Locale = "en-US"
        };

        // Sparse — many fields are null (common for new / incomplete accounts)
        _sparse = new NullableUserEntity
        {
            Id = 99,
            Username = "newuser",
            DisplayName = null,
            Email = "new@example.com",
            AvatarUrl = null,
            Bio = null,
            Age = null,
            DateOfBirth = null,
            LastLoginAt = null,
            CreatedAt = new DateTime(2024, 3, 12),
            IsVerified = null,
            IsAdmin = null,
            AccountBalance = null,
            LoginCount = null,
            Timezone = null,
            Locale = null
        };
    }

    // ── Fully Populated ──────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Hand-written (full)")]
    [BenchmarkCategory("NullableFull")]
    public NullableUserDto HandWrittenFull() => HandWrittenMappers.MapNullableUser(_fullyPopulated);

    [Benchmark(Description = "Forge (full)")]
    [BenchmarkCategory("NullableFull")]
    public NullableUserDto ForgeFull() => NullableForges.Map(_fullyPopulated);

    [Benchmark(Description = "Mapperly (full)")]
    [BenchmarkCategory("NullableFull")]
    public NullableUserDto MapperlyFull() => MapperlyMappers.MapNullableUser(_fullyPopulated);

    [Benchmark(Description = "AutoMapper (full)")]
    [BenchmarkCategory("NullableFull")]
    public NullableUserDto AutoMapperFull() => AutoMapperSetup.Mapper.Map<NullableUserDto>(_fullyPopulated);

    [Benchmark(Description = "Mapster (full)")]
    [BenchmarkCategory("NullableFull")]
    public NullableUserDto MapsterFull() => _fullyPopulated.Adapt<NullableUserDto>();

    [Benchmark(Description = "Facet (full)")]
    [BenchmarkCategory("NullableFull")]
    public NullableUserFacetDto FacetFull() => _fullyPopulated.ToFacet<NullableUserEntity, NullableUserFacetDto>();

    // ── Sparse (many nulls) ──────────────────────────────────

    [Benchmark(Baseline = true, Description = "Hand-written (sparse)")]
    [BenchmarkCategory("NullableSparse")]
    public NullableUserDto HandWrittenSparse() => HandWrittenMappers.MapNullableUser(_sparse);

    [Benchmark(Description = "Forge (sparse)")]
    [BenchmarkCategory("NullableSparse")]
    public NullableUserDto ForgeSparse() => NullableForges.Map(_sparse);

    [Benchmark(Description = "Mapperly (sparse)")]
    [BenchmarkCategory("NullableSparse")]
    public NullableUserDto MapperlySparse() => MapperlyMappers.MapNullableUser(_sparse);

    [Benchmark(Description = "AutoMapper (sparse)")]
    [BenchmarkCategory("NullableSparse")]
    public NullableUserDto AutoMapperSparse() => AutoMapperSetup.Mapper.Map<NullableUserDto>(_sparse);

    [Benchmark(Description = "Mapster (sparse)")]
    [BenchmarkCategory("NullableSparse")]
    public NullableUserDto MapsterSparse() => _sparse.Adapt<NullableUserDto>();

    [Benchmark(Description = "Facet (sparse)")]
    [BenchmarkCategory("NullableSparse")]
    public NullableUserFacetDto FacetSparse() => _sparse.ToFacet<NullableUserEntity, NullableUserFacetDto>();
}
