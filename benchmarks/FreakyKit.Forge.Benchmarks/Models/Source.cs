namespace ForgeBenchmarks;

// ──────────────────────────────────────────────────────────────
//  Source models — these represent the "domain" side of mapping
// ──────────────────────────────────────────────────────────────

public class SimpleSource
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
}

public class MediumSource
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

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

public class NestedSource
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Address Address { get; set; } = new();
}

public class OrderItem
{
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class CollectionSource
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public List<OrderItem> Items { get; set; } = [];
}

public class DeepGraphSource
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Address HomeAddress { get; set; } = new();
    public Address WorkAddress { get; set; } = new();
    public List<OrderItem> RecentOrders { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}

public class FlatteningSource
{
    public string Name { get; set; } = "";
    public Address HomeAddress { get; set; } = new();
}

// ──────────────────────────────────────────────────────────────
//  Real-world scenario: E-Commerce Order (API response mapping)
//  Enums, nested objects, collections, mixed types — what you
//  actually see in production codebases.
// ──────────────────────────────────────────────────────────────

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled,
    Refunded
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    PayPal,
    BankTransfer,
    Crypto
}

public class CustomerEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public Address BillingAddress { get; set; } = new();
}

public class LineItemEntity
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
}

public class OrderEntity
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
    public CustomerEntity Customer { get; set; } = new();
    public Address ShippingAddress { get; set; } = new();
    public List<LineItemEntity> LineItems { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}

// ──────────────────────────────────────────────────────────────
//  Real-world scenario: Nullable Database Entity
//  Simulates an ORM entity loaded from a database where most
//  columns are nullable — maps to a clean API DTO with defaults.
// ──────────────────────────────────────────────────────────────

public class NullableUserEntity
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
