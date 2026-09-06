using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.AcceptRefund
{
    public sealed class AcceptKeetaOrderRefundCommandValidator : AbstractValidator<AcceptKeetaOrderRefundCommand>
    {
        public AcceptKeetaOrderRefundCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("O identificador do pedido (OrderId) deve ser maior que zero.");
        }
    }
}
