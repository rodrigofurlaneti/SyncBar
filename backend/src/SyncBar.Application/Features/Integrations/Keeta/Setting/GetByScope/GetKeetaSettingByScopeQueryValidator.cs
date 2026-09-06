using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByScope
{
    public sealed class GetKeetaSettingByScopeQueryValidator
        : AbstractValidator<GetKeetaSettingByScopeQuery>
    {
        public GetKeetaSettingByScopeQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
