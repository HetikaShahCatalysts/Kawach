using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Api.Security;

public sealed class AdminCredentialValidator(
    IOptions<AdminAuthenticationOptions> options)
{
    private readonly AdminAuthenticationOptions _options = options.Value;

    public bool Validate(string username, string password)
    {
        if (!FixedTimeEquals(username, _options.Username) ||
            string.IsNullOrEmpty(password))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(_options.PasswordSalt);
            var expectedHash = Convert.FromBase64String(_options.PasswordHash);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                _options.PasswordIterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
