namespace Api.Security;

public sealed class AdminAuthenticationOptions
{
    public const string SectionName = "AdminAuthentication";

    public string Username { get; init; } = string.Empty;
    public string PasswordSalt { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public int PasswordIterations { get; init; } = 210_000;
}
