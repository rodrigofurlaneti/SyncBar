using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.Create
{
    public sealed class CreateAsaasIntegrationPaymentCommandValidator : AbstractValidator<CreateAsaasIntegrationPaymentCommand>
    {
        private static readonly string[] ValidBillingTypes = ["PIX", "CREDIT_CARD", "BOLETO", "UNDEFINED"];

        public CreateAsaasIntegrationPaymentCommandValidator()
        {
            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");

            RuleFor(x => x.CustomerOrderId)
                .GreaterThan(0)
                .WithMessage("O identificador do pedido (CustomerOrderId) deve ser maior que zero.");

            RuleFor(x => x.Value)
                .GreaterThan(0)
                .WithMessage("O valor da cobrança deve ser maior que zero.");

            RuleFor(x => x.DueDate)
                .GreaterThanOrEqualTo(DateTime.Today)
                .WithMessage("A data de vencimento não pode ser anterior a hoje.");

            RuleFor(x => x.BillingType)
                .NotEmpty()
                .Must(type => ValidBillingTypes.Contains(type.ToUpperInvariant()))
                .WithMessage("Forma de cobrança inválida. Valores aceitos: PIX, CREDIT_CARD, BOLETO.");

            RuleFor(x => x.InstallmentCount)
                .GreaterThanOrEqualTo(1)
                .WithMessage("O número de parcelas deve ser no mínimo 1.");

            When(x => x.BillingType.Equals("CREDIT_CARD", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(x.CreditCardToken), () =>
            {
                RuleFor(x => x.CreditCard)
                    .NotNull()
                    .WithMessage("Dados do cartão são obrigatórios caso não seja informado um CreditCardToken.");

                When(x => x.CreditCard is not null, () =>
                {
                    RuleFor(x => x.CreditCard!.HolderName).NotEmpty().WithMessage("Nome do titular do cartão é obrigatório.");
                    RuleFor(x => x.CreditCard!.Number).CreditCard().WithMessage("Número de cartão de crédito inválido.");
                    RuleFor(x => x.CreditCard!.ExpiryMonth).NotEmpty().Length(2).WithMessage("Mês de expiração deve ter 2 dígitos (MM).");
                    RuleFor(x => x.CreditCard!.ExpiryYear).NotEmpty().Length(4).WithMessage("Ano de expiração deve ter 4 dígitos (AAAA).");
                    RuleFor(x => x.CreditCard!.Ccv).NotEmpty().Length(3, 4).WithMessage("CCV deve ter 3 ou 4 dígitos.");
                });
            });
        }
    }
}
