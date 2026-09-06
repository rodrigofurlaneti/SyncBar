using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.ExistsByEventId
{
    public sealed class ExistsKeetaOrderEventLogByEventIdQueryValidator
        : AbstractValidator<ExistsKeetaOrderEventLogByEventIdQuery>
    {
        public ExistsKeetaOrderEventLogByEventIdQueryValidator()
        {
            RuleFor(x => x.EventId).NotEmpty()
                .WithMessage("O EventId é obrigatório.");
        }
    }
}
