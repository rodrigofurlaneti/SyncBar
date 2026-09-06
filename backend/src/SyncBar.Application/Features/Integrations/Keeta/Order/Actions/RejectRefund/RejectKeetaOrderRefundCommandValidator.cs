using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.RejectRefund
{
    public sealed class RejectKeetaOrderRefundCommandValidator : AbstractValidator<RejectKeetaOrderRefundCommand>
    {
        private static readonly string[] ValidCodes = ["DISH_ALREADY_DONE", "OUT_FOR_DELIVERY", "OTHER"];

        public RejectKeetaOrderRefundCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("O identificador do pedido (OrderId) deve ser maior que zero.");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("O motivo da rejeição do reembolso é obrigatório.");

            RuleFor(x => x.Code)
                .Must(code => ValidCodes.Contains(code))
                .WithMessage($"O código da rejeição deve ser um dos valores: {string.Join(", ", ValidCodes)}.");
        }
    }
}
