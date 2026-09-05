using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.Create
{
    public sealed class CreateAsaasIntegrationCustomerCommandValidator : AbstractValidator<CreateAsaasIntegrationCustomerCommand>
    {
        public CreateAsaasIntegrationCustomerCommandValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0)
                .WithMessage("O identificador do cliente (CustomerId) deve ser maior que zero.");

            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            RuleFor(x => x.AsaasCustomerId)
                .NotEmpty()
                .WithMessage("O ID do cliente Asaas (AsaasCustomerId) é obrigatório.")
                .MaximumLength(50)
                .WithMessage("O ID do cliente Asaas deve conter no máximo 50 caracteres.");
        }
    }
}
