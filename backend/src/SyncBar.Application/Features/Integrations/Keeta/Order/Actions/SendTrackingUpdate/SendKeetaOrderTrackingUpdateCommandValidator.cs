using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.SendTrackingUpdate
{
    public sealed class SendKeetaOrderTrackingUpdateCommandValidator : AbstractValidator<SendKeetaOrderTrackingUpdateCommand>
    {
        public SendKeetaOrderTrackingUpdateCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("O identificador do pedido (OrderId) deve ser maior que zero.");

            RuleFor(x => x.TrackingEventType)
                .NotEmpty()
                .WithMessage("O tipo do evento de rastreio é obrigatório.");
        }
    }
}
