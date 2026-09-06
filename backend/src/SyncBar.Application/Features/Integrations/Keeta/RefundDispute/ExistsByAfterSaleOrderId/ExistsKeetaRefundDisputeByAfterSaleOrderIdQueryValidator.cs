using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.ExistsByAfterSaleOrderId
{
    public sealed class ExistsKeetaRefundDisputeByAfterSaleOrderIdQueryValidator
        : AbstractValidator<ExistsKeetaRefundDisputeByAfterSaleOrderIdQuery>
    {
        public ExistsKeetaRefundDisputeByAfterSaleOrderIdQueryValidator()
        {
            RuleFor(x => x.AfterSaleOrderId).GreaterThan(0)
                .WithMessage("O AfterSaleOrderId deve ser maior que zero.");
        }
    }
}
