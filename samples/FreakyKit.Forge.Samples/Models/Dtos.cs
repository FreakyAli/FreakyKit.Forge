namespace FreakyKit.Forge.Samples;

// Basic mapping destination
public class PersonDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
}

// ForgeMap destination (Name mapped from FirstName)
public class PersonSummary
{
    [ForgeMap("FirstName")]
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public PersonStatusDto Status { get; set; }
}

// ForgeIgnore destination (InternalNotes excluded on source side)
public class PersonPublicDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
}

// Enum mapping destination
public class PersonWithStatusDto
{
    public string FirstName { get; set; } = "";
    public PersonStatusDto Status { get; set; }
}

// Nullable handling destination
public class PersonScoreDto
{
    public string FirstName { get; set; } = "";
    public int Score { get; set; }         // non-nullable ← nullable source
    public int? Age { get; set; }          // nullable ← non-nullable source
}

// Flattening destination (Address.City → HomeAddressCity)
public class PersonFlatDto
{
    public string FirstName { get; set; } = "";
    public string HomeAddressCity { get; set; } = "";
    public string HomeAddressState { get; set; } = "";
    public string HomeAddressZipCode { get; set; } = "";
}

// Nested forging destinations
public class AddressDto
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
}

public class PersonWithAddressDto
{
    public string FirstName { get; set; } = "";
    public AddressDto HomeAddress { get; set; } = new();
}

// Collection mapping destinations
public class OrderDto
{
    public int OrderId { get; set; }
    public string Product { get; set; } = "";
    public decimal Amount { get; set; }
}

public class OrderItemDto
{
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
}

public class PersonWithOrdersDto
{
    public string FirstName { get; set; } = "";
    public string[] Tags { get; set; } = [];
}

public class PersonWithOrderListDto
{
    public string FirstName { get; set; } = "";
    public List<OrderDto> Orders { get; set; } = [];
}

// Field mapping destination
public class MeasurementDto
{
    public string Label = "";
    public double Value;
    public string Unit = "";
}

// Constructor mapping destination (immutable via ctor)
public class PersonRecordDto
{
    public string Name { get; }
    public int Age { get; }
    public string Email { get; set; } = "";

    public PersonRecordDto(string name, int age)
    {
        Name = name;
        Age = age;
    }
}

// Converter destination
public class EventDto
{
    public string Title { get; set; } = "";
    public string OccurredAt { get; set; } = "";   // DateTime → string via converter
    public double Duration { get; set; }            // TimeSpan → double via converter
}

// Reverse mapping destination
public class PersonReverseDto
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
}

// Update mapping destination
public class PersonMutableDto
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
    public DateTime LastUpdated { get; set; }
}

// Explicit mode destination
public class PersonExplicitDto
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

// Private method destination
public class PersonInternalDto
{
    public string FirstName { get; set; } = "";
    public int Age { get; set; }
}
