using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Features.Auth.CustomerLogin;
using SyncBar.Application.Features.Auth.CustomerRefresh;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories;

public sealed class CustomerAuthenticationTests : RepositoryTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Login_WithNoAdministrativeUser_PersistsCustomerAuditWithForeignKeysEnabled(bool correctPassword)
    {
        var customer = await SeedCustomerAsync();
        var jwt = Jwt();
        var passwords = Substitute.For<IPasswordHasher>();
        passwords.Verify("password", customer.PasswordHash).Returns(correctPassword);
        var handler = new CustomerLoginCommandHandler(new CustomerAppUserRepository(Context),
            new CustomerRefreshTokenRepository(Context), passwords, jwt,
            new AccessLogRepository(Context), new LogTrackerRepository(Context), Context);

        var result = await handler.Handle(new CustomerLoginCommand(customer.Email, "password", 1, null), default);

        result.IsSuccess.Should().Be(correctPassword);
        var log = await Context.Set<AccessLog>().SingleAsync();
        log.AppUserId.Should().BeNull();
        log.CustomerAppUserId.Should().Be(customer.Id);
        (await Context.Set<RefreshToken>().CountAsync()).Should().Be(0);
        (await Context.Set<CustomerRefreshToken>().CountAsync()).Should().Be(correctPassword ? 1 : 0);
        (await Context.Set<LogTracker>().SingleAsync()).AppUserId.Should().BeNull();
        (await Context.Set<AppUser>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ForeignKeys_RejectCustomerIdInAdministrativeTokenTable()
    {
        var customer = await SeedCustomerAsync();
        Context.Add(RefreshToken.Create(customer.Id, "old-bug", DateTime.Now.AddDays(1)).Value);
        await FluentActions.Awaiting(() => Context.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Refresh_RotatesCustomerToken_WithoutAdministrativeIdentity_AndRejectsReplay()
    {
        var customer = await SeedCustomerAsync();
        var old = CustomerRefreshToken.Create(customer.Id, "old-customer-token", DateTime.Now.AddDays(1)).Value;
        Context.Add(old);
        await Context.SaveChangesAsync();
        var jwt = Jwt();
        var handler = new CustomerRefreshTokenCommandHandler(new CustomerRefreshTokenRepository(Context),
            new CustomerAppUserRepository(Context), jwt, new LogTrackerRepository(Context), Context);

        var result = await handler.Handle(new CustomerRefreshTokenCommand(old.Token), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be(customer.CustomerId);
        old.RevokedAt.Should().NotBeNull();
        (await Context.Set<CustomerRefreshToken>().CountAsync()).Should().Be(2);
        jwt.Received(1).GenerateCustomerToken(Arg.Any<CustomerAppUser>(),
            Arg.Is<IReadOnlyCollection<string>>(r => r.Count == 1 && r.Contains("Customer")),
            Arg.Is<IReadOnlyCollection<string>>(p => p.Count == 0));
        jwt.DidNotReceiveWithAnyArgs().GenerateToken(default!, default!, default!);
        var adminRefresh = new SyncBar.Application.Features.Auth.Refresh.RefreshTokenCommandHandler(
            new RefreshTokenRepository(Context), new AppUserRepository(Context), jwt,
            new LogTrackerRepository(Context), Context);
        var crossSession = await adminRefresh.Handle(
            new SyncBar.Application.Features.Auth.Refresh.RefreshTokenCommand(result.Value.RefreshToken), default);
        crossSession.IsFailure.Should().BeTrue();
        var replay = await handler.Handle(new CustomerRefreshTokenCommand(old.Token), default);
        replay.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_RejectsAdministrativeTokenEvenWhenNumericIdsOverlap()
    {
        var customer = await SeedCustomerAsync();
        var admin = AppUser.Create(1, null, "admin", "admin@example.test", "hash").Value;
        // Seed unrelated company dependencies with the fixture's initial setting only.
        await Context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF");
        Context.Add(admin);
        await Context.SaveChangesAsync();
        await Context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON");
        admin.Id.Should().Be(customer.Id);
        Context.Add(RefreshToken.Create(admin.Id, "admin-token", DateTime.Now.AddDays(1)).Value);
        await Context.SaveChangesAsync();
        var handler = new CustomerRefreshTokenCommandHandler(new CustomerRefreshTokenRepository(Context),
            new CustomerAppUserRepository(Context), Jwt(), new LogTrackerRepository(Context), Context);
        var result = await handler.Handle(new CustomerRefreshTokenCommand("admin-token"), default);
        result.IsFailure.Should().BeTrue();
    }

    private async Task<CustomerAppUser> SeedCustomerAsync()
    {
        var customer = CustomerAppUser.Create(1, null, 901, "Cliente", "cliente@example.test", "hash").Value;
        Context.Add(customer);
        await Context.SaveChangesAsync();
        // All authentication operations run with real FK enforcement. The fixture skips only
        // unrelated company setup; there deliberately is no AppUser matching this customer.
        await Context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON");
        return customer;
    }

    private static IJwtTokenProvider Jwt()
    {
        var jwt = Substitute.For<IJwtTokenProvider>();
        jwt.GenerateRefreshToken().Returns("new-customer-token");
        jwt.GenerateCustomerToken(Arg.Any<CustomerAppUser>(), Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<IReadOnlyCollection<string>>()).Returns(new AccessToken("customer-access", DateTime.Now.AddMinutes(30)));
        return jwt;
    }
}
