namespace ForgeBenchmarks.RealWorld.B2BOrderFulfilment;

// ─── Source entities ─────────────────────────────────────────────────────────

public class OrderEntity
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string PurchaseOrderNumber { get; set; } = "";
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "";
    public string PaymentTerms { get; set; } = "";
    public string WarehouseCode { get; set; } = "";
    public bool IsRushOrder { get; set; }
    public string? Notes { get; set; }
    public CustomerEntity Customer { get; set; } = null!;
    public AddressEntity ShipToAddress { get; set; } = null!;
    public AddressEntity BillToAddress { get; set; } = null!;
    public List<LineItemEntity> Lines { get; set; } = new();
    public List<FulfilmentEventEntity> FulfilmentEvents { get; set; } = new();
}

public class CustomerEntity
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = "";
    public string ContactFirstName { get; set; } = "";
    public string ContactLastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string TaxId { get; set; } = "";
    public CustomerTier Tier { get; set; }
}

public class AddressEntity
{
    public string Line1 { get; set; } = "";
    public string? Line2 { get; set; }
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "";
}

public class LineItemEntity
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
}

public class FulfilmentEventEntity
{
    public int Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public FulfilmentEventType EventType { get; set; }
    public string Description { get; set; } = "";
    public string Actor { get; set; } = "";
}

public enum OrderStatus { Draft, Submitted, Approved, Packing, Shipped, Delivered, Cancelled }
public enum CustomerTier { Standard, Preferred, Strategic }
public enum FulfilmentEventType { Created, Approved, PickStarted, PickCompleted, Packed, Shipped, OutForDelivery, Delivered, Exception }

// ─── DTOs ────────────────────────────────────────────────────────────────────

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string PurchaseOrderNumber { get; set; } = "";
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "";
    public string PaymentTerms { get; set; } = "";
    public string WarehouseCode { get; set; } = "";
    public bool IsRushOrder { get; set; }
    public string? Notes { get; set; }
    public CustomerDto Customer { get; set; } = null!;
    public AddressDto ShipToAddress { get; set; } = null!;
    public AddressDto BillToAddress { get; set; } = null!;
    public List<LineItemDto> Lines { get; set; } = new();
    public List<FulfilmentEventDto> FulfilmentEvents { get; set; } = new();
}

public class CustomerDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = "";
    public string ContactFirstName { get; set; } = "";
    public string ContactLastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string TaxId { get; set; } = "";
    public CustomerTier Tier { get; set; }
}

public class AddressDto
{
    public string Line1 { get; set; } = "";
    public string? Line2 { get; set; }
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "";
}

public class LineItemDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
}

public class FulfilmentEventDto
{
    public int Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public FulfilmentEventType EventType { get; set; }
    public string Description { get; set; } = "";
    public string Actor { get; set; } = "";
}

// Facet DTO — Facet generates the projection at compile time from the source type
[Facet.Facet(typeof(CustomerEntity))]
public partial class CustomerFacetDto;

[Facet.Facet(typeof(AddressEntity))]
public partial class AddressFacetDto;

[Facet.Facet(typeof(LineItemEntity))]
public partial class LineItemFacetDto;

[Facet.Facet(typeof(FulfilmentEventEntity))]
public partial class FulfilmentEventFacetDto;

[Facet.Facet(typeof(OrderEntity), NestedFacets = [typeof(CustomerFacetDto), typeof(AddressFacetDto), typeof(LineItemFacetDto), typeof(FulfilmentEventFacetDto)])]
public partial class OrderFacetDto;
