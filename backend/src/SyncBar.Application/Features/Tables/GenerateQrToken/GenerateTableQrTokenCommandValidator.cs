using FluentValidation;

namespace SyncBar.Application.Features.Tables.GenerateQrToken;

public sealed class GenerateTableQrTokenCommandValidator : AbstractValidator<GenerateTableQrTokenCommand>
{
    public GenerateTableQrTokenCommandValidator()
    {
        RuleFor(x => x.DiningTableId).GreaterThan(0);
    }
}
