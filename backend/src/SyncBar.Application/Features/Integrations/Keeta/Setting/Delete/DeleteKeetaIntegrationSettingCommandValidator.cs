using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Setting.Delete
{
    public sealed class DeleteKeetaIntegrationSettingCommandValidator
        : AbstractValidator<DeleteKeetaIntegrationSettingCommand>
    {
        public DeleteKeetaIntegrationSettingCommandValidator()
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
