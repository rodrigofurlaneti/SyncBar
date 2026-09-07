using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.Create;

public sealed class CreateKeetaIntegrationRefundDisputeCommandValidatorTests
{
    private readonly CreateKeetaIntegrationRefundDisputeCommandValidator _validator = new();

    private static CreateKeetaIntegrationRefundDisputeCommand ValidCommand() => new(
        CompanyId: 1,
        BranchId: 1,
        OrderId: "order-1",
        AfterSaleOrderId: 1,
        RefundAmount: 10m,
        ApplyReason: "reason");

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(ValidCommand() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(ValidCommand() with { BranchId = branchId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyOrderId_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { OrderId = string.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveAfterSaleOrderId_ShouldBeInvalid(long afterSaleOrderId)
        => _validator.Validate(ValidCommand() with { AfterSaleOrderId = afterSaleOrderId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveRefundAmount_ShouldBeInvalid(decimal refundAmount)
        => _validator.Validate(ValidCommand() with { RefundAmount = refundAmount }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyApplyReason_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { ApplyReason = string.Empty }).IsValid.Should().BeFalse();
}
