namespace ForgeBenchmarks.RealWorld.InventoryWarehouse;

// ─── Source entities ─────────────────────────────────────────────────────────

public class SkuEntity
{
    public int Id { get; set; }
    public string SkuCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string UnitOfMeasure { get; set; } = "";
    public decimal UnitWeight { get; set; }
    public bool IsHazardous { get; set; }
    public bool IsRefrigerated { get; set; }
    public List<WarehouseStockEntity> Warehouses { get; set; } = new();
    public List<MovementEntity> Movements { get; set; } = new();
}

public class WarehouseStockEntity
{
    public string WarehouseCode { get; set; } = "";
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int Inbound { get; set; }
    public List<BinLocationEntity> Bins { get; set; } = new();
}

public class BinLocationEntity
{
    public string Aisle { get; set; } = "";
    public string Bay { get; set; } = "";
    public string Level { get; set; } = "";
    public int Quantity { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string? LotNumber { get; set; }
}

public class MovementEntity
{
    public Guid Id { get; set; }
    public DateTime At { get; set; }
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public string FromLocation { get; set; } = "";
    public string ToLocation { get; set; } = "";
    public string Reference { get; set; } = "";
    public string Operator { get; set; } = "";
}

public enum MovementType { Receipt, Putaway, Pick, Move, Cycle, Adjustment, Shipment }

// ─── DTOs ────────────────────────────────────────────────────────────────────

public class SkuDto
{
    public int Id { get; set; }
    public string SkuCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string UnitOfMeasure { get; set; } = "";
    public decimal UnitWeight { get; set; }
    public bool IsHazardous { get; set; }
    public bool IsRefrigerated { get; set; }
    public List<WarehouseStockDto> Warehouses { get; set; } = new();
    public List<MovementDto> Movements { get; set; } = new();
}

public class WarehouseStockDto
{
    public string WarehouseCode { get; set; } = "";
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int Inbound { get; set; }
    public List<BinLocationDto> Bins { get; set; } = new();
}

public class BinLocationDto
{
    public string Aisle { get; set; } = "";
    public string Bay { get; set; } = "";
    public string Level { get; set; } = "";
    public int Quantity { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string? LotNumber { get; set; }
}

public class MovementDto
{
    public Guid Id { get; set; }
    public DateTime At { get; set; }
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public string FromLocation { get; set; } = "";
    public string ToLocation { get; set; } = "";
    public string Reference { get; set; } = "";
    public string Operator { get; set; } = "";
}

[Facet.Facet(typeof(BinLocationEntity))]
public partial class BinLocationFacetDto;

[Facet.Facet(typeof(WarehouseStockEntity), NestedFacets = [typeof(BinLocationFacetDto)])]
public partial class WarehouseStockFacetDto;

[Facet.Facet(typeof(MovementEntity))]
public partial class MovementFacetDto;

[Facet.Facet(typeof(SkuEntity), NestedFacets = [typeof(WarehouseStockFacetDto), typeof(MovementFacetDto)])]
public partial class SkuFacetDto;
