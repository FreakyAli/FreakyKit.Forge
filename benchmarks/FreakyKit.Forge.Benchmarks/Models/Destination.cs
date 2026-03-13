namespace ForgeBenchmarks;

// ──────────────────────────────────────────────────────────────
//  Destination models — these represent the "DTO" side of mapping
// ──────────────────────────────────────────────────────────────

public class SimpleDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
}

public class MediumDestination
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public decimal Balance { get; set; }
    public string Notes { get; set; } = "";
}

public class AddressDto
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

public class NestedDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AddressDto Address { get; set; } = new();
}

public class OrderItemDto
{
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class CollectionDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string[] Tags { get; set; } = [];
    public List<OrderItemDto> Items { get; set; } = [];
}

public class DeepGraphDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public AddressDto HomeAddress { get; set; } = new();
    public AddressDto WorkAddress { get; set; } = new();
    public List<OrderItemDto> RecentOrders { get; set; } = [];
    public string[] Tags { get; set; } = [];
}

public class FlatteningDestination
{
    public string Name { get; set; } = "";
    public string HomeAddressStreet { get; set; } = "";
    public string HomeAddressCity { get; set; } = "";
    public string HomeAddressState { get; set; } = "";
    public string HomeAddressZipCode { get; set; } = "";
}

// ──────────────────────────────────────────────────────────────
//  E-Commerce Order DTOs — API response models
// ──────────────────────────────────────────────────────────────

public class CustomerDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public AddressDto BillingAddress { get; set; } = new();
}

public class LineItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public OrderStatus Status { get; set; }
    public PaymentMethod Payment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Notes { get; set; }
    public bool IsGift { get; set; }
    public CustomerDto Customer { get; set; } = new();
    public AddressDto ShippingAddress { get; set; } = new();
    public List<LineItemDto> LineItems { get; set; } = [];
    public string[] Tags { get; set; } = [];
}

// ──────────────────────────────────────────────────────────────
//  Nullable User DTO — clean API model with non-null defaults
// ──────────────────────────────────────────────────────────────

public class NullableUserDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public int? Age { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool? IsVerified { get; set; }
    public bool? IsAdmin { get; set; }
    public decimal? AccountBalance { get; set; }
    public int? LoginCount { get; set; }
    public string? Timezone { get; set; }
    public string? Locale { get; set; }
}
