using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchIdForUpdate
{
    public sealed class GetKeetaSettingByBranchIdForUpdateQueryValidator
        : AbstractValidator<GetKeetaSettingByBranchIdForUpdateQuery>
    {
        public GetKeetaSettingByBranchIdForUpdateQueryValidator()
        {
            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
        }
    }
}
