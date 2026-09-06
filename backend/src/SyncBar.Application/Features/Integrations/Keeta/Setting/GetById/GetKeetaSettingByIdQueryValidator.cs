using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetById
{
    public sealed class GetKeetaSettingByIdQueryValidator
        : AbstractValidator<GetKeetaSettingByIdQuery>
    {
        public GetKeetaSettingByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador da configuração (Id) deve ser maior que zero.");
        }
    }
}
