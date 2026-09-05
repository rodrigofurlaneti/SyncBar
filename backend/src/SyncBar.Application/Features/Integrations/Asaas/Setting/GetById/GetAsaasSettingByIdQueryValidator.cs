using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetById
{
    public sealed class GetAsaasSettingByIdQueryValidator
        : AbstractValidator<GetAsaasSettingByIdQuery>
    {
        public GetAsaasSettingByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador da configuração (Id) deve ser maior que zero.");
        }
    }
}
