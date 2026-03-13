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
