using BrickDex.Core.Models;

namespace BrickDex.Web.ViewModels;

public class UserSetViewModel {
    public Guid Id { get; set; }
    public Guid LegoSetId { get; set; }
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
            LegoSetId = userSet.LegoSetId,
            SetNumber = userSet.LegoSet.SetNumber,
            Name = userSet.LegoSet.Name,
            Year = userSet.LegoSet.Year,
            NumParts = userSet.LegoSet.NumParts,
            ThemeName = userSet.LegoSet.ThemeName,
            ImageUrl = userSet.LegoSet.ImageUrl,
            SetUrl = userSet.LegoSet.SetUrl,
            Quantity = userSet.Quantity,
            Status = userSet.Status,
            Notes = userSet.Notes
        };
    }
}
