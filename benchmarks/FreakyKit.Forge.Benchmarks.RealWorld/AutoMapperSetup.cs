using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeBenchmarks.RealWorld;

public static class AutoMapperSetup
{
    private static readonly Lazy<IMapper> _mapper = new(() =>
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<B2BOrderFulfilment.B2BAutoMapperProfile>();
            cfg.AddProfile<CrmContactImport.CrmAutoMapperProfile>();
            cfg.AddProfile<HealthcarePatient.HealthcareAutoMapperProfile>();
            cfg.AddProfile<BankingLedger.BankingAutoMapperProfile>();
            cfg.AddProfile<CmsContentTree.CmsAutoMapperProfile>();
            cfg.AddProfile<IdentityProvisioning.IdentityAutoMapperProfile>();
            cfg.AddProfile<InventoryWarehouse.InventoryAutoMapperProfile>();
            cfg.AddProfile<PublicApiResponse.PublicApiAutoMapperProfile>();
            // All 8 scenarios registered.
        });
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    });

    public static IMapper Mapper => _mapper.Value;
}
