using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Delete
{
    public sealed class DeleteKeetaIntegrationRefundDisputeCommandValidator
        : AbstractValidator<DeleteKeetaIntegrationRefundDisputeCommand>
    {
        public DeleteKeetaIntegrationRefundDisputeCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("O identificador da disputa (Id) deve ser maior que zero.");
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
