using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByIdForUpdate
{
    public sealed class GetAsaasSettingByIdForUpdateQueryValidator
        : AbstractValidator<GetAsaasSettingByIdForUpdateQuery>
    {
        public GetAsaasSettingByIdForUpdateQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador da configuração (Id) deve ser maior que zero.");
        }
    }
}
