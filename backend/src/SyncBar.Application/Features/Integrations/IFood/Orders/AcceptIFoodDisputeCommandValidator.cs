using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed class AcceptIFoodDisputeCommandValidator : AbstractValidator<AcceptIFoodDisputeCommand>
{
    public AcceptIFoodDisputeCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.DisputeId).NotEmpty().MaximumLength(60);
    }
}
