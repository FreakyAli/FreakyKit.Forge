namespace FreakyKit.Forge.Samples;

/// <summary>
/// Collection mapping: destination arrays use .ToArray(), HashSet&lt;T&gt; uses .ToHashSet(), other collections use .ToList().
/// For different element types, combine with AllowNestedForging.
/// </summary>
[Forge]
public static partial class CollectionForges
{
    // List<string> → string[] (same element type, different collection type)
    public static partial PersonWithOrdersDto ToWithTags(Person source);

    // List<Order> → List<OrderDto> (different element type via nested forge)
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonWithOrderListDto ToWithOrders(Person source);

    public static partial OrderDto ToOrderDto(Order source);
}
