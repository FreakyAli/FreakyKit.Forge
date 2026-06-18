using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Facet.Extensions;
using ForgeBenchmarks.RealWorld.B2BOrderFulfilment;
using Mapster;

namespace ForgeBenchmarks.RealWorld.Benchmarks;

/// <summary>
/// B2B order fulfilment scenario — large flat DTO (~20 props) plus nested customer, two address
/// blocks, line-item collection, and fulfilment event audit trail. Shape is representative of
/// order-management systems in ERP / commerce stacks. See Scenarios/B2BOrderFulfilment.md for
/// provenance and design notes.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class B2BOrderFulfilmentBenchmark
{
    private OrderEntity _order = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterSetup.Configure();
        _ = AutoMapperSetup.Mapper;

        _order = new OrderEntity
        {
            Id = 100245,
            OrderNumber = "ORD-2024-100245",
            PurchaseOrderNumber = "PO-AC-78821",
            Status = OrderStatus.Shipped,
            CreatedAt = new DateTime(2024, 9, 1, 13, 45, 0),
            ApprovedAt = new DateTime(2024, 9, 1, 14, 5, 0),
            ShippedAt = new DateTime(2024, 9, 3, 8, 30, 0),
            DeliveredAt = null,
            Subtotal = 4823.50m,
            TaxAmount = 482.35m,
            ShippingCost = 95.00m,
            TotalAmount = 5400.85m,
            Currency = "USD",
            PaymentTerms = "Net 30",
            WarehouseCode = "WH-CHI-02",
            IsRushOrder = false,
            Notes = "Deliver to dock 4. Call ahead.",
            Customer = new CustomerEntity
            {
                Id = 7821,
                CompanyName = "Acme Industrial Supplies, Inc.",
                ContactFirstName = "Janet",
                ContactLastName = "Reyes",
                Email = "jreyes@acme-industrial.example",
                Phone = "+1-312-555-9120",
                TaxId = "98-7654321",
                Tier = CustomerTier.Preferred,
            },
            ShipToAddress = new AddressEntity
            {
                Line1 = "1455 W Pershing Rd",
                Line2 = "Dock 4",
                City = "Chicago",
                State = "IL",
                PostalCode = "60609",
                Country = "USA",
            },
            BillToAddress = new AddressEntity
            {
                Line1 = "1455 W Pershing Rd",
                Line2 = "Attn: Accounts Payable",
                City = "Chicago",
                State = "IL",
                PostalCode = "60609",
                Country = "USA",
            },
            Lines = Enumerable.Range(0, 12).Select(i => new LineItemEntity
            {
                Id = 9000 + i,
                Sku = $"SKU-{i:D5}",
                ProductName = $"Industrial Widget Series {(char)('A' + i % 8)}",
                Quantity = (i % 5) + 1,
                UnitPrice = 49.99m + i * 12.50m,
                Discount = i % 3 == 0 ? 5.00m : 0m,
                LineTotal = ((i % 5) + 1) * (49.99m + i * 12.50m) - (i % 3 == 0 ? 5.00m : 0m),
            }).ToList(),
            FulfilmentEvents = new List<FulfilmentEventEntity>
            {
                new() { Id = 1, OccurredAt = new DateTime(2024, 9, 1, 13, 45, 0), EventType = FulfilmentEventType.Created, Description = "Order created", Actor = "edi-import" },
                new() { Id = 2, OccurredAt = new DateTime(2024, 9, 1, 14, 5, 0), EventType = FulfilmentEventType.Approved, Description = "Auto-approved (Preferred tier)", Actor = "approval-bot" },
                new() { Id = 3, OccurredAt = new DateTime(2024, 9, 2, 7, 15, 0), EventType = FulfilmentEventType.PickStarted, Description = "Picking started", Actor = "pick-station-3" },
                new() { Id = 4, OccurredAt = new DateTime(2024, 9, 2, 9, 30, 0), EventType = FulfilmentEventType.PickCompleted, Description = "Pick complete (12 lines)", Actor = "pick-station-3" },
                new() { Id = 5, OccurredAt = new DateTime(2024, 9, 2, 14, 0, 0), EventType = FulfilmentEventType.Packed, Description = "Packed in 2 cartons", Actor = "pack-station-1" },
                new() { Id = 6, OccurredAt = new DateTime(2024, 9, 3, 8, 30, 0), EventType = FulfilmentEventType.Shipped, Description = "Handed to carrier", Actor = "shipping-dock" },
            },
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("B2BOrderFulfilment")]
    public OrderDto HandWritten() => B2BHandWritten.MapOrder(_order);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("B2BOrderFulfilment")]
    public OrderDto ForgeGenerated() => B2BForges.MapOrder(_order);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("B2BOrderFulfilment")]
    public OrderDto Mapperly() => B2BMapperly.MapOrder(_order);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("B2BOrderFulfilment")]
    public OrderDto AutoMapper() => AutoMapperSetup.Mapper.Map<OrderDto>(_order);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("B2BOrderFulfilment")]
    public OrderDto Mapster() => _order.Adapt<OrderDto>();

    [Benchmark(Description = "Facet")]
    [BenchmarkCategory("B2BOrderFulfilment")]
    public OrderFacetDto Facet() => _order.ToFacet<OrderEntity, OrderFacetDto>();
}
