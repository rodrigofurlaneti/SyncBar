using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetPendingByBranchId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.GetPendingByBranchId;

public sealed class GetPendingAsaasPaymentsByBranchIdQueryValidatorTests
{
    private readonly GetPendingAsaasPaymentsByBranchIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveBranchId_ShouldBeValid()
        => _validator.Validate(new GetPendingAsaasPaymentsByBranchIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new GetPendingAsaasPaymentsByBranchIdQuery(branchId)).IsValid.Should().BeFalse();
}
