using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.ExistsByKeetaOrderId
{
    public sealed class ExistsKeetaOrderByKeetaOrderIdQueryValidator
        : AbstractValidator<ExistsKeetaOrderByKeetaOrderIdQuery>
    {
        public ExistsKeetaOrderByKeetaOrderIdQueryValidator()
        {
            RuleFor(x => x.KeetaOrderId).NotEmpty()
                .WithMessage("O KeetaOrderId é obrigatório.");
        }
    }
}
