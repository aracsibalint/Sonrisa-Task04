using System.Net;

namespace Alerts.Api;

public static class SandboxBindingPolicy
{
    public static bool IsAllowed(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttp
            && string.IsNullOrEmpty(uri.UserInfo)
            && uri.AbsolutePath == "/"
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && IPAddress.TryParse(uri.IdnHost, out var address)
            && IPAddress.IsLoopback(address);
    }
}
