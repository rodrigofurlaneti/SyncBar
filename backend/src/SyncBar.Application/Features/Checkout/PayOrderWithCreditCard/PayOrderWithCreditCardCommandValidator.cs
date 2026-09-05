using FluentValidation;

namespace SyncBar.Application.Features.Checkout.PayOrderWithCreditCard
{
    public sealed class PayOrderWithCreditCardCommandValidator : AbstractValidator<PayOrderWithCreditCardCommand>
    {
        public PayOrderWithCreditCardCommandValidator()
        {
            RuleFor(x => x.CustomerOrderId)
                .GreaterThan(0)
                .WithMessage("O identificador do pedido (CustomerOrderId) deve ser maior que zero.");

            RuleFor(x => x)
                .Must(x => x.SavedCardId.HasValue ^ x.Card is not null)
                .WithMessage("Informe um cartão salvo (SavedCardId) OU os dados de um cartão novo (Card), nunca os dois nem nenhum.");

            When(x => x.Card is not null, () =>
            {
                RuleFor(x => x.Card!.HolderName).NotEmpty().WithMessage("Nome do titular do cartão é obrigatório.");
                RuleFor(x => x.Card!.Number).CreditCard().WithMessage("Número de cartão de crédito inválido.");
                RuleFor(x => x.Card!.ExpiryMonth).NotEmpty().Length(2).WithMessage("Mês de expiração deve ter 2 dígitos (MM).");
                RuleFor(x => x.Card!.ExpiryYear).NotEmpty().Length(4).WithMessage("Ano de expiração deve ter 4 dígitos (AAAA).");
                RuleFor(x => x.Card!.Ccv).NotEmpty().Length(3, 4).WithMessage("CCV deve ter 3 ou 4 dígitos.");
            });
        }
    }
}
