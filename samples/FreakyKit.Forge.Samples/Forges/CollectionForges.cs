
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Collection mapping: List → array uses .ToArray(), others use .ToList().
/// For different element types, combine with AllowNestedForging.
/// </summary>
[Forge]
public static partial class CollectionForges
{
    // List&lt;string&gt; → string[] (same element type, different collection type)
    public static partial PersonWithOrdersDto ToWithTags(Person source);

    // List&lt;Order&gt; → List&lt;OrderDto&gt; (different element type via nested forge)
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonWithOrderListDto ToWithOrders(Person source);

    public static partial OrderDto ToOrderDto(Order source);
}
