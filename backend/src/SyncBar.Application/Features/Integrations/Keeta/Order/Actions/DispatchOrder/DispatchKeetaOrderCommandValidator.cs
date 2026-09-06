using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.DispatchOrder
{
    public sealed class DispatchKeetaOrderCommandValidator : AbstractValidator<DispatchKeetaOrderCommand>
    {
        public DispatchKeetaOrderCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("O identificador do pedido (OrderId) deve ser maior que zero.");
        }
    }
}
