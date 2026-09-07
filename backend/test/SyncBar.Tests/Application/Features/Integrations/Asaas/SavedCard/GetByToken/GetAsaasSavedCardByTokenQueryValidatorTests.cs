using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByToken;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.GetByToken;

public sealed class GetAsaasSavedCardByTokenQueryValidatorTests
{
    private readonly GetAsaasSavedCardByTokenQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidCreditCardToken_ShouldBeValid()
        => _validator.Validate(new GetAsaasSavedCardByTokenQuery("tok_000001")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyCreditCardToken_ShouldBeInvalid()
        => _validator.Validate(new GetAsaasSavedCardByTokenQuery(string.Empty)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_CreditCardTokenExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(new GetAsaasSavedCardByTokenQuery(new string('a', 151))).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_CreditCardTokenAtMaxLength_ShouldBeValid()
        => _validator.Validate(new GetAsaasSavedCardByTokenQuery(new string('a', 150))).IsValid.Should().BeTrue();
}
