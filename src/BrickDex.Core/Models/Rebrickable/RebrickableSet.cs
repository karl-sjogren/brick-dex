namespace BrickDex.Core.Models.Rebrickable;

public class RebrickableSet {
    public required string SetNum { get; set; }
    public required string Name { get; set; }
    public int Year { get; set; }
    public int ThemeId { get; set; }
    public int NumParts { get; set; }
    public string? ImageUrl { get; set; }

    public RebrickableTheme? Theme { get; set; }
    public ICollection<RebrickableInventory> Inventories { get; set; } = [];
    public ICollection<RebrickableInventorySet> InventorySets { get; set; } = [];
    public ICollection<UserSet> UserSets { get; set; } = [];
}
