namespace BrickDex.Core.Models.Rebrickable;

public class RebrickableInventoryMinifig {
    public int InventoryId { get; set; }
    public required string FigNum { get; set; }
    public int Quantity { get; set; }

    public RebrickableInventory? Inventory { get; set; }
    public RebrickableMinifig? Minifig { get; set; }
}
