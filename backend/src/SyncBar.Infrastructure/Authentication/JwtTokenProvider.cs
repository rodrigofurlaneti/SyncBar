using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Authentication;

internal sealed class JwtTokenProvider(IOptions<JwtOptions> options) : IJwtTokenProvider
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken GenerateToken(AppUser user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("companyId", user.CompanyId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        // Sem essa claim, ICurrentUserService.EmployeeId nunca resolve, e qualquer
        // fluxo que exige um funcionário responsável (ex.: lançar movimento de
        // estoque) falha com "usuário logado não possui um funcionário vinculado",
        // mesmo quando AppUser.EmployeeId está corretamente preenchido no banco.
        if (user.EmployeeId is { } employeeId)
            claims.Add(new Claim("employeeId", employeeId.ToString()));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var expiresAt = DateTime.Now.AddMinutes(_options.ExpiresInMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.Now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
