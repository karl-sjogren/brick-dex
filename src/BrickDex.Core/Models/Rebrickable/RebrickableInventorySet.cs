namespace BrickDex.Core.Models.Rebrickable;

public class RebrickableInventorySet {
    public int InventoryId { get; set; }
    public required string SetNum { get; set; }
    public int Quantity { get; set; }

    public RebrickableInventory? Inventory { get; set; }
    public RebrickableSet? Set { get; set; }
}
