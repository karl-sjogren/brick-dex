namespace BrickDex.Core.Models.Rebrickable;

public class RebrickableInventory {
    public int Id { get; set; }
    public int Version { get; set; }
    public required string SetNum { get; set; }

    public RebrickableSet? Set { get; set; }
    public ICollection<RebrickableInventoryMinifig> InventoryMinifigs { get; set; } = [];
    public ICollection<RebrickableInventorySet> InventorySets { get; set; } = [];
}
