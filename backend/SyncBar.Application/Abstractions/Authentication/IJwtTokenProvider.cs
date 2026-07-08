using SyncBar.Domain.Entities;

namespace SyncBar.Application.Abstractions.Authentication;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface IJwtTokenProvider
{
    AccessToken GenerateToken(AppUser user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions);
    string GenerateRefreshToken();
}
