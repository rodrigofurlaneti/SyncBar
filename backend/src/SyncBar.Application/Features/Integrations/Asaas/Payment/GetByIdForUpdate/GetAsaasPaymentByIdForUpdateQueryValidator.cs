using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByIdForUpdate
{
    public sealed class GetAsaasPaymentByIdForUpdateQueryValidator : AbstractValidator<GetAsaasPaymentByIdForUpdateQuery>
    {
        public GetAsaasPaymentByIdForUpdateQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador do pagamento (Id) deve ser maior que zero.");
        }
    }
}
