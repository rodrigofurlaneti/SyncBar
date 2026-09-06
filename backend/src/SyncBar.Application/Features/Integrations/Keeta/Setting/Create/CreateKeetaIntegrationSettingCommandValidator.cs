using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Setting.Create
{
    public sealed class CreateKeetaIntegrationSettingCommandValidator
        : AbstractValidator<CreateKeetaIntegrationSettingCommand>
    {
        public CreateKeetaIntegrationSettingCommandValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            RuleFor(x => x.BranchId)
                .GreaterThanOrEqualTo(0)
                .WithMessage("O identificador da filial (BranchId) não pode ser negativo.");

            RuleFor(x => x.ClientId)
                .NotEmpty()
                .WithMessage("O ClientId é obrigatório.");

            RuleFor(x => x.ClientSecret)
                .NotEmpty()
                .WithMessage("O ClientSecret é obrigatório.");

            RuleFor(x => x.AppId)
                .NotEmpty()
                .WithMessage("O AppId é obrigatório.");
        }
    }
}
