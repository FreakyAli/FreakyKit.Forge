using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Facet.Extensions;
using Mapster;

namespace ForgeBenchmarks;

/// <summary>
/// Real-world e-commerce scenario: maps an OrderEntity (with nested Customer,
/// ShippingAddress, LineItems collection, enums for Status/Payment, nullable
/// dates, mixed types) to an OrderDto — the kind of mapping every API does.
/// Compares Forge, hand-written, AutoMapper, Mapperly, and Mapster.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class EcommerceOrderBenchmark
{
    private OrderEntity _order = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _ = AutoMapperSetup.Mapper;

        _order = new OrderEntity
        {
            Id = 10042,
            OrderNumber = "ORD-2024-10042",
            Status = OrderStatus.Shipped,
            Payment = PaymentMethod.CreditCard,
            CreatedAt = new DateTime(2024, 3, 15, 14, 30, 0),
            ShippedAt = new DateTime(2024, 3, 17, 9, 0, 0),
            DeliveredAt = null,
            Subtotal = 299.97m,
            Tax = 24.00m,
            Total = 323.97m,
            Currency = "USD",
            Notes = "Please leave at front door",
            IsGift = false,
            Customer = new CustomerEntity
            {
                Id = 5001,
                FirstName = "Sarah",
                LastName = "Connor",
                Email = "sarah.connor@skynet.io",
                Phone = "+1-555-0142",
                BillingAddress = new Address
                {
                    Street = "2144 Laurel Canyon Blvd",
                    City = "Los Angeles",
                    State = "CA",
                    ZipCode = "90046"
                }
            },
            ShippingAddress = new Address
            {
                Street = "800 N Alameda St",
                City = "Los Angeles",
                State = "CA",
                ZipCode = "90012"
            },
            LineItems = Enumerable.Range(0, 5).Select(i => new LineItemEntity
            {
                ProductId = 1000 + i,
                ProductName = $"Widget {(char)('A' + i)}",
                Sku = $"WDG-{i:D4}",
                Quantity = i + 1,
                UnitPrice = 49.99m + i * 10,
                Discount = i > 2 ? 5.00m : 0m
            }).ToList(),
            Tags = ["priority", "west-coast", "returning-customer"]
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("EcommerceOrder")]
    public OrderDto HandWritten() => HandWrittenMappers.MapOrder(_order);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("EcommerceOrder")]
    public OrderDto ForgeGenerated() => EcommerceForges.MapOrder(_order);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("EcommerceOrder")]
    public OrderDto Mapperly() => MapperlyMappers.MapOrder(_order);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("EcommerceOrder")]
    public OrderDto AutoMapper() => AutoMapperSetup.Mapper.Map<OrderDto>(_order);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("EcommerceOrder")]
    public OrderDto Mapster() => _order.Adapt<OrderDto>();

    [Benchmark(Description = "Facet")]
    [BenchmarkCategory("EcommerceOrder")]
    public OrderFacetDto Facet() => _order.ToFacet<OrderEntity, OrderFacetDto>();
}
