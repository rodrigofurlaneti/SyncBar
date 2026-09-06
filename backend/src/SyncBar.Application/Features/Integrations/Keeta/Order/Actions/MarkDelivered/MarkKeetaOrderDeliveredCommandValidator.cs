using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.MarkDelivered
{
    public sealed class MarkKeetaOrderDeliveredCommandValidator : AbstractValidator<MarkKeetaOrderDeliveredCommand>
    {
        public MarkKeetaOrderDeliveredCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("O identificador do pedido (OrderId) deve ser maior que zero.");
        }
    }
}
