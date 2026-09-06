using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Setting.Update
{
    public sealed class UpdateKeetaIntegrationSettingCommandValidator
        : AbstractValidator<UpdateKeetaIntegrationSettingCommand>
    {
        public UpdateKeetaIntegrationSettingCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador da configuração (Id) deve ser maior que zero.");

            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
