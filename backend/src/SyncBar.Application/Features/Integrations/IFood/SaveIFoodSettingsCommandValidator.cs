using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood;

public sealed class SaveIFoodSettingsCommandValidator : AbstractValidator<SaveIFoodSettingsCommand>
{
    public SaveIFoodSettingsCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.ClientId).MaximumLength(200);

        // Não dá pra ligar a integração sem credenciais — evita salvar Enabled=true "no vazio".
        RuleFor(x => x.ClientId)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithMessage("Informe o Client ID antes de ativar a integração.");

        RuleFor(x => x.IFoodCustomerId).MaximumLength(100);
    }
}
