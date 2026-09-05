using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId
{
    public sealed class GetAllAsaasCustomersByCompanyIdQueryValidator
        : AbstractValidator<GetAllAsaasCustomersByCompanyIdQuery>
    {
        public GetAllAsaasCustomersByCompanyIdQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
