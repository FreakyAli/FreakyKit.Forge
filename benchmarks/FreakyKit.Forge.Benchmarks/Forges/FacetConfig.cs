using Facet;

namespace ForgeBenchmarks;

// ──────────────────────────────────────────────────────────────
//  Facet-generated DTOs — Facet generates the properties,
//  constructor, and projection from the source type.
// ──────────────────────────────────────────────────────────────

// Simple (4 props)
[Facet(typeof(SimpleSource))]
public partial class SimpleFacetDto;

// Medium (10 props)
[Facet(typeof(MediumSource))]
public partial class MediumFacetDto;

// Address (for nested)
[Facet(typeof(Address))]
public partial class AddressFacetDto;

// Nested (Address child)
[Facet(typeof(NestedSource), NestedFacets = [typeof(AddressFacetDto)])]
public partial class NestedFacetDto;

// OrderItem (for collections)
[Facet(typeof(OrderItem))]
public partial class OrderItemFacetDto;

// Collection (List<string> + List<OrderItem>)
[Facet(typeof(CollectionSource), NestedFacets = [typeof(OrderItemFacetDto)])]
public partial class CollectionFacetDto;

// Deep Graph (addresses + orders + tags)
[Facet(typeof(DeepGraphSource), NestedFacets = [typeof(AddressFacetDto), typeof(OrderItemFacetDto)])]
public partial class DeepGraphFacetDto;

// Flattening — Facet supports [Flatten] on the source type's nested properties
// but since we can't modify the source models, we'll use the standard Facet mapping
[Facet(typeof(FlatteningSource), NestedFacets = [typeof(AddressFacetDto)])]
public partial class FlatteningFacetDto;

// E-Commerce
[Facet(typeof(CustomerEntity), NestedFacets = [typeof(AddressFacetDto)])]
public partial class CustomerFacetDto;

[Facet(typeof(LineItemEntity))]
public partial class LineItemFacetDto;

[Facet(typeof(OrderEntity), NestedFacets = [typeof(CustomerFacetDto), typeof(AddressFacetDto), typeof(LineItemFacetDto)])]
public partial class OrderFacetDto;

// Nullable User
[Facet(typeof(NullableUserEntity))]
public partial class NullableUserFacetDto;
