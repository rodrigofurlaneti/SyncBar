using SyncBar.Application.Abstractions.Authentication;

namespace SyncBar.Infrastructure.Authentication;

internal sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);

    // Senha NUNCA verificada em SQL — sempre BCrypt em C#.
    public bool Verify(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
