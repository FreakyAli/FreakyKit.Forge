using AutoMapper;

namespace ForgeBenchmarks.RealWorld.B2BOrderFulfilment;

// All mapper configs for this scenario live in the scenario's namespace so the
// Forge generator can emit unqualified type references that resolve cleanly.

// ─── Forge ───────────────────────────────────────────────────────────────────

[global::FreakyKit.Forge.Forge]
public static partial class B2BForges
{
    public static partial AddressDto MapAddress(AddressEntity source);
    public static partial CustomerDto MapCustomer(CustomerEntity source);
    public static partial LineItemDto MapLineItem(LineItemEntity source);
    public static partial FulfilmentEventDto MapEvent(FulfilmentEventEntity source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial OrderDto MapOrder(OrderEntity source);
}

// ─── Hand-written baseline ───────────────────────────────────────────────────

public static class B2BHandWritten
{
    public static OrderDto MapOrder(OrderEntity source)
    {
        var dto = new OrderDto
        {
            Id = source.Id,
            OrderNumber = source.OrderNumber,
            PurchaseOrderNumber = source.PurchaseOrderNumber,
            Status = source.Status,
            CreatedAt = source.CreatedAt,
            ApprovedAt = source.ApprovedAt,
            ShippedAt = source.ShippedAt,
            DeliveredAt = source.DeliveredAt,
            Subtotal = source.Subtotal,
            TaxAmount = source.TaxAmount,
            ShippingCost = source.ShippingCost,
            TotalAmount = source.TotalAmount,
            Currency = source.Currency,
            PaymentTerms = source.PaymentTerms,
            WarehouseCode = source.WarehouseCode,
            IsRushOrder = source.IsRushOrder,
            Notes = source.Notes,
            Customer = new CustomerDto
            {
                Id = source.Customer.Id,
                CompanyName = source.Customer.CompanyName,
                ContactFirstName = source.Customer.ContactFirstName,
                ContactLastName = source.Customer.ContactLastName,
                Email = source.Customer.Email,
                Phone = source.Customer.Phone,
                TaxId = source.Customer.TaxId,
                Tier = source.Customer.Tier,
            },
            ShipToAddress = MapAddress(source.ShipToAddress),
            BillToAddress = MapAddress(source.BillToAddress),
            Lines = new List<LineItemDto>(source.Lines.Count),
            FulfilmentEvents = new List<FulfilmentEventDto>(source.FulfilmentEvents.Count),
        };

        foreach (var line in source.Lines)
        {
            dto.Lines.Add(new LineItemDto
            {
                Id = line.Id,
                Sku = line.Sku,
                ProductName = line.ProductName,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Discount = line.Discount,
                LineTotal = line.LineTotal,
            });
        }

        foreach (var evt in source.FulfilmentEvents)
        {
            dto.FulfilmentEvents.Add(new FulfilmentEventDto
            {
                Id = evt.Id,
                OccurredAt = evt.OccurredAt,
                EventType = evt.EventType,
                Description = evt.Description,
                Actor = evt.Actor,
            });
        }

        return dto;
    }

    private static AddressDto MapAddress(AddressEntity a) => new()
    {
        Line1 = a.Line1,
        Line2 = a.Line2,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country,
    };
}

// ─── Mapperly ────────────────────────────────────────────────────────────────

[Riok.Mapperly.Abstractions.Mapper]
public static partial class B2BMapperly
{
    public static partial AddressDto MapAddress(AddressEntity source);
    public static partial CustomerDto MapCustomer(CustomerEntity source);
    public static partial LineItemDto MapLineItem(LineItemEntity source);
    public static partial FulfilmentEventDto MapEvent(FulfilmentEventEntity source);
    public static partial OrderDto MapOrder(OrderEntity source);
}

// ─── AutoMapper profile ──────────────────────────────────────────────────────

public class B2BAutoMapperProfile : Profile
{
    public B2BAutoMapperProfile()
    {
        CreateMap<AddressEntity, AddressDto>();
        CreateMap<CustomerEntity, CustomerDto>();
        CreateMap<LineItemEntity, LineItemDto>();
        CreateMap<FulfilmentEventEntity, FulfilmentEventDto>();
        CreateMap<OrderEntity, OrderDto>();
    }
}

// ─── Mapster registration ────────────────────────────────────────────────────

public static class B2BMapsterConfig
{
    public static void Register()
    {
        Mapster.TypeAdapterConfig<AddressEntity, AddressDto>.NewConfig();
        Mapster.TypeAdapterConfig<CustomerEntity, CustomerDto>.NewConfig();
        Mapster.TypeAdapterConfig<LineItemEntity, LineItemDto>.NewConfig();
        Mapster.TypeAdapterConfig<FulfilmentEventEntity, FulfilmentEventDto>.NewConfig();
        Mapster.TypeAdapterConfig<OrderEntity, OrderDto>.NewConfig();
    }
}
