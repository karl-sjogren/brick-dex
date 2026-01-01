namespace BrickDex.Core.Models.Rebrickable;

public class RebrickableTheme {
    public int Id { get; set; }
    public required string Name { get; set; }
    public int? ParentId { get; set; }

    public RebrickableTheme? Parent { get; set; }
    public ICollection<RebrickableTheme> Children { get; set; } = [];
    public ICollection<RebrickableSet> Sets { get; set; } = [];
}
