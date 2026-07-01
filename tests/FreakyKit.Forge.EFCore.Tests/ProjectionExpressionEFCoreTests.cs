using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using FreakyKit.Forge;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FreakyKit.Forge.EFCore.Tests;

/// <summary>
/// End-to-end tests for the projection expressions feature against a real EF Core 8 provider.
/// Uses Sqlite in-memory so SQL translation is actually exercised (the InMemory provider doesn't
/// translate — it falls back to LINQ-to-Objects and would mask translation failures).
///
/// Each test seeds rows, projects via the generated <c>Expression&lt;Func&lt;,&gt;&gt;</c>, and asserts
/// the result matches the imperative method's output. If EF can't translate the expression, the
/// query throws <c>InvalidOperationException</c> with a "could not be translated" message — that's
/// the failure mode this whole project exists to catch.
/// </summary>
public sealed class ProjectionExpressionEFCoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContext _db;

    public ProjectionExpressionEFCoreTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new TestDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // ─── Phase 1: simple flat mapping ─────────────────────────────────────────

    [Fact]
    public void Phase1_SimpleFlatProjection_TranslatesAndMatchesImperative()
    {
        _db.People.AddRange(
            new Person { Name = "Alice", Age = 30 },
            new Person { Name = "Bob", Age = 25 });
        _db.SaveChanges();

        var viaExpression = _db.People.Select(PersonForges.ToDtoExpression).OrderBy(d => d.Name).ToList();
        var viaImperative = _db.People.OrderBy(p => p.Name).AsEnumerable().Select(PersonForges.ToDto).ToList();

        Assert.Equal(viaImperative.Count, viaExpression.Count);
        for (int i = 0; i < viaExpression.Count; i++)
        {
            Assert.Equal(viaImperative[i].Name, viaExpression[i].Name);
            Assert.Equal(viaImperative[i].Age, viaExpression[i].Age);
        }
    }

    // ─── Phase 2: nullable handling ───────────────────────────────────────────

    [Fact]
    public void Phase2_NullableProjection_TranslatesWithGetValueOrDefault()
    {
        _db.Widgets.AddRange(
            new Widget { Name = "Alpha", Score = 42 },
            new Widget { Name = "Beta", Score = null });
        _db.SaveChanges();

        var dtos = _db.Widgets.Select(WidgetForges.ToDtoExpression).OrderBy(d => d.Name).ToList();

        Assert.Equal(2, dtos.Count);
        Assert.Equal(42, dtos[0].Score);
        Assert.Equal(0, dtos[1].Score); // GetValueOrDefault on null → 0
    }

    // ─── Phase 3a: enum cast ──────────────────────────────────────────────────

    [Fact]
    public void Phase3_EnumCast_TranslatesAndMatchesImperative()
    {
        _db.Tickets.AddRange(
            new Ticket { Title = "T1", Severity = SrcSeverity.Low },
            new Ticket { Title = "T2", Severity = SrcSeverity.High });
        _db.SaveChanges();

        var dtos = _db.Tickets.Select(TicketForges.ToDtoCastExpression).OrderBy(d => d.Title).ToList();

        Assert.Equal(DestSeverity.Low, dtos[0].Severity);
        Assert.Equal(DestSeverity.High, dtos[1].Severity);
    }

    // ─── Phase 3b: enum ByName chained ternary ────────────────────────────────

    [Fact]
    public void Phase3_EnumByName_ChainedTernary_TranslatesToCaseWhen()
    {
        _db.Tickets.AddRange(
            new Ticket { Title = "T1", Severity = SrcSeverity.Low },
            new Ticket { Title = "T2", Severity = SrcSeverity.Medium },
            new Ticket { Title = "T3", Severity = SrcSeverity.High });
        _db.SaveChanges();

        // This is the critical test — if EF Core 8 can't translate the chained ternary to CASE WHEN,
        // this throws "could not be translated" and we know to revisit the design.
        var dtos = _db.Tickets.Select(TicketForges.ToDtoByNameExpression).OrderBy(d => d.Title).ToList();

        Assert.Equal(DestSeverity.Low, dtos[0].Severity);
        Assert.Equal(DestSeverity.Medium, dtos[1].Severity);
        Assert.Equal(DestSeverity.High, dtos[2].Severity);
    }

    // ─── Phase 4: parameterized constructor ───────────────────────────────────

    [Fact]
    public void Phase4_ParameterizedCtor_RecordPositional_Translates()
    {
        _db.People.AddRange(
            new Person { Name = "Carol", Age = 40 },
            new Person { Name = "Dave", Age = 22 });
        _db.SaveChanges();

        var records = _db.People.OrderBy(p => p.Name).Select(PersonForges.ToRecordExpression).ToList();

        Assert.Equal("Carol", records[0].Name);
        Assert.Equal(40, records[0].Age);
        Assert.Equal("Dave", records[1].Name);
        Assert.Equal(22, records[1].Age);
    }

    // ─── Phase 5: nested forge inlined into expression ────────────────────────

    [Fact]
    public void Phase5_NestedForge_InlinedAcrossNavigation_Translates()
    {
        var home = new Address { City = "Sydney", Zip = "2000" };
        var person = new Person { Name = "Eve", Age = 28, Home = home };
        _db.Addresses.Add(home);
        _db.People.Add(person);
        _db.SaveChanges();

        // EF should translate the inlined ternary into a LEFT JOIN with conditional SELECT
        var dtos = _db.People.Select(PersonForges.ToDtoWithHomeExpression).ToList();

        var eve = Assert.Single(dtos, d => d.Name == "Eve");
        Assert.NotNull(eve.Home);
        Assert.Equal("Sydney", eve.Home.City);
        Assert.Equal("2000", eve.Home.Zip);
    }

    // ─── Phase 6: collection mapping with element inlining ───────────────────

    [Fact]
    public void Phase6_CollectionOfNestedForge_InlinedSelect_Translates()
    {
        var order = new Order
        {
            Reference = "ORD-001",
            Lines = new List<OrderLine>
            {
                new() { Sku = "SKU-1", Quantity = 2 },
                new() { Sku = "SKU-2", Quantity = 5 },
            }
        };
        _db.Orders.Add(order);
        _db.SaveChanges();

        var dtos = _db.Orders.Select(OrderForges.ToDtoExpression).ToList();

        var dto = Assert.Single(dtos);
        Assert.Equal("ORD-001", dto.Reference);
        Assert.NotNull(dto.Lines);
        Assert.Equal(2, dto.Lines.Count);
        Assert.Contains(dto.Lines, l => l.Sku == "SKU-1" && l.Quantity == 2);
        Assert.Contains(dto.Lines, l => l.Sku == "SKU-2" && l.Quantity == 5);
    }

    // ─── Phase 7: flattening (member chain across navigation) ────────────────

    [Fact]
    public void Phase7_Flattening_ReferenceIntermediate_TernaryTranslates()
    {
        var home = new Address { City = "Berlin", Zip = "10115" };
        var person = new Person { Name = "Frank", Age = 33, Home = home };
        _db.Addresses.Add(home);
        _db.People.Add(person);
        _db.People.Add(new Person { Name = "Grace", Age = 45, Home = null });
        _db.SaveChanges();

        var flattened = _db.People.Select(PersonForges.ToFlatViewExpression).OrderBy(v => v.Name).ToList();

        var frank = flattened.First(v => v.Name == "Frank");
        Assert.Equal("Berlin", frank.HomeCity);
        var grace = flattened.First(v => v.Name == "Grace");
        Assert.Null(grace.HomeCity);
    }
}

