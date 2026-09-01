using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

public sealed class RequestIfoodDisputeAlternativeCommandValidator : AbstractValidator<RequestIfoodDisputeAlternativeCommand>
{
    public RequestIfoodDisputeAlternativeCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.DisputeId).NotEmpty().MaximumLength(60);
        RuleFor(x => x.AlternativeId).NotEmpty().MaximumLength(60);
        RuleFor(x => x.AlternativeType).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue);
        RuleFor(x => x.Currency).MaximumLength(3).When(x => x.Currency is not null);
    }
}
