using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByCustomerOrderId
{
    public sealed class GetByCustomerOrderIdQueryValidator : AbstractValidator<GetByCustomerOrderIdQuery>
    {
        public GetByCustomerOrderIdQueryValidator()
        {
            RuleFor(x => x.CustomerOrderId)
                .GreaterThan(0)
                .WithMessage("O identificador do pedido (CustomerOrderId) deve ser maior que zero.");
        }
    }
}
