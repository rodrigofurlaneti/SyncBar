using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

public sealed class AcceptIfoodDisputeCommandValidator : AbstractValidator<AcceptIfoodDisputeCommand>
{
    public AcceptIfoodDisputeCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.DisputeId).NotEmpty().MaximumLength(60);
    }
}
