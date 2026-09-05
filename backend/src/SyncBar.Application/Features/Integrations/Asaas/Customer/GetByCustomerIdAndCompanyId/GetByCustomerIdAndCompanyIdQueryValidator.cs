using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId
{
    public sealed class GetByCustomerIdAndCompanyIdQueryValidator : AbstractValidator<GetByCustomerIdAndCompanyIdQuery>
    {
        public GetByCustomerIdAndCompanyIdQueryValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0)
                .WithMessage("O identificador do cliente (CustomerId) deve ser maior que zero.");

            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
