using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.ExistsForBranch
{
    public sealed class ExistsKeetaSettingForBranchQueryValidator
        : AbstractValidator<ExistsKeetaSettingForBranchQuery>
    {
        public ExistsKeetaSettingForBranchQueryValidator()
        {
            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
        }
    }
}
