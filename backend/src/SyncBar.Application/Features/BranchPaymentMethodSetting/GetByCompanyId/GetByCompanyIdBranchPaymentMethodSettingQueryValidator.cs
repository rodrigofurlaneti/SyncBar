using FluentValidation;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetByCompanyId
{
    public sealed class GetByCompanyIdBranchPaymentMethodSettingQueryValidator
        : AbstractValidator<GetByCompanyIdBranchPaymentMethodSettingQuery>
    {
        public GetByCompanyIdBranchPaymentMethodSettingQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}