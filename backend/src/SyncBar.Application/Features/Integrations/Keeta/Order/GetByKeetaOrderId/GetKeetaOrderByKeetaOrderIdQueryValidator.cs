using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetByKeetaOrderId
{
    public sealed class GetKeetaOrderByKeetaOrderIdQueryValidator
        : AbstractValidator<GetKeetaOrderByKeetaOrderIdQuery>
    {
        public GetKeetaOrderByKeetaOrderIdQueryValidator()
        {
            RuleFor(x => x.KeetaOrderId).NotEmpty()
                .WithMessage("O KeetaOrderId é obrigatório.");
        }
    }
}
