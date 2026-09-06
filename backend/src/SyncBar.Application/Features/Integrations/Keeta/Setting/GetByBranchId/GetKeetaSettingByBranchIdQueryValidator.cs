using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchId
{
    public sealed class GetKeetaSettingByBranchIdQueryValidator
        : AbstractValidator<GetKeetaSettingByBranchIdQuery>
    {
        public GetKeetaSettingByBranchIdQueryValidator()
        {
            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
        }
    }
}
