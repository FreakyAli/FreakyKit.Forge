using AutoMapper;

namespace ForgeBenchmarks.RealWorld.InventoryWarehouse;

// ─── Forge ───────────────────────────────────────────────────────────────────

[global::FreakyKit.Forge.Forge]
public static partial class InventoryForges
{
    public static partial BinLocationDto MapBin(BinLocationEntity source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial WarehouseStockDto MapStock(WarehouseStockEntity source);

    public static partial MovementDto MapMovement(MovementEntity source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial SkuDto MapSku(SkuEntity source);
}

// ─── Hand-written baseline ───────────────────────────────────────────────────

public static class InventoryHandWritten
{
    public static SkuDto MapSku(SkuEntity s)
    {
        var dto = new SkuDto
        {
            Id = s.Id,
            SkuCode = s.SkuCode,
            ProductName = s.ProductName,
            UnitOfMeasure = s.UnitOfMeasure,
            UnitWeight = s.UnitWeight,
            IsHazardous = s.IsHazardous,
            IsRefrigerated = s.IsRefrigerated,
            Warehouses = new List<WarehouseStockDto>(s.Warehouses.Count),
            Movements = new List<MovementDto>(s.Movements.Count),
        };
        foreach (var w in s.Warehouses)
        {
            var wsDto = new WarehouseStockDto
            {
                WarehouseCode = w.WarehouseCode,
                OnHand = w.OnHand,
                Reserved = w.Reserved,
                Inbound = w.Inbound,
                Bins = new List<BinLocationDto>(w.Bins.Count),
            };
            foreach (var b in w.Bins)
                wsDto.Bins.Add(new BinLocationDto { Aisle = b.Aisle, Bay = b.Bay, Level = b.Level, Quantity = b.Quantity, ExpiresOn = b.ExpiresOn, LotNumber = b.LotNumber });
            dto.Warehouses.Add(wsDto);
        }
        foreach (var m in s.Movements)
            dto.Movements.Add(new MovementDto { Id = m.Id, At = m.At, Type = m.Type, Quantity = m.Quantity, FromLocation = m.FromLocation, ToLocation = m.ToLocation, Reference = m.Reference, Operator = m.Operator });
        return dto;
    }
}

// ─── Mapperly ────────────────────────────────────────────────────────────────

[Riok.Mapperly.Abstractions.Mapper]
public static partial class InventoryMapperly
{
    public static partial BinLocationDto MapBin(BinLocationEntity source);
    public static partial WarehouseStockDto MapStock(WarehouseStockEntity source);
    public static partial MovementDto MapMovement(MovementEntity source);
    public static partial SkuDto MapSku(SkuEntity source);
}

// ─── AutoMapper profile ──────────────────────────────────────────────────────

public class InventoryAutoMapperProfile : Profile
{
    public InventoryAutoMapperProfile()
    {
        CreateMap<BinLocationEntity, BinLocationDto>();
        CreateMap<WarehouseStockEntity, WarehouseStockDto>();
        CreateMap<MovementEntity, MovementDto>();
        CreateMap<SkuEntity, SkuDto>();
    }
}

// ─── Mapster registration ────────────────────────────────────────────────────

public static class InventoryMapsterConfig
{
    public static void Register()
    {
        Mapster.TypeAdapterConfig<BinLocationEntity, BinLocationDto>.NewConfig();
        Mapster.TypeAdapterConfig<WarehouseStockEntity, WarehouseStockDto>.NewConfig();
        Mapster.TypeAdapterConfig<MovementEntity, MovementDto>.NewConfig();
        Mapster.TypeAdapterConfig<SkuEntity, SkuDto>.NewConfig();
    }
}
