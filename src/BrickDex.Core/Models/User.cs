namespace BrickDex.Core.Models;

public class User : EntityBase {
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }

    public ICollection<UserLogin> Logins { get; set; } = [];
    public ICollection<UserSet> UserSets { get; set; } = [];
}
