using System.Text.Json;
using FluentAssertions;
using SyncBar.API.Controllers;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class RequiredRequestFieldsTests
{
    [Fact]
    public void OrderOrigin_OmittedStatusShouldFailInsteadOfDeactivating()
    {
        var deserialize = () => JsonSerializer.Deserialize<UpdateOrderOriginRequest>("{\"Name\":\"Website\"}");
        deserialize.Should().Throw<JsonException>();
    }

    [Fact]
    public void OrderOrigin_ExplicitFalseShouldBeAccepted()
    {
        var request = JsonSerializer.Deserialize<UpdateOrderOriginRequest>("{\"Name\":\"Website\",\"IsActive\":false}");
        request.Should().NotBeNull();
        request!.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CustomerAddress_OmittedIdShouldFail()
    {
        var deserialize = () => JsonSerializer.Deserialize<UpdateCustomerAddressRequest>("{}");
        deserialize.Should().Throw<JsonException>();
    }
}
