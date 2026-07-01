namespace ForgeBenchmarks.RealWorld;

public static class MapsterSetup
{
    private static readonly object _lock = new();
    private static bool _configured;

    public static void Configure()
    {
        if (_configured) return;
        lock (_lock)
        {
            if (_configured) return;
            B2BOrderFulfilment.B2BMapsterConfig.Register();
            CrmContactImport.CrmMapsterConfig.Register();
            HealthcarePatient.HealthcareMapsterConfig.Register();
            BankingLedger.BankingMapsterConfig.Register();
            CmsContentTree.CmsMapsterConfig.Register();
            IdentityProvisioning.IdentityMapsterConfig.Register();
            InventoryWarehouse.InventoryMapsterConfig.Register();
            PublicApiResponse.PublicApiMapsterConfig.Register();
            // All 8 scenarios registered.
            _configured = true;
        }
    }
}
