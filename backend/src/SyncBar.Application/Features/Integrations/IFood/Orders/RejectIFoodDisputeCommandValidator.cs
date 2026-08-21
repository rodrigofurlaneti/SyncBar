using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed class RejectIFoodDisputeCommandValidator : AbstractValidator<RejectIFoodDisputeCommand>
{
    public RejectIFoodDisputeCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.DisputeId).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
