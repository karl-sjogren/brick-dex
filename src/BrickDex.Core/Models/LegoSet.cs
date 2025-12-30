namespace BrickDex.Core.Models;

public class LegoSet : EntityBase {
    public required string SetNumber { get; set; }
    public required string Name { get; set; }
    public int Year { get; set; }
    public int NumParts { get; set; }
    public string? ThemeName { get; set; }
    public string? ImageUrl { get; set; }
    public string? SetUrl { get; set; }

    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
    public bool IsWishlist { get; set; }
    public SetStatus Status { get; set; } = SetStatus.None;
}
