using FluentValidation;
namespace SyncBar.Application.Features.OrderOrigin.Update
{
    public sealed class UpdateOrderOriginCommandValidator : AbstractValidator<UpdateOrderOriginCommand>
    {
        public UpdateOrderOriginCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("O ID da origem do pedido é inválido.");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome da origem do pedido é obrigatório.")
                .MaximumLength(100).WithMessage("O nome não pode exceder 100 caracteres.");
        }
    }
}
