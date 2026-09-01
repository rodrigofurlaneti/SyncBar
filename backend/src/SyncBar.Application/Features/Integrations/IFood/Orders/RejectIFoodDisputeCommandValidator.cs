using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

public sealed class RejectIfoodDisputeCommandValidator : AbstractValidator<RejectIfoodDisputeCommand>
{
    public RejectIfoodDisputeCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.DisputeId).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
