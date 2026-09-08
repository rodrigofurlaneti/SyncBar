using FluentAssertions;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using NSubstitute;
using SyncBar.API.Authorization;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Cash;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Authentication;
using Xunit;

namespace SyncBar.Tests;

public sealed class AuditRegressionTests
{
    [Fact]
    public void ReadingProof_IsBoundToTableComandaMethodAndExpiry()
    {
        var clock = new TestClock();
        var service = new ReadingProofService(new EphemeralDataProtectionProvider(), clock);
        var token = Guid.NewGuid();
        var proof = service.Issue(token, "001", "barcode");
        service.Validate(proof, token, "001", ["barcode"]).Should().BeTrue();
        service.Validate(proof, Guid.NewGuid(), "001", ["barcode"]).Should().BeFalse();
        service.Validate(proof, token, "002", ["barcode"]).Should().BeFalse();
        service.Validate(proof, token, "001", ["camera"]).Should().BeFalse();
        service.Validate(proof + "invalid", token, "001", ["barcode"]).Should().BeFalse();
        clock.Now = clock.Now.AddMinutes(31);
        service.Validate(proof, token, "001", ["barcode"]).Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(OrdersController))]
    [InlineData(typeof(CashController))]
    [InlineData(typeof(PreparationController))]
    [InlineData(typeof(CategoriesController))]
    public void FeatureConvention_AttachesAuthorizationToEndpoints(Type type)
    {
        var model = new ControllerModel(type.GetTypeInfo(), []) { ControllerName = type.Name.Replace("Controller", "") };
        new FeatureAccessConvention().Apply(model);
        model.Filters.OfType<AuthorizeFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task DisabledPix_IsRejectedUsingBranchConfiguration()
    {
        var branches = Substitute.For<IBranchRepository>();
        var settings = Substitute.For<IBranchPaymentMethodSettingRepository>();
        branches.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(Branch.Create(1, "Filial", null, null, null, null, null, null, null, null).Value);
        var flags = BranchPaymentMethodSetting.Create(1, 7, false, true, true, true, true);
        settings.GetByBranchOrCompanyFallbackAsync(1, 7, Arg.Any<CancellationToken>()).Returns(flags.Value);
        var service = new PaymentMethodAvailability(branches, settings, Substitute.For<ICustomerOrderRepository>());
        (await service.ValidateAsync(7, [4], CancellationToken.None)).IsFailure.Should().BeTrue();
        (await service.ValidateAsync(7, [1], CancellationToken.None)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Shift_ConsolidatesCardDifferenceAlongWithCash()
    {
        var shift = ShiftClosing.Open(1, 1).Value;
        var session = CashSession.Open(1, 1, 100).Value;
        typeof(CashSession).GetProperty("Id")!.SetValue(session, 10L);
        var card = CashSessionPaymentReconciliation.Create(10, 2, 200, 190).Value;
        session.Close(1, 95, 100, [card]).IsSuccess.Should().BeTrue();
        shift.Close(1, DateTime.Now.AddMinutes(1), [session], null, [card]).IsSuccess.Should().BeTrue();
        shift.TotalExpectedAmount.Should().Be(300);
        shift.TotalRealizedAmount.Should().Be(285);
        shift.TotalDifferenceAmount.Should().Be(-15);
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
