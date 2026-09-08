using FluentValidation;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.ExistsForBranch
{
    public sealed class ExistsBranchPaymentMethodSettingForBranchQueryValidator
        : AbstractValidator<ExistsBranchPaymentMethodSettingForBranchQuery>
    {
        public ExistsBranchPaymentMethodSettingForBranchQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
        }
    }
}