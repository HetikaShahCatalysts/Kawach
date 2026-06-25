using Microsoft.Extensions.Options;

namespace Api.Security;

public sealed class RequestSecurityPolicy(IOptions<RequestSecurityOptions> options)
{
    private readonly HashSet<string> _allowedOrigins = options.Value.AllowedOrigins
        .Where(static origin => !string.IsNullOrWhiteSpace(origin))
        .Select(static origin => origin.Trim().TrimEnd('/'))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _allowedHosts = options.Value.AllowedHosts
        .Where(static host => !string.IsNullOrWhiteSpace(host))
        .Select(static host => host.Trim())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public bool HasOriginRestrictions => _allowedOrigins.Count > 0;
    public bool HasHostRestrictions => _allowedHosts.Count > 0;

    public bool IsAllowedOrigin(string origin) =>
        _allowedOrigins.Contains(origin.Trim().TrimEnd('/'));

    public bool IsAllowedHost(HostString host) =>
        !string.IsNullOrWhiteSpace(host.Host) &&
        _allowedHosts.Contains(host.Host);
}
