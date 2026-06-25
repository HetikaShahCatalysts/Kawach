param(
    [Parameter(Mandatory = $true)]
    [string]$Password,

    [int]$Iterations = 210000
)

$salt = New-Object byte[] 16
[System.Security.Cryptography.RandomNumberGenerator]::Fill($salt)

$hash = [System.Security.Cryptography.Rfc2898DeriveBytes]::Pbkdf2(
    $Password,
    $salt,
    $Iterations,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    32)

[pscustomobject]@{
    PasswordSalt = [Convert]::ToBase64String($salt)
    PasswordHash = [Convert]::ToBase64String($hash)
    PasswordIterations = $Iterations
} | ConvertTo-Json
