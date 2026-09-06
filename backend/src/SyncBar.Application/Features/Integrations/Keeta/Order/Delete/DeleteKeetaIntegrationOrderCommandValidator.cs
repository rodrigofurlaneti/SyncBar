using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Delete
{
    public sealed class DeleteKeetaIntegrationOrderCommandValidator
        : AbstractValidator<DeleteKeetaIntegrationOrderCommand>
    {
        public DeleteKeetaIntegrationOrderCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("O identificador do pedido (Id) deve ser maior que zero.");
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
