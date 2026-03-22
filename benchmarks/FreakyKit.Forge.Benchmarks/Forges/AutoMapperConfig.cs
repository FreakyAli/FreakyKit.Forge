using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeBenchmarks;

public class BenchmarkProfile : Profile
{
    public BenchmarkProfile()
    {
        CreateMap<SimpleSource, SimpleDestination>();

        CreateMap<MediumSource, MediumDestination>();

        CreateMap<Address, AddressDto>();
        CreateMap<NestedSource, NestedDestination>();

        CreateMap<OrderItem, OrderItemDto>();
        CreateMap<CollectionSource, CollectionDestination>();

        CreateMap<DeepGraphSource, DeepGraphDestination>();

        CreateMap<FlatteningSource, FlatteningDestination>()
            .ForMember(d => d.HomeAddressStreet, o => o.MapFrom(s => s.HomeAddress.Street))
            .ForMember(d => d.HomeAddressCity, o => o.MapFrom(s => s.HomeAddress.City))
            .ForMember(d => d.HomeAddressState, o => o.MapFrom(s => s.HomeAddress.State))
            .ForMember(d => d.HomeAddressZipCode, o => o.MapFrom(s => s.HomeAddress.ZipCode));

        // E-Commerce Order
        CreateMap<CustomerEntity, CustomerDto>();
        CreateMap<LineItemEntity, LineItemDto>();
        CreateMap<OrderEntity, OrderDto>();

        // Nullable User
        CreateMap<NullableUserEntity, NullableUserDto>();
    }
}

public static class AutoMapperSetup
{
    private static readonly Lazy<IMapper> _mapper = new(() =>
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<BenchmarkProfile>());
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    });

    public static IMapper Mapper => _mapper.Value;
}
