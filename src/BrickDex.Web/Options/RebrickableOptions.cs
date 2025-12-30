namespace BrickDex.Web.Options;

public class RebrickableOptions {
    public required string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://rebrickable.com/api/v3/";
}
