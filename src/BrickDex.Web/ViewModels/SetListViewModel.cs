namespace BrickDex.Web.ViewModels;

public class SetListViewModel {
    public required IReadOnlyList<UserSetViewModel> Sets { get; init; }
    public required string PageName { get; init; }
    public required string View { get; init; }
    public required string? Query { get; init; }
    public required string SortBy { get; init; }
    public required bool SortDesc { get; init; }
    public required int CurrentPage { get; init; }
    public required int TotalCount { get; init; }
    public required int TotalPages { get; init; }
    public bool ShowActions { get; init; }

    public string GetSortIndicator(string column) {
        if(!string.Equals(SortBy, column, StringComparison.OrdinalIgnoreCase)) {
            return string.Empty;
        }

        return SortDesc ? " \u25bc" : " \u25b2";
    }

    public bool GetNextSortDesc(string column) {
        if(!string.Equals(SortBy, column, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        return !SortDesc;
    }
}
