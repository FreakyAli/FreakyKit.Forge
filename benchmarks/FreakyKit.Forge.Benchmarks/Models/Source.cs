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
