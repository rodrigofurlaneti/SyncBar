using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchOrCompanyFallback
{
    public sealed class GetByBranchOrCompanyFallbackQueryValidator
        : AbstractValidator<GetByBranchOrCompanyFallbackQuery>
    {
        public GetByBranchOrCompanyFallbackQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            When(x => x.BranchId.HasValue, () =>
            {
                RuleFor(x => x.BranchId!.Value)
                    .GreaterThan(0)
                    .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
            });
        }
    }
}
