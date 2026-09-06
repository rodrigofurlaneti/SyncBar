using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.MarkReadyForPickup
{
    public sealed class MarkKeetaOrderReadyForPickupCommandValidator : AbstractValidator<MarkKeetaOrderReadyForPickupCommand>
    {
        public MarkKeetaOrderReadyForPickupCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("O identificador do pedido (OrderId) deve ser maior que zero.");
        }
    }
}
