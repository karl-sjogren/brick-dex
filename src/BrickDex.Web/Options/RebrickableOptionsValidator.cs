using Microsoft.Extensions.Options;

namespace BrickDex.Web.Options;

public class RebrickableOptionsValidator : IValidateOptions<RebrickableOptions> {
    public ValidateOptionsResult Validate(string? name, RebrickableOptions options) {
        if(string.IsNullOrWhiteSpace(options.ApiKey)) {
            return ValidateOptionsResult.Fail("Rebrickable:ApiKey is required. Set it using user secrets.");
        }

        if(string.IsNullOrWhiteSpace(options.BaseUrl)) {
            return ValidateOptionsResult.Fail("Rebrickable:BaseUrl is required.");
        }

        if(!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _)) {
            return ValidateOptionsResult.Fail("Rebrickable:BaseUrl must be a valid absolute URI.");
        }

        return ValidateOptionsResult.Success;
    }
}
