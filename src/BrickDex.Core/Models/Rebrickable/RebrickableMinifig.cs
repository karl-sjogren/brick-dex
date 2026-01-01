namespace BrickDex.Core.Models.Rebrickable;

public class RebrickableMinifig {
    public required string FigNum { get; set; }
    public required string Name { get; set; }
    public int NumParts { get; set; }
    public string? ImageUrl { get; set; }

    public ICollection<RebrickableInventoryMinifig> InventoryMinifigs { get; set; } = [];
}
