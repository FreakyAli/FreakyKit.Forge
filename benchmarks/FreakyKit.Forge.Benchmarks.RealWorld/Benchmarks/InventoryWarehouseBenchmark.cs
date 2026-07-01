using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Facet.Extensions;
using ForgeBenchmarks.RealWorld.InventoryWarehouse;
using Mapster;

namespace ForgeBenchmarks.RealWorld.Benchmarks;

/// <summary>
/// Inventory / warehouse movement scenario — SKU detail with multiple warehouses, each with
/// multiple bin locations. Tests collection-of-collections mapping plus a parallel movement
/// history collection. See Scenarios/InventoryWarehouse.md.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class InventoryWarehouseBenchmark
{
    private SkuEntity _sku = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterSetup.Configure();
        _ = AutoMapperSetup.Mapper;

        _sku = new SkuEntity
        {
            Id = 88421,
            SkuCode = "WIDGET-PRO-V2",
            ProductName = "Professional Widget Mark II",
            UnitOfMeasure = "EA",
            UnitWeight = 1.85m,
            IsHazardous = false,
            IsRefrigerated = false,
            Warehouses = new List<WarehouseStockEntity>
            {
                new()
                {
                    WarehouseCode = "WH-CHI-02",
                    OnHand = 482,
                    Reserved = 47,
                    Inbound = 200,
                    Bins = new List<BinLocationEntity>
                    {
                        new() { Aisle = "A", Bay = "12", Level = "3", Quantity = 144, ExpiresOn = null, LotNumber = "LOT-24Q3-001" },
                        new() { Aisle = "A", Bay = "12", Level = "4", Quantity = 144, ExpiresOn = null, LotNumber = "LOT-24Q3-001" },
                        new() { Aisle = "B", Bay = "07", Level = "2", Quantity = 144, ExpiresOn = null, LotNumber = "LOT-24Q3-002" },
                        new() { Aisle = "B", Bay = "07", Level = "3", Quantity = 50, ExpiresOn = null, LotNumber = "LOT-24Q3-002" },
                    },
                },
                new()
                {
                    WarehouseCode = "WH-LAX-01",
                    OnHand = 218,
                    Reserved = 12,
                    Inbound = 0,
                    Bins = new List<BinLocationEntity>
                    {
                        new() { Aisle = "C", Bay = "03", Level = "1", Quantity = 144, ExpiresOn = null, LotNumber = "LOT-24Q2-007" },
                        new() { Aisle = "C", Bay = "03", Level = "2", Quantity = 74, ExpiresOn = null, LotNumber = "LOT-24Q2-007" },
                    },
                },
                new()
                {
                    WarehouseCode = "WH-NYC-05",
                    OnHand = 96,
                    Reserved = 96,
                    Inbound = 144,
                    Bins = new List<BinLocationEntity>
                    {
                        new() { Aisle = "D", Bay = "19", Level = "5", Quantity = 96, ExpiresOn = null, LotNumber = "LOT-24Q3-003" },
                    },
                },
            },
            Movements = new List<MovementEntity>
            {
                new() { Id = Guid.NewGuid(), At = new DateTime(2024, 9, 15, 8, 0, 0), Type = MovementType.Receipt, Quantity = 144, FromLocation = "DOCK-1", ToLocation = "WH-CHI-02 A-12-3", Reference = "PO-77821", Operator = "rec-001" },
                new() { Id = Guid.NewGuid(), At = new DateTime(2024, 9, 15, 14, 30, 0), Type = MovementType.Putaway, Quantity = 144, FromLocation = "DOCK-1", ToLocation = "WH-CHI-02 A-12-4", Reference = "PO-77821", Operator = "rec-001" },
                new() { Id = Guid.NewGuid(), At = new DateTime(2024, 9, 16, 9, 15, 0), Type = MovementType.Pick, Quantity = 12, FromLocation = "WH-CHI-02 A-12-3", ToLocation = "STAGING-01", Reference = "SO-99001", Operator = "pick-station-3" },
                new() { Id = Guid.NewGuid(), At = new DateTime(2024, 9, 16, 11, 0, 0), Type = MovementType.Pick, Quantity = 8, FromLocation = "WH-CHI-02 A-12-3", ToLocation = "STAGING-01", Reference = "SO-99002", Operator = "pick-station-3" },
                new() { Id = Guid.NewGuid(), At = new DateTime(2024, 9, 16, 16, 45, 0), Type = MovementType.Shipment, Quantity = 20, FromLocation = "STAGING-01", ToLocation = "DOCK-2", Reference = "MANIFEST-441", Operator = "ship-dock" },
                new() { Id = Guid.NewGuid(), At = new DateTime(2024, 9, 17, 10, 0, 0), Type = MovementType.Cycle, Quantity = 1, FromLocation = "WH-LAX-01 C-03-1", ToLocation = "WH-LAX-01 C-03-1", Reference = "CC-2024-09-17", Operator = "auditor-04" },
                new() { Id = Guid.NewGuid(), At = new DateTime(2024, 9, 17, 13, 20, 0), Type = MovementType.Move, Quantity = 24, FromLocation = "WH-LAX-01 C-03-2", ToLocation = "WH-LAX-01 C-03-1", Reference = "MOVE-78", Operator = "fork-op-2" },
                new() { Id = Guid.NewGuid(), At = new DateTime(2024, 9, 18, 8, 30, 0), Type = MovementType.Adjustment, Quantity = -2, FromLocation = "WH-NYC-05 D-19-5", ToLocation = "DAMAGE-01", Reference = "DMG-2024-09-18-01", Operator = "supv-77" },
            },
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("InventoryWarehouse")]
    public SkuDto HandWritten() => InventoryHandWritten.MapSku(_sku);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("InventoryWarehouse")]
    public SkuDto ForgeGenerated() => InventoryForges.MapSku(_sku);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("InventoryWarehouse")]
    public SkuDto Mapperly() => InventoryMapperly.MapSku(_sku);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("InventoryWarehouse")]
    public SkuDto AutoMapper() => AutoMapperSetup.Mapper.Map<SkuDto>(_sku);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("InventoryWarehouse")]
    public SkuDto Mapster() => _sku.Adapt<SkuDto>();

    [Benchmark(Description = "Facet")]
    [BenchmarkCategory("InventoryWarehouse")]
    public SkuFacetDto Facet() => _sku.ToFacet<SkuEntity, SkuFacetDto>();
}
