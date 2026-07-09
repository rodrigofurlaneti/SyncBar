using FluentValidation;

namespace SyncBar.Application.Features.Cash.OpenSession;

public sealed class OpenCashSessionCommandValidator : AbstractValidator<OpenCashSessionCommand>
{
    public OpenCashSessionCommandValidator()
    {
        RuleFor(x => x.CashRegisterId).GreaterThan(0);
        RuleFor(x => x.OpenedByEmployeeId).GreaterThan(0);
        RuleFor(x => x.OpeningAmount).GreaterThanOrEqualTo(0);
    }
}
