using FluentValidation;
using SyncBar.Application.Abstractions.Integrations.IFood;

namespace SyncBar.Application.Features.Integrations.IFood.Financial;

public sealed class GetIFoodFinancialReportQueryValidator : AbstractValidator<GetIFoodFinancialReportQuery>
{
    public GetIFoodFinancialReportQueryValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ReportType).IsInEnum();
    }
}