// ─── DbContext + entities ─────────────────────────────────────────────────────

public sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<Person> People => Set<Person>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Widget> Widgets => Set<Widget>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
}

public class Person
{
    [Key] public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public Address Home { get; set; }
}

public class Address
{
    [Key] public int Id { get; set; }
    public string City { get; set; } = "";
    public string Zip { get; set; } = "";
}

public class Widget
{
    [Key] public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? Score { get; set; }
}

public class Ticket
{
    [Key] public int Id { get; set; }
    public string Title { get; set; } = "";
    public SrcSeverity Severity { get; set; }
}

public enum SrcSeverity { Low, Medium, High }
public enum DestSeverity { Low, Medium, High }

public class Order
{
    [Key] public int Id { get; set; }
    public string Reference { get; set; } = "";
    public List<OrderLine> Lines { get; set; } = new();
}

public class OrderLine
{
    [Key] public int Id { get; set; }
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public int OrderId { get; set; }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public class PersonDto
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

public class PersonWithHomeDto
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public AddressDto Home { get; set; }
}

public class PersonFlatView
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string HomeCity { get; set; }
}

public class AddressDto
{
    public string City { get; set; } = "";
    public string Zip { get; set; } = "";
}

public record PersonRecord(string Name, int Age);

public class WidgetDto
{
    public string Name { get; set; } = "";
    public int Score { get; set; }
}

public class TicketDto
{
    public string Title { get; set; } = "";
    public DestSeverity Severity { get; set; }
}

public class OrderDto
{
    public string Reference { get; set; } = "";
    public List<OrderLineDto> Lines { get; set; }
}

public class OrderLineDto
{
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
}

// ─── Forge classes ────────────────────────────────────────────────────────────

[Forge]
public static partial class PersonForges
{
    public static partial AddressDto ToAddressDto(Address source);

    [ForgeMethod(GenerateExpression = true)]
    public static partial PersonDto ToDto(Person source);

    [ForgeMethod(GenerateExpression = true)]
    public static partial PersonRecord ToRecord(Person source);

    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
    public static partial PersonWithHomeDto ToDtoWithHome(Person source);

    [ForgeMethod(GenerateExpression = true, AllowFlattening = true)]
    public static partial PersonFlatView ToFlatView(Person source);
}

[Forge]
public static partial class WidgetForges
{
    [ForgeMethod(GenerateExpression = true)]
    public static partial WidgetDto ToDto(Widget source);
}

[Forge]
public static partial class TicketForges
{
    [ForgeMethod(GenerateExpression = true)]
    public static partial TicketDto ToDtoCast(Ticket source);

    [ForgeMethod(GenerateExpression = true, MappingStrategy = ForgeMapping.ByName)]
    public static partial TicketDto ToDtoByName(Ticket source);
}

[Forge]
public static partial class OrderForges
{
    public static partial OrderLineDto ToOrderLineDto(OrderLine source);

    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
    public static partial OrderDto ToDto(Order source);
}
