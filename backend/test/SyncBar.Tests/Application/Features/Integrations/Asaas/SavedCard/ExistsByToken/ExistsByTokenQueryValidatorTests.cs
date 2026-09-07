using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.ExistsByToken;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.ExistsByToken;

public sealed class ExistsByTokenQueryValidatorTests
{
    private readonly ExistsByTokenQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidCreditCardToken_ShouldBeValid()
        => _validator.Validate(new ExistsByTokenQuery("tok_000001")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyCreditCardToken_ShouldBeInvalid()
        => _validator.Validate(new ExistsByTokenQuery(string.Empty)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_CreditCardTokenExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(new ExistsByTokenQuery(new string('a', 151))).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_CreditCardTokenAtMaxLength_ShouldBeValid()
        => _validator.Validate(new ExistsByTokenQuery(new string('a', 150))).IsValid.Should().BeTrue();
}
