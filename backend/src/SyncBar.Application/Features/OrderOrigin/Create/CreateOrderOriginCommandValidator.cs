using FluentValidation;

namespace SyncBar.Application.Features.OrderOrigin.Create
{
    public sealed class CreateOrderOriginCommandValidator : AbstractValidator<CreateOrderOriginCommand>
    {
        public CreateOrderOriginCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome da origem do pedido é obrigatório.")
                .MaximumLength(100).WithMessage("O nome não pode exceder 100 caracteres.");
        }
    }
}
