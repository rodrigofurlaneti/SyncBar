using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SyncBar.API.Controllers;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class ShiftRouteTests
{
    [Theory]
    [InlineData("Open", "api/shift-closing")]
    [InlineData("GetOpen", "api/shift-closing/open")]
    [InlineData("GetHistory", "api/shift-closing/history")]
    [InlineData("Close", "api/shift-closing/{id:long}/close")]
    [InlineData("Open", "api/ShiftClosing")]
    public void Mvc_ShouldExposeShiftRoutes(string action, string route)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers().AddApplicationPart(typeof(ShiftClosingController).Assembly);
        using var provider = services.BuildServiceProvider();
        var descriptors = provider.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items;
        Assert.Contains(descriptors.OfType<ControllerActionDescriptor>(), descriptor =>
            descriptor.ControllerTypeInfo.AsType() == typeof(ShiftClosingController) &&
            descriptor.ActionName == action && descriptor.AttributeRouteInfo?.Template == route);
    }
}
