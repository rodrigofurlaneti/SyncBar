using FluentValidation;
namespace SyncBar.Application.Features.OrderOrigin.Remove
{
    public sealed class RemoveOrderOriginCommandValidator : AbstractValidator<RemoveOrderOriginCommand>
    {
        public RemoveOrderOriginCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O ID da origem do pedido é inválido.");
        }
    }
}
