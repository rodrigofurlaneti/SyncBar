using FluentValidation;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.ExistsForCompany
{
    public sealed class ExistsBranchPaymentMethodSettingForCompanyQueryValidator
        : AbstractValidator<ExistsBranchPaymentMethodSettingForCompanyQuery>
    {
        public ExistsBranchPaymentMethodSettingForCompanyQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}