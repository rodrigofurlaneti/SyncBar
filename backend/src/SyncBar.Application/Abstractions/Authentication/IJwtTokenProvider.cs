using SyncBar.Domain.Entities;

namespace SyncBar.Application.Abstractions.Authentication;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface IJwtTokenProvider
{
    AccessToken GenerateCustomerToken(CustomerAppUser customer, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions);
    AccessToken GenerateToken(AppUser user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions);
    string GenerateRefreshToken();
}
