using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.Update
{
    public sealed class UpdateAsaasIntegrationCustomerCommandValidator : AbstractValidator<UpdateAsaasIntegrationCustomerCommand>
    {
        public UpdateAsaasIntegrationCustomerCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador (Id) deve ser maior que zero.");

            RuleFor(x => x.NewAsaasCustomerId)
                .NotEmpty()
                .WithMessage("O novo AsaasCustomerId é obrigatório.")
                .MaximumLength(50)
                .WithMessage("O AsaasCustomerId deve conter no máximo 50 caracteres.");
        }
    }
}
