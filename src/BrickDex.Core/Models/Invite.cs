namespace BrickDex.Core.Models;

public class Invite : EntityBase {
    /// <summary>
    /// The unique invite code (URL-safe string, 16-character base64url).
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// When this invite expires (approximately 1 week from creation).
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// User who created this invite.
    /// </summary>
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// User who used this invite (null if not yet used).
    /// </summary>
    public Guid? UsedByUserId { get; set; }
    public User? UsedByUser { get; set; }

    /// <summary>
    /// When the invite was used (null if not yet used).
    /// </summary>
    public DateTimeOffset? UsedAt { get; set; }
}
