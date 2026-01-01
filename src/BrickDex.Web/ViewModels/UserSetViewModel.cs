using BrickDex.Core.Models;

namespace BrickDex.Web.ViewModels;

public class UserSetViewModel {
    public Guid Id { get; set; }
    public string SetNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int NumParts { get; set; }
    public string? ThemeName { get; set; }
    public string? ImageUrl { get; set; }
    public string? SetUrl { get; set; }
    public int Quantity { get; set; }
    public SetStatus Status { get; set; }
    public string? Notes { get; set; }

    public static UserSetViewModel FromUserSet(UserSet userSet) {
        return new UserSetViewModel {
            Id = userSet.Id,
            SetNumber = userSet.SetNumber,
            Name = userSet.Set.Name,
            Year = userSet.Set.Year,
            NumParts = userSet.Set.NumParts,
            ThemeName = userSet.Set.Theme?.Name,
            ImageUrl = userSet.Set.ImageUrl,
            SetUrl = $"https://rebrickable.com/sets/{userSet.SetNumber}/",
            Quantity = userSet.Quantity,
            Status = userSet.Status,
            Notes = userSet.Notes
        };
    }
}
