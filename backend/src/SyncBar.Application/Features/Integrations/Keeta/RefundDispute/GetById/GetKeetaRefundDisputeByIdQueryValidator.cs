using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById
{
    public sealed class GetKeetaRefundDisputeByIdQueryValidator
        : AbstractValidator<GetKeetaRefundDisputeByIdQuery>
    {
        public GetKeetaRefundDisputeByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("O identificador da disputa (Id) deve ser maior que zero.");
        }
    }
}
