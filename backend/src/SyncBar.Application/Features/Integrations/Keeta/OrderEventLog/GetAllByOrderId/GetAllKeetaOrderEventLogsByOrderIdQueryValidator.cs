using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetAllByOrderId
{
    public sealed class GetAllKeetaOrderEventLogsByOrderIdQueryValidator
        : AbstractValidator<GetAllKeetaOrderEventLogsByOrderIdQuery>
    {
        public GetAllKeetaOrderEventLogsByOrderIdQueryValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty()
                .WithMessage("O OrderId é obrigatório.");
        }
    }
}
