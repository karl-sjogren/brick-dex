using BrickDex.Core.Contracts;
using Microsoft.AspNetCore.Http;

namespace BrickDex.Core.Services;

public class ViewPreferenceService : IViewPreferenceService {
    private readonly TimeProvider _timeProvider;
    private const string _cookieName = "BrickDex_ViewPreference";

    public ViewPreferenceService(TimeProvider timeProvider) {
        _timeProvider = timeProvider;
    }

    public string GetViewPreference(HttpRequest request, string? requestedView, string defaultView) {
        if(!string.IsNullOrEmpty(requestedView)) {
            return requestedView;
        }

        return request.Cookies[_cookieName] ?? defaultView;
    }

    public void SetViewPreference(HttpResponse response, string view) {
        response.Cookies.Append(_cookieName, view, new CookieOptions {
            Expires = _timeProvider.GetUtcNow().AddYears(1),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax
        });
    }
}
