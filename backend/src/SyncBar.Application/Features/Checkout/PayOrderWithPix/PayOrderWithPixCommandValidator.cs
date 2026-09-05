using FluentValidation;

namespace SyncBar.Application.Features.Checkout.PayOrderWithPix
{
    public sealed class PayOrderWithPixCommandValidator : AbstractValidator<PayOrderWithPixCommand>
    {
        public PayOrderWithPixCommandValidator()
        {
            RuleFor(x => x.CustomerOrderId)
                .GreaterThan(0)
                .WithMessage("O identificador do pedido (CustomerOrderId) deve ser maior que zero.");
        }
    }
}
