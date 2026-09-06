using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Update
{
    public sealed class UpdateKeetaIntegrationRefundDisputeCommandValidator
        : AbstractValidator<UpdateKeetaIntegrationRefundDisputeCommand>
    {
        public UpdateKeetaIntegrationRefundDisputeCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("O identificador da disputa (Id) deve ser maior que zero.");
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
