using Microsoft.AspNetCore.Http;

namespace BrickDex.Core.Contracts;

public interface IViewPreferenceService {
    string GetViewPreference(HttpRequest request, string? requestedView, string defaultView);
    void SetViewPreference(HttpResponse response, string view);
}
