using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetByDisplayId
{
    public sealed class GetKeetaOrderByDisplayIdQueryValidator
        : AbstractValidator<GetKeetaOrderByDisplayIdQuery>
    {
        public GetKeetaOrderByDisplayIdQueryValidator()
        {
            RuleFor(x => x.DisplayId).NotEmpty()
                .WithMessage("O DisplayId é obrigatório.");
        }
    }
}
