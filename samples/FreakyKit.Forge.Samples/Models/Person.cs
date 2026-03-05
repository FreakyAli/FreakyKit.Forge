namespace FreakyKit.Forge.Samples;

// Primary domain model used across most samples
public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
    public string InternalNotes { get; set; } = "";
    public int? Score { get; set; }
    public PersonStatus Status { get; set; }
    public Address HomeAddress { get; set; } = new();
    public List<string> Tags { get; set; } = [];
    public List<Order> Orders { get; set; } = [];
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

public class Order
{
    public int OrderId { get; set; }
    public string Product { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// Model with public fields (for field mapping demo)
public class Measurement
{
    public string Label = "";
    public double Value;
    public string Unit = "";
}

// Model for constructor demo (immutable destination)
public class PersonRecord
{
    public string Name { get; }
    public int Age { get; }
    public string Email { get; set; } = "";

    public PersonRecord(string name, int age)
    {
        Name = name;
        Age = age;
    }
}

// Model for converter demo
public class Event
{
    public string Title { get; set; } = "";
    public DateTime OccurredAt { get; set; }
    public TimeSpan Duration { get; set; }
}
