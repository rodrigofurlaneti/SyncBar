using FluentValidation;
using SyncBar.Application.Abstractions.Integrations.Ifood;

namespace SyncBar.Application.Features.Integrations.Ifood.Financial;

public sealed class GetIfoodFinancialReportQueryValidator : AbstractValidator<GetIfoodFinancialReportQuery>
{
    public GetIfoodFinancialReportQueryValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ReportType).IsInEnum();
    }
}
