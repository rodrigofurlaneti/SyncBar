using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByBranchId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.GetByBranchId;

public sealed class GetByBranchIdQueryValidatorTests
{
    private readonly GetByBranchIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveBranchId_ShouldBeValid()
        => _validator.Validate(new GetByBranchIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new GetByBranchIdQuery(branchId)).IsValid.Should().BeFalse();
}
