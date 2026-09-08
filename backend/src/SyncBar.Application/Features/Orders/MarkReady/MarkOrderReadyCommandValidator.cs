using FluentValidation;

namespace SyncBar.Application.Features.Orders.MarkReady;

public sealed class MarkOrderReadyCommandValidator : AbstractValidator<MarkOrderReadyCommand>
{
    public MarkOrderReadyCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
    }
}
