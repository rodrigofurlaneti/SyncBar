using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.Update
{
    public sealed class UpdateAsaasIntegrationSavedCardCommandValidator
        : AbstractValidator<UpdateAsaasIntegrationSavedCardCommand>
    {
        public UpdateAsaasIntegrationSavedCardCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador do cartão (Id) deve ser maior que zero.");

            RuleFor(x => x.CustomerId)
                .GreaterThan(0)
                .WithMessage("O identificador do cliente (CustomerId) deve ser maior que zero.");

            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            When(x => !string.IsNullOrWhiteSpace(x.HolderName), () =>
            {
                RuleFor(x => x.HolderName)
                    .MaximumLength(100)
                    .WithMessage("O nome do titular deve ter no máximo 100 caracteres.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.ExpiryMonth), () =>
            {
                RuleFor(x => x.ExpiryMonth)
                    .Matches(@"^(0[1-9]|1[0-2])$")
                    .WithMessage("Mês de validade deve conter 2 dígitos entre 01 e 12 (MM).");
            });

            When(x => !string.IsNullOrWhiteSpace(x.ExpiryYear), () =>
            {
                RuleFor(x => x.ExpiryYear)
                    .Matches(@"^\d{4}$")
                    .WithMessage("Ano de validade deve conter 4 dígitos (AAAA).")
                    .Must(year => int.TryParse(year, out var y) && y >= DateTime.UtcNow.Year)
                    .WithMessage("O ano de validade do cartão não pode ser anterior ao ano atual.");
            });
        }
    }
}
