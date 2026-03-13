namespace ForgeBenchmarks;

[global::FreakyKit.Forge.Forge]
public static partial class SimpleForges
{
    public static partial SimpleDestination Map(SimpleSource source);
}

[global::FreakyKit.Forge.Forge]
public static partial class MediumForges
{
    public static partial MediumDestination Map(MediumSource source);
}

[global::FreakyKit.Forge.Forge]
public static partial class NestedForges
{
    public static partial AddressDto MapAddress(Address source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial NestedDestination Map(NestedSource source);
}

[global::FreakyKit.Forge.Forge]
public static partial class CollectionForges
{
    public static partial OrderItemDto MapItem(OrderItem source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial CollectionDestination Map(CollectionSource source);
}

[global::FreakyKit.Forge.Forge]
public static partial class DeepGraphForges
{
    public static partial AddressDto MapAddress(Address source);
    public static partial OrderItemDto MapItem(OrderItem source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial DeepGraphDestination Map(DeepGraphSource source);
}

[global::FreakyKit.Forge.Forge]
public static partial class FlatteningForges
{
    [global::FreakyKit.Forge.ForgeMethod(AllowFlattening = true)]
    public static partial FlatteningDestination Map(FlatteningSource source);
}

[global::FreakyKit.Forge.Forge]
public static partial class UpdateForges
{
    public static partial void Update(SimpleSource source, SimpleDestination existing);
}

[global::FreakyKit.Forge.Forge]
public static partial class EcommerceForges
{
    public static partial AddressDto MapAddress(Address source);
    public static partial LineItemDto MapLineItem(LineItemEntity source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial CustomerDto MapCustomer(CustomerEntity source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial OrderDto MapOrder(OrderEntity source);
}

[global::FreakyKit.Forge.Forge]
public static partial class NullableForges
{
    public static partial NullableUserDto Map(NullableUserEntity source);
}
