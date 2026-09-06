using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetById
{
    public sealed class GetKeetaOrderEventLogByIdQueryValidator
        : AbstractValidator<GetKeetaOrderEventLogByIdQuery>
    {
        public GetKeetaOrderEventLogByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("O identificador do evento (Id) deve ser maior que zero.");
        }
    }
}
