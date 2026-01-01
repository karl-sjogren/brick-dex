namespace BrickDex.Core.Models;

public class UserSet : EntityBase {
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid LegoSetId { get; set; }
    public LegoSet LegoSet { get; set; } = null!;

    public int Quantity { get; set; } = 1;
    public SetStatus Status { get; set; } = SetStatus.None;
    public bool IsWishlist { get; set; }
    public string? Notes { get; set; }
}
