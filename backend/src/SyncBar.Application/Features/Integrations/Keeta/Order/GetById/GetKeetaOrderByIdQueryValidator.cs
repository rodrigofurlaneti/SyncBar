using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetById
{
    public sealed class GetKeetaOrderByIdQueryValidator
        : AbstractValidator<GetKeetaOrderByIdQuery>
    {
        public GetKeetaOrderByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("O identificador do pedido (Id) deve ser maior que zero.");
        }
    }
}
