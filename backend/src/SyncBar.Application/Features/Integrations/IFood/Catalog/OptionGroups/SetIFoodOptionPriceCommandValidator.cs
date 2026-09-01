using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

public sealed class SetIfoodOptionPriceCommandValidator : AbstractValidator<SetIfoodOptionPriceCommand>
{
    public SetIfoodOptionPriceCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionId).NotEmpty();
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
    }
}
