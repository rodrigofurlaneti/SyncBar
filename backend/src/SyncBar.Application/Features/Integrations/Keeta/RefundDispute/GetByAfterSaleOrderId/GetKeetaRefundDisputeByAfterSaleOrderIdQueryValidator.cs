using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetByAfterSaleOrderId
{
    public sealed class GetKeetaRefundDisputeByAfterSaleOrderIdQueryValidator
        : AbstractValidator<GetKeetaRefundDisputeByAfterSaleOrderIdQuery>
    {
        public GetKeetaRefundDisputeByAfterSaleOrderIdQueryValidator()
        {
            RuleFor(x => x.AfterSaleOrderId).GreaterThan(0)
                .WithMessage("O AfterSaleOrderId deve ser maior que zero.");
        }
    }
}
