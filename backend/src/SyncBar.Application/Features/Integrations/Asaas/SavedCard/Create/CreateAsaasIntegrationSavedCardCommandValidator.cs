using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.Create
{
    public sealed class CreateAsaasIntegrationSavedCardCommandValidator : AbstractValidator<CreateAsaasIntegrationSavedCardCommand>
    {
        public CreateAsaasIntegrationSavedCardCommandValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0)
                .WithMessage("O identificador do cliente (CustomerId) deve ser maior que zero.");

            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            RuleFor(x => x.HolderName)
                .NotEmpty()
                .WithMessage("O nome impresso no cartão (HolderName) é obrigatório.")
                .MaximumLength(100)
                .WithMessage("O nome do titular deve ter no máximo 100 caracteres.");

            RuleFor(x => x.CardNumber)
                .NotEmpty()
                .CreditCard()
                .WithMessage("Número de cartão de crédito inválido.");

            RuleFor(x => x.ExpiryMonth)
                .NotEmpty()
                .Matches(@"^(0[1-9]|1[0-2])$")
                .WithMessage("Mês de validade deve conter 2 dígitos entre 01 e 12 (MM).");

            RuleFor(x => x.ExpiryYear)
                .NotEmpty()
                .Matches(@"^\d{4}$")
                .WithMessage("Ano de validade deve conter 4 dígitos (AAAA).")
                .Must(year => int.TryParse(year, out var y) && y >= DateTime.UtcNow.Year)
                .WithMessage("O ano de validade do cartão não pode ser anterior ao ano atual.");

            RuleFor(x => x.Ccv)
                .NotEmpty()
                .Matches(@"^\d{3,4}$")
                .WithMessage("O código de segurança (CCV) deve conter 3 ou 4 dígitos numéricos.");
        }
    }
}
