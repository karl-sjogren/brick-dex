using System.ComponentModel.DataAnnotations;

namespace BrickDex.Core.Models;

public enum SetStatus {
    None = 0,
    Ordered = 1,

    [Display(Name = "In Storage")]
    InStorage = 2,

    Building = 3,
    Built = 4,
    Sold = 5
}
