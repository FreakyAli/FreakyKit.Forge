using Riok.Mapperly.Abstractions;

namespace ForgeBenchmarks;

[Mapper]
public static partial class MapperlyMappers
{
    public static partial SimpleDestination MapSimple(SimpleSource source);

    public static partial MediumDestination MapMedium(MediumSource source);

    public static partial AddressDto MapAddress(Address source);

    public static partial NestedDestination MapNested(NestedSource source);

    public static partial OrderItemDto MapOrderItem(OrderItem source);

    public static partial CollectionDestination MapCollection(CollectionSource source);

    public static partial DeepGraphDestination MapDeepGraph(DeepGraphSource source);

    [MapProperty(nameof(FlatteningSource.HomeAddress) + "." + nameof(Address.Street), nameof(FlatteningDestination.HomeAddressStreet))]
    [MapProperty(nameof(FlatteningSource.HomeAddress) + "." + nameof(Address.City), nameof(FlatteningDestination.HomeAddressCity))]
    [MapProperty(nameof(FlatteningSource.HomeAddress) + "." + nameof(Address.State), nameof(FlatteningDestination.HomeAddressState))]
    [MapProperty(nameof(FlatteningSource.HomeAddress) + "." + nameof(Address.ZipCode), nameof(FlatteningDestination.HomeAddressZipCode))]
    public static partial FlatteningDestination MapFlattening(FlatteningSource source);

    public static partial CustomerDto MapCustomer(CustomerEntity source);
    public static partial LineItemDto MapLineItem(LineItemEntity source);
    public static partial OrderDto MapOrder(OrderEntity source);

    public static partial NullableUserDto MapNullableUser(NullableUserEntity source);
}
