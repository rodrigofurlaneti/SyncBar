using FluentAssertions;
using SyncBar.Application.Features.Integrations.Ifood.Review;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Review;

// Cobertura das regras de validação (FluentValidation) do comando de
// Integrations/Ifood/Review — sem FluentValidation.TestHelper (não referenciado neste projeto),
// então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodReviewValidatorsTests
{
    [Fact]
    public void ReplyIfoodReviewCommandValidator_WithValidCommand_ShouldBeValid()
        => new ReplyIfoodReviewCommandValidator()
            .Validate(new ReplyIfoodReviewCommand(1, "review-1", "Obrigado pelo feedback!"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void ReplyIfoodReviewCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new ReplyIfoodReviewCommandValidator()
            .Validate(new ReplyIfoodReviewCommand(0, "review-1", "Obrigado pelo feedback!"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ReplyIfoodReviewCommandValidator_WithEmptyReviewId_ShouldBeInvalid()
        => new ReplyIfoodReviewCommandValidator()
            .Validate(new ReplyIfoodReviewCommand(1, "", "Obrigado pelo feedback!"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ReplyIfoodReviewCommandValidator_WithEmptyText_ShouldBeInvalid()
        => new ReplyIfoodReviewCommandValidator()
            .Validate(new ReplyIfoodReviewCommand(1, "review-1", ""))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ReplyIfoodReviewCommandValidator_WithTextLongerThan2000Chars_ShouldBeInvalid()
        => new ReplyIfoodReviewCommandValidator()
            .Validate(new ReplyIfoodReviewCommand(1, "review-1", new string('A', 2001)))
            .IsValid.Should().BeFalse();
}
