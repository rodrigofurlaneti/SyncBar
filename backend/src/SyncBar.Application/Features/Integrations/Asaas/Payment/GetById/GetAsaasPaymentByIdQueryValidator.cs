using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetById
{
    public sealed class GetAsaasPaymentByIdQueryValidator : AbstractValidator<GetAsaasPaymentByIdQuery>
    {
        public GetAsaasPaymentByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador do pagamento (Id) deve ser maior que zero.");
        }
    }
}
