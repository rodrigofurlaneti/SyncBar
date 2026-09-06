using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.RequestCancellation
{
    public sealed class RequestKeetaOrderCancellationCommandValidator : AbstractValidator<RequestKeetaOrderCancellationCommand>
    {
        private static readonly string[] ValidCodes =
        [
            "SYSTEMIC_ISSUES", "DUPLICATE_APPLICATION", "UNAVAILABLE_ITEM",
            "RESTAURANT_WITHOUT_DELIVERY_PERSON", "OUTDATED_MENU", "ORDER_OUTSIDE_THE_DELIVERY_AREA",
            "BLOCKED_CUSTOMER", "OUTSIDE_DELIVERY_HOURS", "INTERNAL_DIFFICULTIES_OF_THE_RESTAURANT",
            "RISK_AREA", "DELIVERY_PROBLEM"
        ];

        private static readonly string[] ValidModes = ["AUTO", "MANUAL"];

        public RequestKeetaOrderCancellationCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("O identificador do pedido (OrderId) deve ser maior que zero.");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("O motivo do cancelamento é obrigatório.");

            RuleFor(x => x.Code)
                .Must(code => ValidCodes.Contains(code))
                .WithMessage($"O código do cancelamento deve ser um dos valores: {string.Join(", ", ValidCodes)}.");

            RuleFor(x => x.Mode)
                .Must(mode => ValidModes.Contains(mode))
                .WithMessage("O modo do cancelamento deve ser AUTO ou MANUAL.");
        }
    }
}
