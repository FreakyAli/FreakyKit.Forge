
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

    public static CustomerDto MapCustomer(CustomerEntity source)
    {
        var result = new CustomerDto();
        result.Id = source.Id;
        result.FirstName = source.FirstName;
        result.LastName = source.LastName;
        result.Email = source.Email;
        result.Phone = source.Phone;
        result.BillingAddress = MapAddress(source.BillingAddress);
        return result;
    }

    public static LineItemDto MapLineItem(LineItemEntity source)
    {
        var result = new LineItemDto();
        result.ProductId = source.ProductId;
        result.ProductName = source.ProductName;
        result.Sku = source.Sku;
        result.Quantity = source.Quantity;
        result.UnitPrice = source.UnitPrice;
        result.Discount = source.Discount;
        return result;
    }

    public static OrderDto MapOrder(OrderEntity source)
    {
        var result = new OrderDto();
        result.Id = source.Id;
        result.OrderNumber = source.OrderNumber;
        result.Status = source.Status;
        result.Payment = source.Payment;
        result.CreatedAt = source.CreatedAt;
        result.ShippedAt = source.ShippedAt;
        result.DeliveredAt = source.DeliveredAt;
        result.Subtotal = source.Subtotal;
        result.Tax = source.Tax;
        result.Total = source.Total;
        result.Currency = source.Currency;
        result.Notes = source.Notes;
        result.IsGift = source.IsGift;
        result.Customer = MapCustomer(source.Customer);
        result.ShippingAddress = MapAddress(source.ShippingAddress);
        result.LineItems = source.LineItems.Select(x => MapLineItem(x)).ToList();
        result.Tags = source.Tags.ToArray();
        return result;
    }

    public static NullableUserDto MapNullableUser(NullableUserEntity source)
    {
        var result = new NullableUserDto();
        result.Id = source.Id;
        result.Username = source.Username;
        result.DisplayName = source.DisplayName;
        result.Email = source.Email;
        result.AvatarUrl = source.AvatarUrl;
        result.Bio = source.Bio;
        result.Age = source.Age;
        result.DateOfBirth = source.DateOfBirth;
        result.LastLoginAt = source.LastLoginAt;
        result.CreatedAt = source.CreatedAt;
        result.IsVerified = source.IsVerified;
        result.IsAdmin = source.IsAdmin;
        result.AccountBalance = source.AccountBalance;
        result.LoginCount = source.LoginCount;
        result.Timezone = source.Timezone;
        result.Locale = source.Locale;
        return result;
    }
}
