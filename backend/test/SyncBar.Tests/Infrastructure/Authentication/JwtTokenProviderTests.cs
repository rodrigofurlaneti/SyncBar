using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Authentication;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Authentication;

public sealed class JwtTokenProviderTests
{
    private readonly JwtTokenProvider _provider;

    public JwtTokenProviderTests()
    {
        var options = Options.Create(new JwtOptions
        {
            Secret = "super-secret-test-key-with-at-least-32-chars!",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiresInMinutes = 60
        });

        _provider = new JwtTokenProvider(options);
    }

    private static AppUser CreateAppUser(long? employeeId)
    {
        var result = AppUser.Create(1, employeeId, "user1", "user1@mail.com", "hashed-password");
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static CustomerAppUser CreateCustomerAppUser(long? branchId, long? customerId)
    {
        var result = CustomerAppUser.Create(1, branchId, customerId, "customer1", "customer1@mail.com", "hashed-password");
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public void GenerateToken_WithoutEmployeeId_ShouldNotIncludeEmployeeIdClaim()
    {
        var user = CreateAppUser(null);
        var roles = new[] { "Admin" };
        var permissions = new[] { "orders.read" };

        var accessToken = _provider.GenerateToken(user, roles, permissions);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);
        jwt.Claims.Should().NotContain(c => c.Type == "employeeId");
    }

    [Fact]
    public void GenerateToken_WithEmployeeId_ShouldIncludeEmployeeIdClaim()
    {
        var user = CreateAppUser(42);
        var roles = new[] { "Admin" };
        var permissions = new[] { "orders.read" };

        var accessToken = _provider.GenerateToken(user, roles, permissions);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);
        jwt.Claims.Should().ContainSingle(c => c.Type == "employeeId" && c.Value == "42");
    }

    [Fact]
    public void GenerateToken_ShouldIncludeCoreClaimsRolesPermissionsAndTokenMetadata()
    {
        var user = CreateAppUser(null);
        var roles = new[] { "Admin", "Manager" };
        var permissions = new[] { "orders.read", "orders.write" };

        var accessToken = _provider.GenerateToken(user, roles, permissions);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == user.UserName);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == "companyId" && c.Value == user.CompanyId.ToString());
        jwt.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Jti);
        jwt.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value).Should().BeEquivalentTo(roles);
        jwt.Claims.Where(c => c.Type == "permission")
            .Select(c => c.Value).Should().BeEquivalentTo(permissions);

        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().ContainSingle().Which.Should().Be("test-audience");
        jwt.ValidTo.Should().BeCloseTo(accessToken.ExpiresAt.ToUniversalTime(), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateCustomerToken_WithoutCustomerIdAndBranchId_ShouldNotIncludeThoseClaims()
    {
        var customer = CreateCustomerAppUser(null, null);
        var roles = new[] { "Customer" };
        var permissions = Array.Empty<string>();

        var accessToken = _provider.GenerateCustomerToken(customer, roles, permissions);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);
        jwt.Claims.Should().NotContain(c => c.Type == "customerId");
        jwt.Claims.Should().NotContain(c => c.Type == "branchId");
    }

    [Fact]
    public void GenerateCustomerToken_WithCustomerIdOnly_ShouldIncludeCustomerIdButNotBranchId()
    {
        var customer = CreateCustomerAppUser(null, 7);
        var roles = new[] { "Customer" };
        var permissions = Array.Empty<string>();

        var accessToken = _provider.GenerateCustomerToken(customer, roles, permissions);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);
        jwt.Claims.Should().ContainSingle(c => c.Type == "customerId" && c.Value == "7");
        jwt.Claims.Should().NotContain(c => c.Type == "branchId");
    }

    [Fact]
    public void GenerateCustomerToken_WithBranchIdOnly_ShouldIncludeBranchIdButNotCustomerId()
    {
        var customer = CreateCustomerAppUser(3, null);
        var roles = new[] { "Customer" };
        var permissions = Array.Empty<string>();

        var accessToken = _provider.GenerateCustomerToken(customer, roles, permissions);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);
        jwt.Claims.Should().ContainSingle(c => c.Type == "branchId" && c.Value == "3");
        jwt.Claims.Should().NotContain(c => c.Type == "customerId");
    }

    [Fact]
    public void GenerateCustomerToken_WithCustomerIdAndBranchId_ShouldIncludeBothClaims()
    {
        var customer = CreateCustomerAppUser(3, 7);
        var roles = new[] { "Customer" };
        var permissions = new[] { "profile.read" };

        var accessToken = _provider.GenerateCustomerToken(customer, roles, permissions);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);
        jwt.Claims.Should().ContainSingle(c => c.Type == "customerId" && c.Value == "7");
        jwt.Claims.Should().ContainSingle(c => c.Type == "branchId" && c.Value == "3");
    }

    [Fact]
    public void GenerateCustomerToken_ShouldIncludeCoreClaimsRolesPermissionsAndTokenMetadata()
    {
        var customer = CreateCustomerAppUser(3, 7);
        var roles = new[] { "Customer" };
        var permissions = new[] { "profile.read", "orders.create" };

        var accessToken = _provider.GenerateCustomerToken(customer, roles, permissions);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == customer.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == customer.UserName);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == customer.Email);
        jwt.Claims.Should().Contain(c => c.Type == "companyId" && c.Value == customer.CompanyId.ToString());
        jwt.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Jti);
        jwt.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value).Should().BeEquivalentTo(roles);
        jwt.Claims.Where(c => c.Type == "permission")
            .Select(c => c.Value).Should().BeEquivalentTo(permissions);

        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().ContainSingle().Which.Should().Be("test-audience");
        jwt.ValidTo.Should().BeCloseTo(accessToken.ExpiresAt.ToUniversalTime(), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64StringOf64Bytes()
    {
        var refreshToken = _provider.GenerateRefreshToken();

        refreshToken.Should().NotBeNullOrWhiteSpace();
        var bytes = Convert.FromBase64String(refreshToken);
        bytes.Should().HaveCount(64);
    }

    [Fact]
    public void GenerateRefreshToken_CalledTwice_ShouldReturnDifferentValues()
    {
        var first = _provider.GenerateRefreshToken();
        var second = _provider.GenerateRefreshToken();

        first.Should().NotBe(second);
    }
}
