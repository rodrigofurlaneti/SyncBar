using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByIdForUpdate
{
    public sealed class GetKeetaSettingByIdForUpdateQueryValidator
        : AbstractValidator<GetKeetaSettingByIdForUpdateQuery>
    {
        public GetKeetaSettingByIdForUpdateQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador da configuração (Id) deve ser maior que zero.");
        }
    }
}
