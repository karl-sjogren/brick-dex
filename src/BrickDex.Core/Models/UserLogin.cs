namespace BrickDex.Core.Models;

public class UserLogin : EntityBase {
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string Provider { get; set; }
    public required string ProviderKey { get; set; }
}
