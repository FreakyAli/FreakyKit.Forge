
namespace ForgeBenchmarks;

// ──────────────────────────────────────────────────────────────
//  Hand-written mappers — the baseline Forge should match
// ──────────────────────────────────────────────────────────────

public static class HandWrittenMappers
{
    public static SimpleDestination MapSimple(SimpleSource source)
    {
        var result = new SimpleDestination();
        result.Id = source.Id;
        result.Name = source.Name;
        result.Age = source.Age;
        result.Email = source.Email;
        return result;
    }

    public static MediumDestination MapMedium(MediumSource source)
    {
        var result = new MediumDestination();
        result.Id = source.Id;
        result.FirstName = source.FirstName;
        result.LastName = source.LastName;
        result.Age = source.Age;
        result.Email = source.Email;
        result.Phone = source.Phone;
        result.CreatedAt = source.CreatedAt;
        result.IsActive = source.IsActive;
        result.Balance = source.Balance;
        result.Notes = source.Notes;
        return result;
    }

    public static AddressDto MapAddress(Address source)
    {
        var result = new AddressDto();
        result.Street = source.Street;
        result.City = source.City;
        result.State = source.State;
        result.ZipCode = source.ZipCode;
        return result;
    }

    public static NestedDestination MapNested(NestedSource source)
    {
        var result = new NestedDestination();
        result.Id = source.Id;
        result.Name = source.Name;
        result.Address = MapAddress(source.Address);
        return result;
    }

    public static OrderItemDto MapOrderItem(OrderItem source)
    {
        var result = new OrderItemDto();
        result.Sku = source.Sku;
        result.Quantity = source.Quantity;
        result.UnitPrice = source.UnitPrice;
        return result;
    }

    public static CollectionDestination MapCollection(CollectionSource source)
    {
        var result = new CollectionDestination();
        result.Id = source.Id;
        result.Name = source.Name;
        result.Tags = source.Tags.ToArray();
        result.Items = source.Items.Select(x => MapOrderItem(x)).ToList();
        return result;
    }

    public static DeepGraphDestination MapDeepGraph(DeepGraphSource source)
    {
        var result = new DeepGraphDestination();
        result.Id = source.Id;
        result.Name = source.Name;
        result.Email = source.Email;
        result.Age = source.Age;
        result.IsActive = source.IsActive;
        result.CreatedAt = source.CreatedAt;
        result.HomeAddress = MapAddress(source.HomeAddress);
        result.WorkAddress = MapAddress(source.WorkAddress);
        result.RecentOrders = source.RecentOrders.Select(x => MapOrderItem(x)).ToList();
        result.Tags = source.Tags.ToArray();
        return result;
    }

    public static FlatteningDestination MapFlattening(FlatteningSource source)
    {
        var result = new FlatteningDestination();
        result.Name = source.Name;
        result.HomeAddressStreet = source.HomeAddress.Street;
        result.HomeAddressCity = source.HomeAddress.City;
        result.HomeAddressState = source.HomeAddress.State;
        result.HomeAddressZipCode = source.HomeAddress.ZipCode;
        return result;
    }

    public static void UpdateSimple(SimpleSource source, SimpleDestination existing)
    {
        existing.Id = source.Id;
        existing.Name = source.Name;
        existing.Age = source.Age;
        existing.Email = source.Email;
    }
}
