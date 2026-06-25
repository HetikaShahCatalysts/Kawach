namespace Api.Security;

public sealed class RequestSecurityOptions
{
    public const string SectionName = "RequestSecurity";

    public string[] AllowedOrigins { get; init; } = [];
    public string[] AllowedHosts { get; init; } = [];
}
