using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Create
{
    public sealed class CreateKeetaIntegrationOrderCommandValidator
        : AbstractValidator<CreateKeetaIntegrationOrderCommand>
    {
        public CreateKeetaIntegrationOrderCommandValidator()
        {
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
            RuleFor(x => x.BranchId).GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
            RuleFor(x => x.CustomerOrderId).GreaterThan(0)
                .WithMessage("O identificador do pedido interno (CustomerOrderId) deve ser maior que zero.");
            RuleFor(x => x.KeetaOrderId).NotEmpty()
                .WithMessage("O KeetaOrderId é obrigatório.");
            RuleFor(x => x.DisplayId).NotEmpty()
                .WithMessage("O DisplayId é obrigatório.");
            RuleFor(x => x.InternalMerchantId).NotEmpty()
                .WithMessage("O InternalMerchantId é obrigatório.");
            RuleFor(x => x.KeetaMerchantId).GreaterThan(0)
                .WithMessage("O KeetaMerchantId deve ser maior que zero.");
            RuleFor(x => x.OrderType).NotEmpty()
                .WithMessage("O tipo do pedido (OrderType) é obrigatório.");
            RuleFor(x => x.DeliveredBy).NotEmpty()
                .WithMessage("O responsável pela entrega (DeliveredBy) é obrigatório.");
            RuleFor(x => x.OrderAmount).GreaterThanOrEqualTo(0)
                .WithMessage("O valor do pedido (OrderAmount) não pode ser negativo.");
            RuleFor(x => x.RawOrderJson).NotEmpty()
                .WithMessage("O payload bruto do pedido (RawOrderJson) é obrigatório.");
        }
    }
}
