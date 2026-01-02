namespace BrickDex.Core.Contracts;

/// <summary>
/// Represents a theme for display in hierarchical dropdowns.
/// </summary>
/// <param name="Id">The theme ID.</param>
/// <param name="Name">The theme name.</param>
/// <param name="Depth">Nesting level (0 = root, 1 = child, 2 = grandchild, etc.)</param>
public record ThemeDisplayItem(int Id, string Name, int Depth);
