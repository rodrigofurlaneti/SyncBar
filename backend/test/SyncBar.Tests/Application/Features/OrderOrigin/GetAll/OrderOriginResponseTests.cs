using FluentAssertions;
using SyncBar.Application.Features.OrderOrigin.GetAll;
using Xunit;

namespace SyncBar.Tests.Application.Features.OrderOrigin.GetAll;

public sealed class OrderOriginResponseTests
{
    [Fact]
    public void Construction_ShouldExposeAllProvidedValues()
    {
        var createdAt = new DateTime(2026, 1, 1);

        var response = new OrderOriginResponse(1, 2, 3, "LOCAL", true, createdAt);

        response.Id.Should().Be(1);
        response.CompanyId.Should().Be(2);
        response.BranchId.Should().Be(3);
        response.Name.Should().Be("LOCAL");
        response.IsActive.Should().BeTrue();
        response.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Equals_SameValues_ShouldBeEqual()
    {
        var createdAt = new DateTime(2026, 1, 1);
        var first = new OrderOriginResponse(1, null, null, "LOCAL", true, createdAt);
        var second = new OrderOriginResponse(1, null, null, "LOCAL", true, createdAt);

        first.Should().Be(second);
    }
}
