using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.ConfirmOrder
{
    public sealed class ConfirmKeetaOrderCommandValidator : AbstractValidator<ConfirmKeetaOrderCommand>
    {
        public ConfirmKeetaOrderCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("O identificador do pedido (OrderId) deve ser maior que zero.");

            RuleFor(x => x.PreparationTimeMinutes)
                .GreaterThan(0)
                .When(x => x.PreparationTimeMinutes.HasValue)
                .WithMessage("O tempo de preparo, quando informado, deve ser maior que zero.");
        }
    }
}
