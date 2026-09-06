using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Create
{
    public sealed class CreateKeetaIntegrationRefundDisputeCommandValidator
        : AbstractValidator<CreateKeetaIntegrationRefundDisputeCommand>
    {
        public CreateKeetaIntegrationRefundDisputeCommandValidator()
        {
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
            RuleFor(x => x.BranchId).GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
            RuleFor(x => x.OrderId).NotEmpty()
                .WithMessage("O OrderId é obrigatório.");
            RuleFor(x => x.AfterSaleOrderId).GreaterThan(0)
                .WithMessage("O AfterSaleOrderId deve ser maior que zero.");
            RuleFor(x => x.RefundAmount).GreaterThan(0)
                .WithMessage("O valor do reembolso (RefundAmount) deve ser maior que zero.");
            RuleFor(x => x.ApplyReason).NotEmpty()
                .WithMessage("O motivo da solicitação (ApplyReason) é obrigatório.");
        }
    }
}
