using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain;

public sealed class AppUserTests
{
    [Fact]
    public void RegisterLoginFailure_FiveTimes_ShouldLockOut()
    {
        var user = AppUser.Create(1, null, "waiter", "w@bar.com", "hash").Value;

        for (var i = 0; i < 5; i++)
            user.RegisterLoginFailure();

        user.IsLockedOut().Should().BeTrue();
        user.FailedAccessCount.Should().Be(5);
    }

    [Fact]
    public void RegisterLoginSuccess_ShouldResetLockout()
    {
        var user = AppUser.Create(1, null, "waiter", "w@bar.com", "hash").Value;
        user.RegisterLoginFailure();

        user.RegisterLoginSuccess();

        user.FailedAccessCount.Should().Be(0);
        user.IsLockedOut().Should().BeFalse();
        user.LastLoginAt.Should().NotBeNull();
    }
}
